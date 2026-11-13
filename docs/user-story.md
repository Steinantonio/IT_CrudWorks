# User Story — CrudWorks

- **Author:** candidate (orchestrator session)
- **Date:** 2026-11-11
- **Status:** accepted
- **Requirement:** R-04 (informal user story created by the candidate)

## The story

I keep my work in notes, chat messages, and a paper pad. Every Monday I spend
more time reconstructing what I promised than actually doing it. I do not need a
project-management suite; I need one small place where I can sign in, write down
what I owe, move it forward, and close it when it is done — without wondering who
else can see it.

So I want a simple web application where I can create an account, sign in, and
manage a private list of tasks. A task has a title, an optional description, a
status (todo, in progress, done), and an optional due date. I want to see my tasks
newest first, edit one when it changes, and delete one when it is no longer
relevant. Most importantly, my tasks are mine: another signed-in user must never
be able to read, change, or delete them, even if they guess the identifier.

## Why this shape

- **Authentication is not decoration.** The privacy promise only means something
  if every task is tied to an authenticated owner and every query is scoped to
  that owner.
- **One list, not a workspace.** Sharing, teams, comments, and notifications
  would dilute the exercise. The value is a small flow that is fully correct.
- **The demo must be immediate.** A seeded account and a handful of tasks let a
  reviewer see the product in under a minute.

## In plain language, done means

1. I can register with my name, email, and password, and the password is never
   stored or shown in plain text.
2. I can sign in and receive a session; an invalid email or password gives the
   same safe message either way.
3. I can create a task; it belongs to me, not to a value I typed into the form.
4. I can list my tasks and only my tasks, newest first.
5. I can open, edit, and delete a task I own.
6. If I ask for someone else's task — by id, by guessing, by changing a request —
   the answer is "not found", never a peek at their data.
7. On a phone or a laptop, the forms are usable, labeled, and keyboard-friendly.
8. A reviewer can clone the project, run two commands, sign in with the demo
   account, and complete the flow without editing code.

## Out of scope (deliberately)

Password reset, email verification, refresh tokens, roles, task sharing, teams,
attachments, comments, reminders, search, pagination, drag-and-drop, dark mode.
These are product decisions, not forgotten features; the reasons are in
`specs/000-product/spec.md` §2 and ADR-0002.

## Traceability

| Story promise | Spec | Acceptance criteria |
| --- | --- | --- |
| Register, sign in, current user | `specs/001-auth/spec.md` | AC-001-01…AC-001-07 |
| Create, list, read, update, delete | `specs/002-task-crud/spec.md` | AC-002-01…AC-002-10 |
| Private by ownership | `specs/002-task-crud/spec.md` | AC-002-02…AC-002-05 |
| Usable client | `specs/003-web-client/spec.md` | AC-003-01…AC-003-10 |
| Reproducible demo | `docs/02-definition-of-done.md` | Clean-clone checklist |

## How this story was used

The story is the human anchor for the specs. It does not replace them: the specs
contain the testable Given/When/Then criteria and the API contract. When a
feature was proposed that did not serve this story, it was cut or recorded as
out of scope.
