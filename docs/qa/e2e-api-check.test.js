'use strict';
// Self-test for the IDOR leak detector used by docs/qa/e2e-api-check.js (C4).
// Purpose: prove the corrected assertion still FAILS on a real leak and does
// not flag the standard RFC 7807 ProblemDetails body of a 404.
//
// Run: node --test docs/qa/e2e-api-check.test.js

const test = require('node:test');
const assert = require('node:assert');
const { findLeakedTaskData } = require('./e2e-api-check.js');

const victim = {
  id: '5a0f6a6e-1a3f-4d6f-9c2e-8b7d4f1a2c33',
  title: 'Review the API contract',
};

const problem404 = {
  type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5',
  title: 'Task not found.',
  status: 404,
  traceId: '00-0f6b1c2d3e4f5a6b7c8d9e0f1a2b3c4d-00f6b1c2d3e4f5a6b7c8d9e0f1a2b3c4d-01',
};

test('a standard 404 ProblemDetails body is not reported as a leak', () => {
  const leaks = findLeakedTaskData(problem404, JSON.stringify(problem404), victim);
  assert.deepStrictEqual(leaks, []);
});

test('the numeric ProblemDetails status is not mistaken for the task status enum', () => {
  const leaks = findLeakedTaskData(problem404, JSON.stringify(problem404), victim);
  assert.ok(!leaks.some((l) => l.includes('"status"')));
});

test('a full task resource in the error body is reported as a leak', () => {
  const leaked = {
    id: victim.id,
    title: victim.title,
    description: 'Check ADR-0004 against the implemented routes.',
    status: 'InProgress',
    dueDate: '2026-11-14',
    createdAtUtc: '2026-11-12T18:30:00Z',
    updatedAtUtc: '2026-11-12T18:30:00Z',
  };
  const leaks = findLeakedTaskData(leaked, JSON.stringify(leaked), victim);
  for (const field of ['id', 'description', 'dueDate', 'createdAtUtc', 'updatedAtUtc']) {
    assert.ok(
      leaks.some((l) => l.includes('"' + field + '"')),
      'expected a leak for "' + field + '", got: ' + JSON.stringify(leaks),
    );
  }
  assert.ok(leaks.some((l) => l.includes('"status"')));
  assert.ok(leaks.some((l) => l.includes('"title"')));
});

test('a task status enum string in the error body is reported as a leak', () => {
  const body = { ...problem404, status: 'Todo' };
  const leaks = findLeakedTaskData(body, JSON.stringify(body), victim);
  assert.ok(
    leaks.some((l) => l.includes('task status enum')),
    'expected the task status enum to be flagged, got: ' + JSON.stringify(leaks),
  );
});

test('the victim title echoed by the error body is reported as a leak', () => {
  const body = { ...problem404, title: victim.title };
  const leaks = findLeakedTaskData(body, JSON.stringify(body), victim);
  assert.ok(
    leaks.some((l) => l.includes('victim task title')),
    'expected the echoed title to be flagged, got: ' + JSON.stringify(leaks),
  );
});

test('the victim id echoed in the raw error body is reported as a leak', () => {
  const body = { ...problem404 };
  const raw = JSON.stringify(body).replace('"Task not found."', '"' + victim.id + '"');
  const leaks = findLeakedTaskData(body, raw, victim);
  assert.ok(
    leaks.some((l) => l.includes('victim task id')),
    'expected the echoed id to be flagged, got: ' + JSON.stringify(leaks),
  );
});
