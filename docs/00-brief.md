# 00 — Brief Breakdown

- **Source:** `C:\Users\User\Downloads\Net - BLA - Technical Interview Exercise - V6 (1).pdf`
- **Extracted:** 2026-11-11
- **Timebox:** Friday 2026-11-13 (two working days)
- **Status:** working summary; the PDF is authoritative
- **Purpose:** turn the exercise into testable requirements with IDs

## 1. One-sentence summary

Build a simple full-stack web application with a .NET C# API and data layer, following Clean Architecture and TDD, that supports CRUD on records, user registration and login, authorized and anonymous endpoints, and a responsive frontend, with seeded demo data and a documented GenAI workflow.

## 2. Hard requirements

### Backend

| ID | Requirement | Notes |
| --- | --- | --- |
| R-01 | Web application with API and data layer using .NET C#, ASP.NET, and a database/data store | ASP.NET Core Web API + React |
| R-02 | Clean Architecture principles | Separation of concerns, dependency rule |
| R-03 | TDD methodology | Failing test first; document pragmatic exceptions |
| R-04 | Informal user story created by the candidate | Include in the presentation |
| R-05 | CRUD records via API endpoints | Create, read, update, delete |
| R-06 | Create a user, log in, store user information | Registration + login + persisted user |
| R-07 | At least two tables/objects/containers: app data + users | SQLite tables: `Users`, `Tasks` |
| R-08 | Data table has a unique identifier (PK) and at least two other fields | Task: Id + Title, Description, Status, DueDate, UserId |
| R-09 | API endpoints with appropriate HTTP verbs, parameters, and return values | 200/201/204/400/401/404/409 |
| R-10 | Second API with user creation, login, and authorized + non-authorized endpoints | Auth endpoints + `[Authorize]`/`[AllowAnonymous]` |
| R-11 | Data access layer with CRUD operations | Repository interfaces in Application, EF Core in Infrastructure |
| R-12 | Business logic layer independent of data and API | Application services + validators, no EF/HTTP types |
| R-13 | Unit tests for data access, business logic, and API endpoints | xUnit + SQLite test database + WebApplicationFactory |

### Frontend

| ID | Requirement | Notes |
| --- | --- | --- |
| R-14 | Integrate the backend with a frontend framework | React + TypeScript + Vite |
| R-15 | Responsive and user-friendly | Mobile, tablet, desktop; accessible forms |
| R-16 | CRUD operations in the UI | List, create, edit, delete, auth flow |
| R-17 | Structured components and state | Feature folders, TanStack Query, form validation |

### Submission

| ID | Requirement | Notes |
| --- | --- | --- |
| R-18 | README with setup instructions and documentation | Prereqs, env vars, migrations, run, test, demo |
| R-19 | Seeded data/credentials for demo | Demo user + sample tasks |

### GenAI

| ID | Requirement | Notes |
| --- | --- | --- |
| R-20 | Write the prompt used to generate the API scaffold or full implementation | Task management system: title, description, status, due date, user |
| R-21 | Show the output code or a representative sample | Keep the actual prompt/output pair |
| R-22 | Describe validation, corrections, edge cases, auth, and validations | This is the critical-thinking section |

Because the main application uses the same task-management domain, the GenAI exercise can use the real API scaffold as its output. Do not fabricate a separate toy example: save the actual prompt, the real output, and the real corrections.

### Presentation and review

| ID | Requirement | Notes |
| --- | --- | --- |
| R-23 | Present user story, design choices, architecture, and a demo | 10–15 minute outline |
| R-24 | Answer code review questions and explain decisions | Prepare a Q&A sheet |

## 3. Evaluation criteria

| ID | Criterion | How we answer it |
| --- | --- | --- |
| R-25 | Clean Architecture | Four projects, dependency rule, ADRs, no framework types in the core |
| R-26 | Testing and TDD | Test pyramid, red-green-refactor evidence, coverage report |
| R-27 | Code quality | Warnings as errors, nullable, async, small classes, consistent naming |
| R-28 | Functionality | Demo script, API tests, browser console clean |
| R-29 | Presentation | Rehearsed outline, architecture diagram, trade-offs, Q&A |
| R-30 | GenAI fluency | Prompt log, output samples, validation, corrections, rejected suggestions |

## 4. Domain proposal: Task Manager

Use the same domain as the GenAI exercise. This keeps the application and the AI narrative coherent.

### User story

> As a busy professional, I want to register, log in, and manage my own tasks with a title, description, status, and due date, so that I can see what is due and track progress without relying on scattered notes.

### Entities

**User**

| Field | Type | Rules |
| --- | --- | --- |
| Id | Guid | PK |
| Email | string | required, unique, valid email, normalized |
| DisplayName | string | required, 2–100 chars |
| PasswordHash | string | required; never returned by the API |
| CreatedAtUtc | DateTime | UTC |

**TaskItem**

| Field | Type | Rules |
| --- | --- | --- |
| Id | Guid | PK |
| UserId | Guid | FK → User, required, indexed |
| Title | string | required, 3–200 chars |
| Description | string? | optional, max 2000 chars |
| Status | enum | Todo, InProgress, Done |
| DueDate | DateOnly? | optional; used for sorting |
| CreatedAtUtc | DateTime | UTC |
| UpdatedAtUtc | DateTime | UTC |

### API surface

| Method | Route | Auth | Success | Notes |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | anonymous | 201 | creates user; duplicate email → 409 |
| POST | `/api/auth/login` | anonymous | 200 | returns JWT + user summary |
| GET | `/api/auth/me` | authorized | 200 | current user |
| GET | `/api/tasks` | authorized | 200 | only current user's tasks |
| GET | `/api/tasks/{id}` | authorized | 200 | 404 if not owned |
| POST | `/api/tasks` | authorized | 201 | Location header |
| PUT | `/api/tasks/{id}` | authorized | 200 | full update |
| DELETE | `/api/tasks/{id}` | authorized | 204 | idempotent delete |
| GET | `/api/health` | anonymous | 200 | liveness/readiness |

### Non-goals

- Teams, sharing, comments, attachments, notifications.
- Password reset, email confirmation, OAuth providers.
- Recurring tasks, subtasks, drag-and-drop ordering.
- Microservices, message brokers, background workers.
- Mobile app, offline mode, i18n.

Non-goals are as important as goals: they keep the exercise reviewable and the demo honest.

## 5. Deliverables

- [ ] `project/` solution with four backend projects and a React frontend.
- [ ] `README.md` with setup, architecture, demo credentials, and test commands.
- [ ] `docs/adr/` with the key decisions.
- [ ] `docs/ai/` with the GenAI evidence.
- [ ] `docs/user-story.md` with acceptance criteria.
- [ ] `docs/presentation.md` with the outline and Q&A prep.
- [ ] Seeded demo data and credentials.
- [ ] A passing build, test suite, and frontend build.
- [ ] A rehearsed demo.

## 6. Assumptions and open questions

**Assumptions**

- The panel runs the app locally or in Docker; no cloud deployment is required.
- A single deployable API is acceptable; the exercise does not ask for microservices.
- English is the delivery language.
- The task domain from the GenAI section is acceptable as the main use case.

**Open questions**

- ~~Is there a hard deadline or timebox for the exercise?~~ **Confirmed: Friday 2026-11-13.**
- Does the panel prefer a specific database, or can we choose PostgreSQL?
- Is Docker available in the interview environment?
- Do they expect ASP.NET MVC views, or is a React SPA + Web API acceptable? (The brief says "ASP.NET MVC, Web API" and also asks for a frontend framework, so a SPA is the coherent reading.)
- Is there a required .NET version?

These questions do not block the agent work; they should be resolved before Wave 5.

## 7. Timebox strategy (two working days)

The deadline is Friday 2026-11-13. Optimize for a complete, defensible submission, not for maximum scope.

### Must-have (non-negotiable)

- Four-project Clean Architecture backend with the dependency rule visible.
- `Users` + `Tasks` tables, EF Core migrations, seeded demo user and tasks.
- Register, login, JWT, authorized task CRUD, anonymous health endpoint.
- Unit tests for Domain + Application; API tests for every endpoint; at least one IDOR test.
- React frontend with login/register, task list, create, edit, delete, and loading/error/empty states.
- README with setup, architecture, seeded credentials, and test commands.
- GenAI prompt, real output sample, validation, corrections, and rejected suggestions.
- Presentation outline + rehearsed demo.

### Nice-to-have (cut first if the critical path slips)

- PostgreSQL provider-swap validation against a real PostgreSQL instance.
- Playwright E2E (fallback: API-level end-to-end test).
- Frontend component tests beyond the critical form/list.
- Tailwind or a custom design system beyond a clean responsive baseline.
- CI pipeline, Dockerfile, OpenAPI UI polish.
- Refresh tokens, password reset, email confirmation.

### Critical path

1. Install .NET 10 SDK and Node.js LTS.
2. Scaffold the four projects + React app.
3. Domain + Application + tests (red/green).
4. EF Core + SQLite + migrations + seed.
5. Auth + task endpoints + API tests.
6. React auth + task CRUD.
7. README + GenAI evidence.
8. Presentation + rehearsal.

### Timebox rule

If a wave threatens the critical path, cut from the nice-to-have list. Never cut the dependency rule, the auth/IDOR checks, the core tests, or the demo. A smaller submission that runs and is understood beats a larger one that does not.
