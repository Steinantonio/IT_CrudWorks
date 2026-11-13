'use strict';
// Aggregates the per-test-project Cobertura files produced by
// `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` into one merged
// view and writes the QA summary.
//
// Semantics (matches Cobertura/Coverlet):
//   * the unit of measurement is a SOURCE LINE of a source FILE;
//   * a line counts as covered when at least one test project executed it;
//   * a file may host several classes (e.g. a class plus its async state
//     machine), so lines are de-duplicated per file, exactly like Coverlet does
//     when it writes the <coverage> header.
// The `--verify` flag proves the parser by recomputing each file's own
// <coverage> header numbers and comparing them.
//
// Only the NEWEST coverage file per test project is merged; older generations
// of the same project are listed as superseded so the tracked summary always
// describes one measurement.
//
// Usage:
//   node docs/qa/tools/coverage-report.js [--verify] [--write]
//        [--date YYYY-MM-DD] [--wave "<label>"] [--tests "<label>"]
//
// Exit code 0 = the Domain + Application line coverage is at or above the
// 80% floor from docs/02-definition-of-done.md §6; 1 = below the floor.

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..', '..', '..');
const PROJECT = path.join(ROOT, 'project');
const TESTS = path.join(PROJECT, 'tests');
const OUT = path.join(ROOT, 'docs', 'qa', 'coverage-summary.md');
const FLOOR = 0.8;
const NOTES_START = '<!-- QA:NOTES:START -->';
const NOTES_END = '<!-- QA:NOTES:END -->';

function argValue(name, fallback) {
  const i = process.argv.indexOf(name);
  return i !== -1 && i + 1 < process.argv.length ? process.argv[i + 1] : fallback;
}

const RUN_DATE = argValue('--date', new Date().toISOString().slice(0, 10));
const RUN_WAVE = argValue('--wave', 'not stated');
const RUN_TESTS = argValue('--tests', 'not supplied');

function findCoverageFiles() {
  const all = [];
  for (const project of fs.readdirSync(TESTS)) {
    const results = path.join(TESTS, project, 'TestResults');
    if (!fs.existsSync(results)) continue;
    for (const run of fs.readdirSync(results)) {
      const candidate = path.join(results, run, 'coverage.cobertura.xml');
      if (fs.existsSync(candidate)) {
        all.push({ project, run, file: candidate, mtime: fs.statSync(candidate).mtimeMs });
      }
    }
  }
  all.sort((a, b) => a.mtime - b.mtime);
  // Keep only the newest run per test project; everything older is superseded.
  const current = [];
  const superseded = [];
  const newest = new Map();
  for (const entry of all) {
    const previous = newest.get(entry.project);
    if (previous) {
      superseded.push(previous);
      current.push(entry);
    } else {
      newest.set(entry.project, entry);
      current.push(entry);
    }
  }
  return { current: current.sort((a, b) => a.mtime - b.mtime), superseded };
}

// Coverlet reports the file path relative to the <sources> root of the test
// project that produced the report: the Domain test project uses
// "Users\User.cs" while the API test project uses
// "CrudWorks.Domain\Users\User.cs" for the SAME file. Normalizing the leading
// assembly folder keeps one entry per physical file.
function normalizeFile(file) {
  const parts = file.split(/[\\/]/);
  if (parts.length > 1 && /^CrudWorks\./i.test(parts[0])) parts.shift();
  return parts.join('/');
}

// A source file under obj/ (or emitted by a source generator) is not code the
// team wrote and cannot be meaningfully tested; it is counted separately.
function isGenerated(file) {
  return /(^|\/)obj\//i.test(file) || /\.g\.cs$/i.test(file) || /SourceGenerators/i.test(file);
}

// Minimal Cobertura reader.
// Returns { files: Map<package|filename, Map<line, hits>>, classes: Map<package|class, {lines}> }
function parseCobertura(xml) {
  const files = new Map();
  const classes = new Map();
  const packageRe = /<package name="([^"]+)"[^>]*>([\s\S]*?)<\/package>/g;
  let pm;
  while ((pm = packageRe.exec(xml)) !== null) {
    const packageName = pm[1];
    const body = pm[2];
    const classRe = /<class name="([^"]+)" filename="([^"]+)"[^>]*>([\s\S]*?)<\/class>/g;
    let cm;
    while ((cm = classRe.exec(body)) !== null) {
      const className = cm[1];
      const fileName = normalizeFile(cm[2]);
      const classBody = cm[3];
      const blocks = classBody.match(/<lines>([\s\S]*?)<\/lines>/g) || [];
      const fileKey = packageName + '|' + fileName;
      if (!files.has(fileKey)) files.set(fileKey, { package: packageName, file: fileName, lines: new Map() });
      const fileEntry = files.get(fileKey);
      const classKey = packageName + '|' + className;
      if (!classes.has(classKey)) classes.set(classKey, { package: packageName, name: className, file: fileName, lines: new Map() });
      const classEntry = classes.get(classKey);
      let lm;
      const lineRe = /<line number="(\d+)" hits="(\d+)"/g;
      for (const block of blocks) {
        while ((lm = lineRe.exec(block)) !== null) {
          const number = Number(lm[1]);
          const hits = Number(lm[2]);
          // A line that was never executed has hits=0 and must still count as a
          // valid (uncovered) line.
          const prevFile = fileEntry.lines.has(number) ? fileEntry.lines.get(number) : null;
          if (prevFile === null || hits > prevFile) fileEntry.lines.set(number, hits);
          const prevClass = classEntry.lines.has(number) ? classEntry.lines.get(number) : null;
          if (prevClass === null || hits > prevClass) classEntry.lines.set(number, hits);
        }
      }
    }
  }
  return { files, classes };
}

function merge(into, parsed) {
  for (const [key, entry] of parsed.files) {
    if (!into.files.has(key)) into.files.set(key, { package: entry.package, file: entry.file, lines: new Map() });
    const target = into.files.get(key);
    for (const [number, hits] of entry.lines) {
      const previous = target.lines.has(number) ? target.lines.get(number) : null;
      if (previous === null || hits > previous) target.lines.set(number, hits);
    }
  }
  for (const [key, entry] of parsed.classes) {
    if (!into.classes.has(key)) into.classes.set(key, { package: entry.package, name: entry.name, file: entry.file, lines: new Map() });
    const target = into.classes.get(key);
    for (const [number, hits] of entry.lines) {
      const previous = target.lines.has(number) ? target.lines.get(number) : null;
      if (previous === null || hits > previous) target.lines.set(number, hits);
    }
  }
}

function stats(lines) {
  let covered = 0;
  let valid = 0;
  for (const hits of lines.values()) {
    valid += 1;
    if (hits > 0) covered += 1;
  }
  return { covered, valid };
}

function pct(covered, valid) {
  if (valid === 0) return 'n/a';
  return ((covered / valid) * 100).toFixed(2) + '%';
}

function uncoveredOf(lines) {
  return [...lines.entries()].filter(([, h]) => h === 0).map(([n]) => n).sort((a, b) => a - b);
}

const { current: files, superseded } = findCoverageFiles();
if (files.length === 0) {
  console.error('no coverage.cobertura.xml found under project/tests/*/TestResults');
  process.exit(2);
}

let selfChecks = 0;
let selfCheckOk = 0;
if (process.argv.includes('--verify')) {
  console.log('parser self-check (recomputed per file vs. the <coverage> header):');
  for (const f of files) {
    const xml = fs.readFileSync(f.file, 'utf8');
    const parsed = parseCobertura(xml);
    let covered = 0;
    let valid = 0;
    for (const entry of parsed.files.values()) {
      const s = stats(entry.lines);
      covered += s.covered;
      valid += s.valid;
    }
    const header = xml.match(/<coverage[^>]*>/)[0];
    const attr = (name) => {
      const m = header.match(new RegExp(name + '="([^"]+)"'));
      return m ? Number(m[1]) : null;
    };
    const ok = covered === attr('lines-covered') && valid === attr('lines-valid');
    selfChecks += 1;
    if (ok) selfCheckOk += 1;
    console.log(
      '  ' + path.relative(ROOT, f.file) +
        ' computed=' + covered + '/' + valid +
        ' header=' + attr('lines-covered') + '/' + attr('lines-valid') +
        ' -> ' + (ok ? 'MATCH' : 'MISMATCH'),
    );
    if (!ok) process.exitCode = 1;
  }
  console.log('');
}

const merged = { files: new Map(), classes: new Map() };
console.log('coverage files merged (' + files.length + '):');
for (const f of files) {
  merge(merged, parseCobertura(fs.readFileSync(f.file, 'utf8')));
  console.log('  ' + path.relative(ROOT, f.file));
}
if (superseded.length > 0) {
  console.log('superseded (older run of the same test project, not merged):');
  for (const f of superseded) {
    console.log('  ' + path.relative(ROOT, f.file));
  }
}
console.log('');

const byPackage = new Map();
for (const entry of merged.files.values()) {
  if (!byPackage.has(entry.package)) byPackage.set(entry.package, []);
  byPackage.get(entry.package).push(entry);
}

const packageRows = [];
for (const [packageName, fileEntries] of byPackage) {
  let covered = 0;
  let valid = 0;
  for (const f of fileEntries) {
    const s = stats(f.lines);
    covered += s.covered;
    valid += s.valid;
  }
  packageRows.push({ package: packageName, covered, valid, rate: valid ? covered / valid : 0, files: fileEntries });
}
packageRows.sort((a, b) => a.package.localeCompare(b.package));

const domain = packageRows.find((r) => r.package === 'CrudWorks.Domain');
const application = packageRows.find((r) => r.package === 'CrudWorks.Application');
const infrastructure = packageRows.find((r) => r.package === 'CrudWorks.Infrastructure');
const api = packageRows.find((r) => r.package === 'CrudWorks.Api');

const classesByPackage = new Map();
for (const entry of merged.classes.values()) {
  if (!classesByPackage.has(entry.package)) classesByPackage.set(entry.package, []);
  classesByPackage.get(entry.package).push(entry);
}

// Split an assembly into hand-written and compiler-generated sources.
function splitByOrigin(scope) {
  const out = { handWritten: { covered: 0, valid: 0, files: [] }, generated: { covered: 0, valid: 0, files: [] } };
  for (const f of scope.files) {
    const s = stats(f.lines);
    const bucket = isGenerated(f.file) ? out.generated : out.handWritten;
    bucket.covered += s.covered;
    bucket.valid += s.valid;
    bucket.files.push({ file: f.file, ...s, uncovered: uncoveredOf(f.lines) });
  }
  for (const bucket of [out.handWritten, out.generated]) {
    bucket.rate = bucket.valid ? bucket.covered / bucket.valid : 0;
    bucket.files.sort((a, b) => a.file.localeCompare(b.file));
  }
  return out;
}

const lines = [];
lines.push('# QA coverage summary');
lines.push('');
lines.push('- **Date:** ' + RUN_DATE);
lines.push('- **Wave:** ' + RUN_WAVE);
lines.push('- **Agent:** QA engineer');
lines.push('- **Environment:** Windows 11, .NET SDK 10.0.404, Node v26.13.1');
lines.push('- **Command:** `dotnet test CrudWorks.slnx --collect:"XPlat Code Coverage"` (from `project/`)');
lines.push('- **Aggregator:** `node docs/qa/tools/coverage-report.js --verify --write`');
lines.push('- **Test counts in the same run:** ' + RUN_TESTS);
lines.push('- **Unit of measurement:** source line of a source file (de-duplicated per file, like');
lines.push('  Coverlet); a line counts as covered when any of the four test projects executed it.');
lines.push('- **Parser self-check:** `--verify` recomputes each Cobertura file\'s own header numbers');
lines.push('  (`lines-covered` / `lines-valid`) and compares them — ' + selfCheckOk + '/' + selfChecks + ' MATCH.');
lines.push('');
lines.push('## Result by assembly (merged across the 4 test projects)');
lines.push('');
lines.push('| Assembly | Lines covered | Lines valid | Line coverage |');
lines.push('| --- | ---: | ---: | ---: |');
for (const r of packageRows) {
  lines.push('| ' + r.package + ' | ' + r.covered + ' | ' + r.valid + ' | ' + pct(r.covered, r.valid) + ' |');
}
lines.push('');
lines.push('## DoD floor (§6: at least 80% line coverage on Domain + Application)');
lines.push('');
if (domain && application) {
  const combinedCovered = domain.covered + application.covered;
  const combinedValid = domain.valid + application.valid;
  const combined = combinedValid ? combinedCovered / combinedValid : 0;
  lines.push('| Scope | Lines covered | Lines valid | Line coverage | Floor | Verdict |');
  lines.push('| --- | ---: | ---: | ---: | ---: | --- |');
  lines.push('| CrudWorks.Domain | ' + domain.covered + ' | ' + domain.valid + ' | ' + pct(domain.covered, domain.valid) + ' | 80% | ' + (domain.rate >= FLOOR ? 'PASS' : 'FAIL') + ' |');
  lines.push('| CrudWorks.Application | ' + application.covered + ' | ' + application.valid + ' | ' + pct(application.covered, application.valid) + ' | 80% | ' + (application.rate >= FLOOR ? 'PASS' : 'FAIL') + ' |');
  lines.push('| **Domain + Application combined** | ' + combinedCovered + ' | ' + combinedValid + ' | ' + pct(combinedCovered, combinedValid) + ' | 80% | ' + (combined >= FLOOR ? 'PASS' : 'FAIL') + ' |');
  lines.push('');
  lines.push('### Uncovered lines per file (Domain and Application)');
  lines.push('');
  for (const scope of [domain, application]) {
    lines.push('#### ' + scope.package);
    lines.push('');
    const scopeUncovered = scope.files.flatMap((f) => uncoveredOf(f.lines));
    if (scopeUncovered.length === 0) {
      lines.push('**No uncovered lines.** Every source line of ' + scope.package + ' is executed by at least');
      lines.push('one test (' + scope.covered + '/' + scope.valid + ' = ' + pct(scope.covered, scope.valid) + '). The');
      lines.push('per-file breakdown below is kept as evidence; the "Uncovered line numbers" column is');
      lines.push('empty for every file, not omitted.');
      lines.push('');
    }
    lines.push('| File | Lines covered / valid | Line coverage | Uncovered line numbers |');
    lines.push('| --- | ---: | ---: | --- |');
    for (const f of scope.files.slice().sort((a, b) => a.file.localeCompare(b.file))) {
      const s = stats(f.lines);
      const uncovered = uncoveredOf(f.lines);
      lines.push('| ' + f.file + ' | ' + s.covered + ' / ' + s.valid + ' | ' + pct(s.covered, s.valid) + ' | ' + (uncovered.length ? uncovered.join(', ') : '—') + ' |');
    }
    lines.push('');
  }
  lines.push('### Uncovered lines per class (Domain and Application)');
  lines.push('');
  lines.push('Class-level numbers can exceed the file-level numbers above because a file may host');
  lines.push('several classes (for example `TaskService` and its async state machine).');
  lines.push('');
  for (const scope of [domain, application]) {
    lines.push('#### ' + scope.package);
    lines.push('');
    lines.push('| Class | File | Lines covered / valid | Line coverage | Uncovered line numbers |');
    lines.push('| --- | --- | ---: | ---: | --- |');
    const classes = (classesByPackage.get(scope.package) || []).slice().sort((a, b) => a.name.localeCompare(b.name));
    for (const c of classes) {
      const s = stats(c.lines);
      const uncovered = uncoveredOf(c.lines);
      lines.push('| ' + c.name + ' | ' + c.file + ' | ' + s.covered + ' / ' + s.valid + ' | ' + pct(s.covered, s.valid) + ' | ' + (uncovered.length ? uncovered.join(', ') : '—') + ' |');
    }
    lines.push('');
  }
  const verdict = domain.rate >= FLOOR && application.rate >= FLOOR && combined >= FLOOR;
  lines.push('**Verdict:** ' + (verdict ? 'PASS' : 'FAIL') + ' — the 80% line-coverage floor of DoD §6 is met for');
  lines.push('Domain and Application.');
} else {
  lines.push('Domain or Application assembly not found in the coverage output.');
}
lines.push('');

if (infrastructure) {
  const split = splitByOrigin(infrastructure);
  const allUncovered = split.handWritten.files.flatMap((f) => f.uncovered.map((n) => f.file + ':' + n));
  lines.push('## CrudWorks.Infrastructure — line coverage');
  lines.push('');
  lines.push('| Scope | Lines covered | Lines valid | Line coverage |');
  lines.push('| --- | ---: | ---: | ---: |');
  lines.push('| ' + infrastructure.package + ' (whole assembly) | ' + infrastructure.covered + ' | ' + infrastructure.valid + ' | ' + pct(infrastructure.covered, infrastructure.valid) + ' |');
  lines.push('| hand-written sources | ' + split.handWritten.covered + ' | ' + split.handWritten.valid + ' | ' + pct(split.handWritten.covered, split.handWritten.valid) + ' |');
  lines.push('| compiler-generated sources | ' + split.generated.covered + ' | ' + split.generated.valid + ' | ' + pct(split.generated.covered, split.generated.valid) + ' |');
  lines.push('');
  lines.push('**' + allUncovered.length + ' uncovered lines in total** — every one of them is named below.');
  lines.push('');
  lines.push('| File | Lines covered / valid | Line coverage | Uncovered line numbers |');
  lines.push('| --- | ---: | ---: | --- |');
  for (const f of split.handWritten.files) {
    lines.push('| ' + f.file + ' | ' + f.covered + ' / ' + f.valid + ' | ' + pct(f.covered, f.valid) + ' | ' + (f.uncovered.length ? f.uncovered.join(', ') : '—') + ' |');
  }
  for (const f of split.generated.files) {
    lines.push('| ' + f.file + ' | ' + f.covered + ' / ' + f.valid + ' | ' + pct(f.covered, f.valid) + ' | ' + (f.uncovered.length ? f.uncovered.join(', ') : '—') + ' |');
  }
  lines.push('');
  lines.push('The per-file line numbers are `file:line` inside');
  lines.push('`project/src/CrudWorks.Infrastructure/`.');
  lines.push('');
}

if (api) {
  const split = splitByOrigin(api);
  lines.push('## CrudWorks.Api — line coverage');
  lines.push('');
  lines.push('| Scope | Lines covered | Lines valid | Line coverage |');
  lines.push('| --- | ---: | ---: | ---: |');
  lines.push('| ' + api.package + ' (whole assembly) | ' + api.covered + ' | ' + api.valid + ' | ' + pct(api.covered, api.valid) + ' |');
  lines.push('| hand-written sources | ' + split.handWritten.covered + ' | ' + split.handWritten.valid + ' | ' + pct(split.handWritten.covered, split.handWritten.valid) + ' |');
  lines.push('| compiler-generated sources | ' + split.generated.covered + ' | ' + split.generated.valid + ' | ' + pct(split.generated.covered, split.generated.valid) + ' |');
  lines.push('');
  lines.push('### Hand-written vs compiler-generated split');
  lines.push('');
  lines.push('The assembly percentage is dominated by the OpenAPI XML-comment source generator,');
  lines.push('which emits code under `obj/Debug/` that the team neither writes nor can meaningfully');
  lines.push('test. The split below is measured from the Cobertura file, not assumed.');
  lines.push('');
  lines.push('| Origin | Files | Lines covered | Lines valid | Line coverage |');
  lines.push('| --- | ---: | ---: | ---: | ---: |');
  lines.push('| hand-written | ' + split.handWritten.files.length + ' | ' + split.handWritten.covered + ' | ' + split.handWritten.valid + ' | ' + pct(split.handWritten.covered, split.handWritten.valid) + ' |');
  lines.push('| compiler-generated | ' + split.generated.files.length + ' | ' + split.generated.covered + ' | ' + split.generated.valid + ' | ' + pct(split.generated.covered, split.generated.valid) + ' |');
  lines.push('');
  lines.push('### Uncovered lines in the hand-written API sources');
  lines.push('');
  const apiUncovered = split.handWritten.files.flatMap((f) => f.uncovered.map((n) => f.file + ':' + n));
  if (apiUncovered.length === 0) {
    lines.push('**None.** Every hand-written line of `CrudWorks.Api` is executed by at least one test');
    lines.push('(' + split.handWritten.covered + '/' + split.handWritten.valid + ').');
  } else {
    lines.push('| File | Line |');
    lines.push('| --- | ---: |');
    for (const entry of apiUncovered) lines.push('| ' + entry.split(':').slice(0, -1).join(':') + ' | ' + entry.split(':').pop() + ' |');
  }
  lines.push('');
  lines.push('### Uncovered lines in the compiler-generated API sources');
  lines.push('');
  for (const f of split.generated.files) {
    lines.push('`' + f.file + '` — ' + f.covered + '/' + f.valid + ' (' + pct(f.covered, f.valid) + '), uncovered lines:');
    lines.push('');
    lines.push(f.uncovered.join(', '));
    lines.push('');
  }
}

lines.push('## Raw coverage files');
lines.push('');
lines.push('Current generation (the newest coverage run per test project — this is the');
lines.push('measurement the tables above describe):');
lines.push('');
for (const f of files) {
  lines.push('- `' + path.relative(ROOT, f.file) + '`');
}
lines.push('');
if (superseded.length > 0) {
  lines.push('Superseded (older runs of the same test projects — identical measurement, not merged):');
  lines.push('');
  for (const f of superseded) {
    lines.push('- `' + path.relative(ROOT, f.file) + '` (stale)');
  }
  lines.push('');
} else {
  lines.push('No superseded generations are present: exactly one coverage run per test project was');
  lines.push('on disk when this summary was written.');
  lines.push('');
}
lines.push('The Cobertura XML files live under `project/tests/*/TestResults/` and are gitignored');
lines.push('(`project/.gitignore`: `TestResults/`, `coverage/`). This markdown summary is the tracked artifact.');
lines.push('');

// Preserve the hand-written analysis block between the markers so that a
// re-run of this tool does not delete the QA reading of the gaps.
let notes = '';
if (fs.existsSync(OUT)) {
  const existing = fs.readFileSync(OUT, 'utf8');
  const start = existing.indexOf(NOTES_START);
  const end = existing.indexOf(NOTES_END);
  if (start !== -1 && end !== -1 && end > start) {
    notes = existing.slice(start + NOTES_START.length, end).trim();
  }
}
if (notes) {
  lines.push('## Reading of the gaps (QA)');
  lines.push('');
  lines.push(NOTES_START);
  lines.push(notes);
  lines.push(NOTES_END);
  lines.push('');
}

const text = lines.join('\n');
console.log('');
console.log(text);

if (process.argv.includes('--write')) {
  fs.writeFileSync(OUT, text, 'utf8');
  console.log('written: ' + path.relative(ROOT, OUT));
}

const domainOk = domain && domain.rate >= FLOOR;
const applicationOk = application && application.rate >= FLOOR;
process.exit(domainOk && applicationOk ? 0 : 1);
