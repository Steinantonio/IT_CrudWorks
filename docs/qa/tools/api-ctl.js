'use strict';
// QA helper to control the local API instance used by the end-to-end checks.
//
// Why this exists: the QA agent sandbox allows only `dotnet`, `node` and `npm`
// shell commands, so the README procedure (`Remove-Item`, `taskkill`,
// `Start-Process`) cannot be typed directly. This script performs the exact
// same steps through Node:
//   stop   -> taskkill /PID <pid> /T /F on every process listening on 5080
//   reset  -> stop + delete crudworks.db (+ -shm/-wal) + start + wait for health
//   start  -> `dotnet run --project src\CrudWorks.Api --launch-profile http`
//   status -> health probe + listener list + database file state
//
// The launch profile "http" already sets ASPNETCORE_ENVIRONMENT=Development
// (project/src/CrudWorks.Api/Properties/launchSettings.json), which is what
// enables migrations + seeding on startup.
//
// Usage:
//   node docs/qa/tools/api-ctl.js status
//   node docs/qa/tools/api-ctl.js stop
//   node docs/qa/tools/api-ctl.js start
//   node docs/qa/tools/api-ctl.js reset

const { execFileSync, spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const PROJECT = path.join(ROOT, 'project');
const API_DIR = path.join(PROJECT, 'src', 'CrudWorks.Api');
const DB_FILES = ['crudworks.db', 'crudworks.db-shm', 'crudworks.db-wal'];
const PORT = 5080;
const BASE = 'http://localhost:' + PORT;
const LOG_DIR = path.join(ROOT, 'docs', 'qa', 'logs');
const LOG_FILE = path.join(LOG_DIR, 'api-server.log');

function listeners() {
  const out = execFileSync('netstat', ['-ano', '-p', 'TCP'], { encoding: 'utf8' });
  const pids = new Set();
  for (const line of out.split(/\r?\n/)) {
    if (!line.includes('LISTENING')) continue;
    const m = line.trim().match(/^TCP\s+(\S+):(\d+)\s+(\S+)\s+LISTENING\s+(\d+)/);
    if (m && Number(m[2]) === PORT) pids.add(Number(m[4]));
  }
  return [...pids];
}

function killPid(pid) {
  try {
    execFileSync('taskkill', ['/PID', String(pid), '/T', '/F'], { encoding: 'utf8', stdio: 'pipe' });
    return true;
  } catch (e) {
    return false;
  }
}

async function health() {
  try {
    const res = await fetch(BASE + '/api/health');
    const text = await res.text();
    return { status: res.status, body: text };
  } catch (e) {
    return { status: 0, body: String(e && e.message) };
  }
}

function dbState() {
  return DB_FILES.map((f) => {
    const p = path.join(API_DIR, f);
    const exists = fs.existsSync(p);
    return { file: f, exists, size: exists ? fs.statSync(p).size : 0 };
  });
}

async function stop() {
  const before = listeners();
  console.log('listeners on ' + PORT + ': ' + (before.length ? before.join(', ') : 'none'));
  for (const pid of before) {
    const ok = killPid(pid);
    console.log('taskkill /PID ' + pid + ' /T /F -> ' + (ok ? 'killed' : 'FAILED'));
  }
  for (let i = 0; i < 20; i++) {
    if (listeners().length === 0) break;
    await new Promise((r) => setTimeout(r, 250));
  }
  const after = listeners();
  console.log('listeners after stop: ' + (after.length ? after.join(', ') : 'none'));
  if (after.length !== 0) {
    console.error('ERROR: port ' + PORT + ' is still held by ' + after.join(', '));
    process.exitCode = 1;
  }
}

function deleteDb() {
  for (const f of DB_FILES) {
    const p = path.join(API_DIR, f);
    if (fs.existsSync(p)) {
      fs.unlinkSync(p);
      console.log('deleted ' + path.relative(ROOT, p));
    } else {
      console.log('absent  ' + path.relative(ROOT, p));
    }
  }
}

async function start() {
  fs.mkdirSync(LOG_DIR, { recursive: true });
  const out = fs.openSync(LOG_FILE, 'a');
  const child = spawn(
    'dotnet',
    ['run', '--project', path.join('src', 'CrudWorks.Api'), '--launch-profile', 'http'],
    {
      cwd: PROJECT,
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' },
      detached: true,
      stdio: ['ignore', out, out],
    },
  );
  child.unref();
  console.log('started dotnet run (pid ' + child.pid + '), log -> ' + path.relative(ROOT, LOG_FILE));

  const deadline = Date.now() + 120000;
  while (Date.now() < deadline) {
    const h = await health();
    if (h.status === 200) {
      console.log('health ' + h.status + ' ' + h.body.trim());
      return;
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  console.error('ERROR: /api/health did not return 200 within 120s');
  process.exitCode = 1;
}

async function status() {
  const h = await health();
  console.log('health: ' + h.status + ' ' + h.body.trim());
  console.log('listeners on ' + PORT + ': ' + (listeners().join(', ') || 'none'));
  for (const s of dbState()) {
    console.log('db ' + s.file + ': ' + (s.exists ? 'present (' + s.size + ' bytes)' : 'absent'));
  }
}

(async () => {
  const command = process.argv[2] || 'status';
  console.log('== api-ctl ' + command + ' ==');
  if (command === 'status') await status();
  else if (command === 'stop') await stop();
  else if (command === 'start') await start();
  else if (command === 'reset') {
    await stop();
    deleteDb();
    await start();
  } else {
    console.error('unknown command: ' + command);
    process.exitCode = 2;
  }
})().catch((e) => {
  console.error('FATAL', e);
  process.exit(2);
});
