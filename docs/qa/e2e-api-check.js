'use strict';
// QA end-to-end API check for IT CrudWorks (SPEC-001 + SPEC-002 + ADR-0004).
// Runs against a fresh Development database at http://localhost:5080.
// Usage: node docs/qa/e2e-api-check.js
// Exit code 0 = every check passed.

const fs = require('node:fs');
const path = require('node:path');

const BASE = process.env.BASE || 'http://localhost:5080';
const results = [];

function record(name, pass, detail) {
  results.push({ name, pass });
  console.log((pass ? 'PASS' : 'FAIL') + ' | ' + name + (detail ? ' | ' + detail : ''));
}

async function call(method, path, { token, body } = {}) {
  const headers = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers['Authorization'] = 'Bearer ' + token;
  const res = await fetch(BASE + path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try { json = JSON.parse(text); } catch { json = null; }
  return { status: res.status, headers: res.headers, body: json, text };
}

function errors(body) {
  return body && typeof body === 'object' && body.errors && typeof body.errors === 'object'
    ? body.errors
    : null;
}

// ADR-0006: GET /api/tasks returns { items, nextCursor }, not a bare array.
function pageItems(body) {
  return body && typeof body === 'object' && Array.isArray(body.items) ? body.items : [];
}

// ---------------------------------------------------------------------------
// Task-resource leak detection
// ---------------------------------------------------------------------------
// The task resource (ADR-0004 §4, `TaskResponse`) is:
//   { id, title, description, status, dueDate, createdAtUtc, updatedAtUtc }
// where `status` is the STRING enum "Todo" | "InProgress" | "Done".
//
// The RFC 7807 error body (ApiProblemDetails.Create) is:
//   { type, title, status, traceId[, errors] }
// where `status` is the NUMERIC HTTP status code (404) and `title` is a fixed
// safe string such as "Task not found.".
//
// The first version of check C4 asserted `body.status !== undefined`, i.e. it
// treated the numeric ProblemDetails `status` as if it were the task status
// member. ProblemDetails ALWAYS serializes `status`, so that term was always
// true: the check could never pass and reported a false positive instead of an
// IDOR defect (QA wave 1, 2026-11-12). `title` is likewise ambiguous — it
// exists in both shapes — so it is compared by value against the victim's own
// title instead of by member name.
//
// The check is NOT weakened: it still fails when the error body carries any
// task-resource member, when `status` holds the task status enum, when `title`
// echoes the victim's title, or when the victim's id/title appears in the raw
// response text.
const TASK_STATUS_VALUES = ['Todo', 'InProgress', 'Done'];
const TASK_ONLY_FIELDS = ['id', 'description', 'dueDate', 'createdAtUtc', 'updatedAtUtc', 'userId'];

/**
 * @param {object|null} body parsed response body
 * @param {string} rawText raw response text
 * @param {{id?: string, title?: string}} victim the task the caller must not see
 * @returns {string[]} descriptions of the leaks found; empty means "no task data leaked"
 */
function findLeakedTaskData(body, rawText, victim) {
  const leaks = [];
  const text = typeof rawText === 'string' ? rawText : '';
  const v = victim || {};

  if (body && typeof body === 'object') {
    for (const field of TASK_ONLY_FIELDS) {
      if (body[field] !== undefined) {
        leaks.push('member "' + field + '"');
      }
    }
    // The task status is a string enum; the ProblemDetails status is a number.
    if (typeof body.status === 'string' && TASK_STATUS_VALUES.includes(body.status)) {
      leaks.push('member "status" holds the task status enum "' + body.status + '"');
    }
    if (v.title !== undefined && body.title === v.title) {
      leaks.push('member "title" holds the victim task title');
    }
  }

  if (v.id && text.includes(v.id)) {
    leaks.push('raw body contains the victim task id');
  }
  if (v.title && text.includes(v.title)) {
    leaks.push('raw body contains the victim task title');
  }

  return leaks;
}

// ---------------------------------------------------------------------------
// Cleanup of the scratch user (QA-06)
// ---------------------------------------------------------------------------
// The public API contract (ADR-0004) exposes register / login / me only — there
// is no "delete user" endpoint, so the scratch user registered by check C1
// cannot be removed through the API. The QA harness therefore removes exactly
// its own row from the SQLite file the API is running on, so that a run leaves
// the database in the seeded baseline: one demo user and the five seeded
// tasks.
//
// This is deliberately narrow:
//   * only the row created by THIS run is touched (Id AND Email must match);
//   * tasks owned by that user are removed first, so the FK never breaks;
//   * the seeded demo user and the seeded tasks are never touched;
//   * the cleanup is reported on its own line and is NOT one of the 40
//     checks, so the recorded "40/40" stays valid;
//   * if the cleanup cannot be performed, the run reports it and exits 3 —
//     a green 40/40 must never hide a database that was left dirty.
//
// Override the database file with CRUDWORKS_DB; set QA_SKIP_CLEANUP=1 to skip
// the cleanup (for a non-SQLite provider, where this cleanup does not apply).
const ROOT = path.resolve(__dirname, '..', '..');
const DB_PATH =
  process.env.CRUDWORKS_DB || path.join(ROOT, 'project', 'src', 'CrudWorks.Api', 'crudworks.db');

function cleanupScratchUser(userId, email) {
  if (process.env.QA_SKIP_CLEANUP === '1') {
    return { ok: false, skipped: true, reason: 'QA_SKIP_CLEANUP=1 — cleanup skipped on purpose' };
  }
  if (!userId || !email) {
    return { ok: false, reason: 'the scratch user id or email is unknown — nothing was deleted' };
  }
  if (!fs.existsSync(DB_PATH)) {
    return { ok: false, reason: 'database file not found: ' + DB_PATH };
  }
  let DatabaseSync;
  try {
    ({ DatabaseSync } = require('node:sqlite'));
  } catch (e) {
    return { ok: false, reason: 'node:sqlite is unavailable: ' + e.message };
  }
  let db;
  try {
    db = new DatabaseSync(DB_PATH);
    db.exec('PRAGMA busy_timeout = 5000');
    // EF Core stores a Guid as an UPPERCASE string ("D10BDFF7-…") while the
    // JSON body carries it lowercase ("d10bdff7-…"). SQLite compares TEXT
    // case-sensitively by default, so the comparison is made case-insensitive
    // — otherwise the delete silently matches nothing.
    const owned = db
      .prepare('SELECT Id FROM Tasks WHERE UserId = ? COLLATE NOCASE')
      .all(userId);
    for (const t of owned) db.prepare('DELETE FROM Tasks WHERE Id = ? COLLATE NOCASE').run(t.Id);
    const res = db
      .prepare('DELETE FROM Users WHERE Id = ? COLLATE NOCASE AND Email = ?')
      .run(userId, email);
    const users = db.prepare('SELECT COUNT(*) AS c FROM Users').get().c;
    const tasks = db.prepare('SELECT COUNT(*) AS c FROM Tasks').get().c;
    const demo = db
      .prepare('SELECT COUNT(*) AS c FROM Users WHERE Email = ?')
      .get('demo@crudworks.local').c;
    // The run is clean when ITS OWN row is gone and the seeded demo user
    // survived. A database that legitimately holds other users (someone
    // registered one by hand) is not a failure — only the scratch row and
    // the seeded rows matter here.
    const ok = res.changes === 1 && demo === 1;
    return {
      ok,
      deleted: res.changes,
      ownedTasksRemoved: owned.length,
      users,
      tasks,
      demoUsers: demo,
      baseline: users === 1 && tasks === 5,
      reason: ok
        ? ''
        : 'unexpected state after the delete: users=' + users + ' tasks=' + tasks +
          ' demoUsers=' + demo + ' deleted=' + res.changes,
    };
  } catch (e) {
    return { ok: false, reason: 'sqlite error: ' + e.message };
  } finally {
    if (db) db.close();
  }
}

async function main() {
  // ---------- A. Demo login and seeded data ----------
  const login = await call('POST', '/api/auth/login', {
    body: { email: 'demo@crudworks.local', password: 'Demo123!' },
  });
  const demoToken = login.body && login.body.accessToken;
  record(
    'A1 demo login returns 200 + token',
    login.status === 200 && typeof demoToken === 'string' && demoToken.length > 0,
    'status=' + login.status + ' token=' + (demoToken ? 'present' : 'missing') + ' expiresAtUtc=' + (login.body && login.body.expiresAtUtc),
  );

  const seeded = await call('GET', '/api/tasks', { token: demoToken });
  const tasks = pageItems(seeded.body);
  record(
    'A2 demo list returns 5 seeded tasks',
    seeded.status === 200 && tasks.length === 5,
    'status=' + seeded.status + ' count=' + tasks.length,
  );
  const counts = {};
  for (const t of tasks) counts[t.status] = (counts[t.status] || 0) + 1;
  record(
    'A3 statuses are 2 Todo / 2 InProgress / 1 Done',
    counts.Todo === 2 && counts.InProgress === 2 && counts.Done === 1,
    JSON.stringify(counts),
  );
  const times = tasks.map((t) => t.createdAtUtc);
  record(
    'A4 list is newest first by createdAtUtc',
    JSON.stringify(times) === JSON.stringify([...times].sort().reverse()),
    JSON.stringify(times),
  );

  const page1 = await call('GET', '/api/tasks?take=2', { token: demoToken });
  const page1Items = pageItems(page1.body);
  const page1Cursor = page1.body && page1.body.nextCursor;
  record(
    'A5 take=2 returns 2 tasks + nextCursor',
    page1.status === 200 && page1Items.length === 2 &&
      typeof page1Cursor === 'string' && page1Cursor.length > 0,
    'status=' + page1.status + ' count=' + page1Items.length +
      ' cursor=' + (page1Cursor ? 'present' : 'missing'),
  );

  const page2 = await call(
    'GET',
    '/api/tasks?take=2&cursor=' + encodeURIComponent(page1Cursor || ''),
    { token: demoToken },
  );
  const page2Items = pageItems(page2.body);
  const page2Cursor = page2.body && page2.body.nextCursor;
  const page1Ids = page1Items.map((t) => t.id);
  const duplicateIds = page2Items.filter((t) => page1Ids.includes(t.id));
  record(
    'A6 cursor page returns the next 2 tasks without duplicates',
    page2.status === 200 && page2Items.length === 2 &&
      typeof page2Cursor === 'string' && duplicateIds.length === 0,
    'status=' + page2.status + ' count=' + page2Items.length +
      ' duplicates=' + duplicateIds.length,
  );

  const page3 = await call(
    'GET',
    '/api/tasks?take=2&cursor=' + encodeURIComponent(page2Cursor || ''),
    { token: demoToken },
  );
  const page3Items = pageItems(page3.body);
  record(
    'A7 final page returns 1 task and null nextCursor',
    page3.status === 200 && page3Items.length === 1 &&
      page3.body && page3.body.nextCursor === null,
    'status=' + page3.status + ' count=' + page3Items.length +
      ' nextCursor=' + JSON.stringify(page3.body && page3.body.nextCursor),
  );

  // ---------- B. CRUD lifecycle on the demo user ----------
  const create = await call('POST', '/api/tasks', {
    token: demoToken,
    body: { title: 'QA created task', description: 'from the QA script', status: 'Todo', dueDate: '2026-11-20' },
  });
  const location = create.headers.get('location');
  const created = create.body;
  record(
    'B1 create returns 201 + Location /api/tasks/{id}',
    create.status === 201 && !!location && location.includes('/api/tasks/') && !!created && !!created.id,
    'status=' + create.status + ' location=' + location,
  );
  record(
    'B2 create body has server id/timestamps and stored status',
    !!created && !!created.id && !!created.createdAtUtc && !!created.updatedAtUtc && created.status === 'Todo',
    'id=' + (created && created.id) + ' status=' + (created && created.status),
  );

  const getCreated = await call('GET', '/api/tasks/' + created.id, { token: demoToken });
  record(
    'B3 get by id returns 200',
    getCreated.status === 200 && getCreated.body && getCreated.body.id === created.id,
    'status=' + getCreated.status,
  );

  const put1 = await call('PUT', '/api/tasks/' + created.id, {
    token: demoToken,
    body: { title: 'QA updated task', description: 'updated', status: 'InProgress', dueDate: '2026-11-21' },
  });
  record('B4 put returns 200', put1.status === 200, 'status=' + put1.status);
  const afterPut = await call('GET', '/api/tasks/' + created.id, { token: demoToken });
  const p = afterPut.body || {};
  record(
    'B5 update persists after a fresh GET',
    afterPut.status === 200 && p.title === 'QA updated task' && p.status === 'InProgress' && p.dueDate === '2026-11-21',
    JSON.stringify({ title: p.title, status: p.status, dueDate: p.dueDate }),
  );
  record(
    'B6 owner/createdAtUtc unchanged and updatedAtUtc >= createdAtUtc',
    p.createdAtUtc === created.createdAtUtc && !!p.updatedAtUtc && p.updatedAtUtc >= created.createdAtUtc,
    'createdAtUtc=' + p.createdAtUtc + ' updatedAtUtc=' + p.updatedAtUtc,
  );

  const put2 = await call('PUT', '/api/tasks/' + created.id, {
    token: demoToken,
    body: { title: 'QA updated task', description: null, status: 'Done', dueDate: null },
  });
  const afterPut2 = await call('GET', '/api/tasks/' + created.id, { token: demoToken });
  record(
    'B7 put null dueDate/description clears them',
    put2.status === 200 && afterPut2.body && afterPut2.body.dueDate === null && afterPut2.body.description === null,
    'status=' + put2.status + ' dueDate=' + (afterPut2.body && afterPut2.body.dueDate) + ' description=' + (afterPut2.body && afterPut2.body.description),
  );

  const del = await call('DELETE', '/api/tasks/' + created.id, { token: demoToken });
  const afterDel = await call('GET', '/api/tasks/' + created.id, { token: demoToken });
  record(
    'B8 delete returns 204 then get returns 404',
    del.status === 204 && afterDel.status === 404,
    'delete=' + del.status + ' getAfterDelete=' + afterDel.status,
  );

  // ---------- C. Second user and IDOR ----------
  const emailB = 'qa-user-b+' + Date.now() + '@crudworks.local';
  const regB = await call('POST', '/api/auth/register', {
    body: { email: emailB, displayName: 'QA User B', password: 'Password1!' },
  });
  record(
    'C1 register user B returns 201',
    regB.status === 201 && !!regB.body && !!regB.body.id,
    'status=' + regB.status + ' location=' + regB.headers.get('location'),
  );
  // Kept so the scratch user can be removed again at the end of the run
  // (QA-06 — the API has no delete-user endpoint).
  const scratchUserId = regB.body && regB.body.id;
  const loginB = await call('POST', '/api/auth/login', {
    body: { email: emailB, password: 'Password1!' },
  });
  const tokenB = loginB.body && loginB.body.accessToken;
  record(
    'C2 login user B returns 200 + token',
    loginB.status === 200 && typeof tokenB === 'string' && tokenB.length > 0,
    'status=' + loginB.status,
  );

  const listB = await call('GET', '/api/tasks', { token: tokenB });
  const listBItems = pageItems(listB.body);
  record(
    'C3 user B list is empty (not the demo tasks)',
    listB.status === 200 && listBItems.length === 0,
    'status=' + listB.status + ' count=' + listBItems.length,
  );

  const victim = tasks[0];
  const idorGet = await call('GET', '/api/tasks/' + victim.id, { token: tokenB });
  const idorPut = await call('PUT', '/api/tasks/' + victim.id, {
    token: tokenB,
    body: { title: 'HACKED', status: 'Todo' },
  });
  const idorDel = await call('DELETE', '/api/tasks/' + victim.id, { token: tokenB });
  const idorLeaks = findLeakedTaskData(idorGet.body, idorGet.text, victim);
  record(
    'C4 IDOR read returns 404 and no task data',
    idorGet.status === 404 && idorLeaks.length === 0,
    'status=' + idorGet.status +
      ' leaks=' + (idorLeaks.length ? idorLeaks.join(', ') : 'none') +
      ' body=' + idorGet.text.slice(0, 160),
  );
  record('C5 IDOR update returns 404', idorPut.status === 404, 'status=' + idorPut.status);
  record('C6 IDOR delete returns 404', idorDel.status === 404, 'status=' + idorDel.status);

  const demoAfter = await call('GET', '/api/tasks', { token: demoToken });
  const demoAfterItems = pageItems(demoAfter.body);
  const victimIntact = demoAfterItems.some(
    (t) => t.id === victim.id && t.title === victim.title && t.status === victim.status,
  );
  record(
    'C7 demo data intact after IDOR (5 tasks, victim unchanged)',
    demoAfter.status === 200 && demoAfterItems.length === 5 && victimIntact,
    'count=' + demoAfterItems.length + ' victim=' + JSON.stringify({ title: victim.title, status: victim.status }),
  );

  // ---------- D. Validation ----------
  const v1 = await call('POST', '/api/tasks', { token: demoToken, body: { title: 'ab', status: 'Todo' } });
  record(
    'D1 short title -> 400 errors.title',
    v1.status === 400 && !!errors(v1.body) && !!errors(v1.body).title,
    'status=' + v1.status + ' errors=' + JSON.stringify(v1.body && v1.body.errors),
  );
  const v2 = await call('POST', '/api/tasks', { token: demoToken, body: { title: 'Valid title', status: '1' } });
  record(
    'D2 status "1" -> 400 errors.status',
    v2.status === 400 && !!errors(v2.body) && !!errors(v2.body).status,
    'status=' + v2.status + ' errors=' + JSON.stringify(v2.body && v2.body.errors),
  );
  const v3 = await call('POST', '/api/tasks', { token: demoToken, body: { title: 'Valid title', status: 'todo' } });
  record(
    'D3 status "todo" (lowercase) -> 400 errors.status',
    v3.status === 400 && !!errors(v3.body) && !!errors(v3.body).status,
    'status=' + v3.status + ' errors=' + JSON.stringify(v3.body && v3.body.errors),
  );
  const v4 = await call('POST', '/api/tasks', {
    token: demoToken,
    body: { title: 'Valid title', status: 'Todo', description: 'x'.repeat(2001) },
  });
  record(
    'D4 overlong description -> 400 errors.description',
    v4.status === 400 && !!errors(v4.body) && !!errors(v4.body).description,
    'status=' + v4.status + ' errors=' + JSON.stringify(v4.body && v4.body.errors),
  );
  const v5 = await call('POST', '/api/tasks', {
    token: demoToken,
    body: { title: 'x'.repeat(201), status: 'Todo' },
  });
  record(
    'D5 overlong title -> 400 errors.title',
    v5.status === 400 && !!errors(v5.body) && !!errors(v5.body).title,
    'status=' + v5.status + ' errors=' + JSON.stringify(v5.body && v5.body.errors),
  );
  const v6 = await call('POST', '/api/tasks', {
    token: demoToken,
    body: { title: 'Valid title', status: 'Todo', dueDate: '2026-13-45' },
  });
  record(
    'D6 invalid dueDate -> 400 errors.dueDate',
    v6.status === 400 && !!errors(v6.body) && !!errors(v6.body).dueDate,
    'status=' + v6.status + ' errors=' + JSON.stringify(v6.body && v6.body.errors),
  );

  // ---------- E. Anonymous access ----------
  const anonGet = await call('GET', '/api/tasks');
  const anonGetOne = await call('GET', '/api/tasks/' + victim.id);
  const anonPost = await call('POST', '/api/tasks', { body: { title: 'Anon task', status: 'Todo' } });
  const anonPut = await call('PUT', '/api/tasks/' + victim.id, { body: { title: 'Anon task', status: 'Todo' } });
  const anonDel = await call('DELETE', '/api/tasks/' + victim.id);
  record('E1 anonymous GET /api/tasks -> 401', anonGet.status === 401, 'status=' + anonGet.status);
  record('E2 anonymous GET /api/tasks/{id} -> 401', anonGetOne.status === 401, 'status=' + anonGetOne.status);
  record('E3 anonymous POST /api/tasks -> 401', anonPost.status === 401, 'status=' + anonPost.status);
  record('E4 anonymous PUT /api/tasks/{id} -> 401', anonPut.status === 401, 'status=' + anonPut.status);
  record('E5 anonymous DELETE /api/tasks/{id} -> 401', anonDel.status === 401, 'status=' + anonDel.status);
  const demoListAfterAnon = await call('GET', '/api/tasks', { token: demoToken });
  const demoListAfterAnonItems = pageItems(demoListAfterAnon.body);
  record(
    'E6 anonymous POST wrote nothing',
    demoListAfterAnon.status === 200 && demoListAfterAnonItems.length === 5,
    'count=' + demoListAfterAnonItems.length,
  );

  // ---------- F. Auth edge cases ----------
  const dup = await call('POST', '/api/auth/register', {
    body: { email: 'demo@crudworks.local', displayName: 'Dup', password: 'Password1!' },
  });
  record('F1 duplicate email -> 409', dup.status === 409, 'status=' + dup.status);
  const wrongPw = await call('POST', '/api/auth/login', {
    body: { email: 'demo@crudworks.local', password: 'WrongPass1!' },
  });
  record('F2 wrong password -> 401', wrongPw.status === 401, 'status=' + wrongPw.status);
  const meAnon = await call('GET', '/api/auth/me');
  record('F3 anonymous /api/auth/me -> 401', meAnon.status === 401, 'status=' + meAnon.status);

  // ---------- G. Client-supplied userId is ignored ----------
  const userBId = (loginB.body && loginB.body.user && loginB.body.user.id) || '00000000-0000-0000-0000-000000000000';
  const withUserId = await call('POST', '/api/tasks', {
    token: demoToken,
    body: { title: 'Ownership probe', status: 'Todo', userId: userBId },
  });
  const demoList2 = await call('GET', '/api/tasks', { token: demoToken });
  const bList2 = await call('GET', '/api/tasks', { token: tokenB });
  const demoList2Items = pageItems(demoList2.body);
  const bList2Items = pageItems(bList2.body);
  const ownedByDemo = demoList2Items.some((t) => t.id === (withUserId.body && withUserId.body.id));
  const notOwnedByB = !bList2Items.some((t) => t.id === (withUserId.body && withUserId.body.id));
  record(
    'G1 client-supplied userId ignored; task owned by caller',
    withUserId.status === 201 && ownedByDemo && notOwnedByB,
    'status=' + withUserId.status + ' inDemoList=' + ownedByDemo + ' inBList=' + !notOwnedByB,
  );

  // Cleanup the ownership probe as the demo owner. Without this the script left
  // a sixth task behind, so a second run against the same database failed the
  // count-based checks (A2, A3, C7, E6). The script is now idempotent: it
  // always ends with exactly the 5 seeded tasks in the demo account.
  const probeId = withUserId.body && withUserId.body.id;
  const probeDelete = probeId
    ? await call('DELETE', '/api/tasks/' + probeId, { token: demoToken })
    : { status: 0 };
  const demoList3 = await call('GET', '/api/tasks', { token: demoToken });
  const demoList3Items = pageItems(demoList3.body);
  record(
    'G2 ownership probe removed; demo list back to the 5 seeded tasks',
    probeDelete.status === 204 && demoList3.status === 200 && demoList3Items.length === 5,
    'delete=' + probeDelete.status + ' count=' + demoList3Items.length,
  );

  // ---------- H. Missing task ids ----------
  const missingId = '00000000-0000-0000-0000-000000000000';
  const missGet = await call('GET', '/api/tasks/' + missingId, { token: demoToken });
  const missPut = await call('PUT', '/api/tasks/' + missingId, { token: demoToken, body: { title: 'Nope', status: 'Todo' } });
  const missDel = await call('DELETE', '/api/tasks/' + missingId, { token: demoToken });
  record(
    'H1 missing task GET/PUT/DELETE -> 404',
    missGet.status === 404 && missPut.status === 404 && missDel.status === 404,
    'get=' + missGet.status + ' put=' + missPut.status + ' delete=' + missDel.status,
  );

  const passed = results.filter((r) => r.pass).length;
  console.log('\nSUMMARY ' + passed + '/' + results.length + ' checks passed');

  // ---------- Cleanup of the scratch user registered by C1 (QA-06) ----------
  const cleanup = cleanupScratchUser(scratchUserId, emailB);
  if (cleanup.ok) {
    console.log(
      'CLEANUP | scratch user ' + emailB + ' deleted' +
        (cleanup.ownedTasksRemoved ? ' together with ' + cleanup.ownedTasksRemoved + ' task(s)' : '') +
        '; Users=' + cleanup.users + ' Tasks=' + cleanup.tasks +
        (cleanup.baseline
          ? ' — the database is back at the seeded baseline'
          : ' — the seeded baseline is 1 demo user and 5 tasks; extra rows were already there'),
    );
  } else {
    console.log(
      'CLEANUP FAILED | ' + cleanup.reason +
        ' — the run left the Users table dirty; the idempotency claim does NOT hold for this run',
    );
  }

  if (passed !== results.length) process.exit(1);
  else if (!cleanup.ok) process.exit(3);
  else process.exit(0);
}

if (require.main === module) {
  main().catch((e) => {
    console.error('FATAL', e);
    process.exit(2);
  });
}

module.exports = { findLeakedTaskData, TASK_STATUS_VALUES, TASK_ONLY_FIELDS };
