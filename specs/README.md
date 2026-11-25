# Specs — Spec-Driven Development

This directory is the **source of truth for what the product must do**. Code is a
consequence of a spec, not the other way around.

The project uses a lightweight Spec-Driven Development (SDD) loop combined with
an AI-Driven Development Life Cycle (AI-DLS). The full rationale, the artifact
chain, and the agent mapping are in `docs/07-sdd-and-ai-dls.md`.

## 1. The chain

```
Brief (why) → Constitution (rules) → Spec (what) → Plan (how) → Tasks (steps)
      → Red test → Implementation → Validation → Evidence → Learning
```

| Artifact | Answers | Owner | Location |
| --- | --- | --- | --- |
| Brief | Why does this exist? | Orchestrator | `docs/00-brief.md` |
| Constitution | What is never negotiable? | Orchestrator | `specs/constitution.md` |
| Spec | What must the system do? | Architect | `specs/NNN-*/spec.md` |
| Plan | How will we build it? | Architect | `specs/NNN-*/plan.md` |
| Tasks | What is the next small step? | Orchestrator + implementers | `specs/NNN-*/tasks.md` |
| Code and tests | Does it work? | Implementers | `project/` |
| Evidence | How do we know? | Every agent | `docs/ai/`, `docs/qa/`, `docs/reviews/` |

One artifact has one owner. A change to behavior starts in the spec, not in the
code.

## 2. Spec lifecycle

| Status | Meaning | Who moves it |
| --- | --- | --- |
| `draft` | Being written; not safe to implement | Architect |
| `approved` | Reviewed against the brief and accepted | Orchestrator |
| `implementing` | Tasks are in progress | Orchestrator |
| `implemented` | All acceptance criteria have evidence | QA engineer |
| `retired` | Replaced by another spec; link the successor | Architect |

A spec is **approved** only when:

- every acceptance criterion is testable (Given/When/Then);
- in-scope and out-of-scope are explicit;
- the API contract and validation rules are unambiguous;
- the requirements it satisfies are listed by ID;
- open questions are either resolved or marked as accepted risks.

Implementation does not start on a `draft` spec. If the spec is wrong, fix the
spec first and record why.

## 3. Naming

- Spec folder: `NNN-kebab-case` (`001-auth`, `002-task-crud`).
- Spec ID: `SPEC-NNN`.
- Plan ID: `PLAN-NNN`.
- Acceptance criterion: `AC-NNN-XX`.
- Task: `T-NNN-XX`.
- Evidence entry: `docs/ai/NNN-topic.md`.

Numbers are never reused.

## 4. Scenarios, not wishes

Every acceptance criterion uses Given/When/Then and names the observable result:

```md
### AC-002-03 — Another user's task is invisible
- **Given** user B is authenticated and user A owns task T
- **When** B requests `GET /api/tasks/{T}`
- **Then** the API returns `404` and no task data
```

Avoid "the API should be secure". Name the request, the status code, and the
data that must not leak.

## 5. Requirement traceability

Every spec lists the requirement IDs it satisfies (`R-05`, `R-10`, …). The
orchestrator keeps `docs/04-requirement-traceability.md` current. A requirement
is done only when a spec, a test, and real evidence exist.

## 6. Change protocol

When behavior changes after approval:

1. Add a **Change log** entry to the spec with the date, the reason, and the
   approver.
2. Update the acceptance criteria and the contract.
3. Update the affected tasks and tests.
4. Record the change in `docs/ai/` and, if architectural, in an ADR.

Do not silently "fix" code to match a new idea. The spec is the contract.

## 7. Templates

Copy `specs/_template/` when starting a new capability:

- `spec.md` — problem, scope, stories, acceptance criteria, rules.
- `plan.md` — architecture impact, contract, test strategy, risks.
- `tasks.md` — ordered, owned, testable tasks.

Keep a spec under ~150 lines. If it needs more, split it.

## 8. Current specs

| ID | Capability | Status | Requirements |
| --- | --- | --- | --- |
| SPEC-000 | Product definition and cross-cutting rules | Approved | R-01…R-04, R-23 |
| SPEC-001 | Authentication and user identity | Implemented | R-06, R-10 |
| SPEC-002 | Task CRUD and ownership | Implemented | R-05, R-07…R-09, R-11, R-12 |
| SPEC-003 | Web client experience | Implemented | R-14…R-17 |
| SPEC-004 | Architecture boundary tests | Implemented | R-02, R-25, R-26 |
| SPEC-005 | OpenAPI-generated frontend types | Implemented | R-09, R-14, R-17, R-27 |
| SPEC-006 | Frontend resilience (lazy routes and error boundary) | Implemented | R-14, R-15, R-17 |
| SPEC-007 | Security response headers (CSP, nosniff, referrer, framing, HSTS) | Implemented | R-10, R-27 |
| SPEC-008 | Task list pagination (keyset cursor, 10/page, cap 100) | Implemented | R-05, R-09, R-16, R-17 |
