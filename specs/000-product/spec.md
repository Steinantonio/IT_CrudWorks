# SPEC-000 — Product definition

- **Status:** approved
- **Owner:** architect
- **Approved by:** orchestrator
- **Requirements:** R-01, R-02, R-03, R-04, R-23
- **ADRs:** 0001, 0002, 0003, 0004
- **Last updated:** 2026-11-11

## 1. Problem

A busy professional keeps tasks in scattered notes and loses track of what is due.
They need one small web application where they can register, log in, and manage
only their own tasks. The application is also the vehicle for the BLA technical
exercise: it must demonstrate Clean Architecture, TDD, security, a responsive UI,
and an honest GenAI workflow.

## 2. Scope

**In scope**

- User registration, login, and a persisted user record.
- Task CRUD scoped to the authenticated owner.
- A responsive React client covering the full flow.
- Seed data that makes the demo immediate.
- Specs, ADRs, tests, and AI evidence as first-class deliverables.

**Out of scope**

- Task sharing, teams, roles, or permissions beyond ownership.
- Password reset, email verification, refresh tokens, MFA.
- Attachments, comments, reminders, notifications.
- Pagination, search, bulk actions, drag-and-drop.
- Multi-tenancy, microservices, event sourcing, CQRS.

## 3. Actors

| Actor | Goal |
| --- | --- |
| Visitor | Register or log in to reach the task list. |
| Authenticated user | Manage their own tasks safely. |
| Reviewer / panel | Clone, run, inspect, and challenge every decision. |

## 4. Domain language

| Term | Meaning |
| --- | --- |
| User | A person with a unique email, a display name, and a password hash. |
| Task | A unit of work owned by exactly one user. |
| Ownership | The rule that a user can only see and change their own tasks. |
| Status | `Todo`, `InProgress`, or `Done`. |

## 5. User stories

| ID | Story |
| --- | --- |
| US-000-01 | As a visitor, I want to register and log in, so that my tasks are private. |
| US-000-02 | As a user, I want to create, read, update, and delete my tasks, so that the list reflects reality. |
| US-000-03 | As a user, I want a clear status and due date, so that I know what to do next. |
| US-000-04 | As a reviewer, I want a documented, testable decision trail, so that I can trust the delivery. |

## 6. Cross-cutting acceptance criteria

### AC-000-01 — Clean dependency direction

- **Given** the four backend projects
- **When** their project references are inspected
- **Then** `Domain` references nothing, `Application` references only `Domain`,
  `Infrastructure` references `Application`, and `Api` references both.

### AC-000-02 — A clean clone runs

- **Given** a machine with the .NET SDK and Node.js
- **When** the README commands are followed
- **Then** the API starts, the database is created and seeded, the frontend starts,
  and the demo flow works without editing code.

### AC-000-03 — Every claim has evidence

- **Given** a behavior described in a spec
- **When** it is marked done
- **Then** a test name, a command output, or a `file:line` reference exists.

## 7. Quality attributes

| Attribute | Target |
| --- | --- |
| Security | No plaintext password, no IDOR, no secret in git, safe errors |
| Testability | Every acceptance criterion has an automated or named manual check |
| Maintainability | Warnings as errors; one dependency direction; small classes |
| Accessibility | WCAG AA contrast, labeled inputs, keyboard flow, visible focus |
| Performance | Demo-scale data; no premature optimization |
| Reproducibility | SQLite file reset; migrations and seed run automatically |

## 8. Success definition

The exercise is successful when a senior panel can pick any requirement, trace it
to a spec, a test, and a real command output, and then use the application without
instructions.

## 9. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-11 | Initial definition | Foundation for SDD | Orchestrator |
