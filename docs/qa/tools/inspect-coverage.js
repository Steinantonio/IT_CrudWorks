'use strict';
// Per-file coverage inspector - the reproduction command for the per-file
// uncovered-line tables published in docs/qa/coverage-summary.md.
// Prints the per-file line coverage and the uncovered line numbers for one
// assembly, split by "hand-written" vs "compiler-generated" source.
//
// Usage: node docs/qa/tools/inspect-coverage.js <assembly-package-name>

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const TESTS = path.join(ROOT, 'project', 'tests');

const wanted = process.argv[2];

function findCoverageFiles() {
  const files = [];
  for (const project of fs.readdirSync(TESTS)) {
    const results = path.join(TESTS, project, 'TestResults');
    if (!fs.existsSync(results)) continue;
    for (const run of fs.readdirSync(results)) {
      const candidate = path.join(results, run, 'coverage.cobertura.xml');
      if (fs.existsSync(candidate)) files.push(candidate);
    }
  }
  return files;
}

function normalizeFile(file) {
  const parts = file.split(/[\\/]/);
  if (parts.length > 1 && /^CrudWorks\./i.test(parts[0])) parts.shift();
  return parts.join('/');
}

const merged = new Map();
for (const f of findCoverageFiles()) {
  const xml = fs.readFileSync(f, 'utf8');
  const packageRe = /<package name="([^"]+)"[^>]*>([\s\S]*?)<\/package>/g;
  let pm;
  while ((pm = packageRe.exec(xml)) !== null) {
    const packageName = pm[1];
    if (wanted && packageName !== wanted) continue;
    const classRe = /<class name="([^"]+)" filename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g;
    let cm;
    while ((cm = classRe.exec(pm[2])) !== null) {
      const fileName = normalizeFile(cm[2]);
      if (!merged.has(fileName)) merged.set(fileName, new Map());
      const lines = merged.get(fileName);
      const lineRe = /<line number="(\d+)" hits="(\d+)"/g;
      let lm;
      while ((lm = lineRe.exec(cm[3])) !== null) {
        const n = Number(lm[1]);
        const h = Number(lm[2]);
        const prev = lines.has(n) ? lines.get(n) : null;
        if (prev === null || h > prev) lines.set(n, h);
      }
    }
  }
}

let covered = 0;
let valid = 0;
const rows = [];
for (const [file, lines] of merged) {
  let c = 0;
  const uncovered = [];
  for (const [n, h] of [...lines.entries()].sort((a, b) => a[0] - b[0])) {
    valid += 1;
    if (h > 0) c += 1;
    else uncovered.push(n);
  }
  covered += c;
  rows.push({ file, covered: c, valid: lines.size, uncovered });
}
rows.sort((a, b) => a.file.localeCompare(b.file));
for (const r of rows) {
  console.log(
    r.covered + '/' + r.valid + '  ' + ((r.covered / r.valid) * 100).toFixed(2) + '%  ' +
      r.file + (r.uncovered.length ? '   UNCOVERED: ' + r.uncovered.join(', ') : ''),
  );
}
console.log('TOTAL ' + covered + '/' + valid + ' = ' + ((covered / valid) * 100).toFixed(2) + '%');
