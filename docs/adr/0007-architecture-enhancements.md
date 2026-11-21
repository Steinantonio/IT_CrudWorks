# ADR-0007: Architecture Enhancements for the Portfolio Application

- **Status:** accepted
- **Date:** 2026-11-18
- **Decision owner:** project owner (technical proposal drafted by the orchestrator)
- **Related decisions:** ADR-0001, ADR-0002, ADR-0004, ADR-0005
- **Related specs:** `specs/001-auth/spec.md`, `specs/002-task-crud/spec.md`,
  `specs/003-web-client/spec.md`, `specs/005-openapi-types/spec.md`

> The owner approved this ADR and the order (architecture tests → generated API
> types → frontend resilience). Implementation still requires approved specs
> and ordered tasks for each wave; this ADR alone does not authorize code changes.

## Context

The portfolio application already has explicit backend boundaries and a small
React SPA. The next improvement should make those boundaries easier to verify,
reduce API/frontend contract drift, and improve frontend resilience without
adding enterprise-scale infrastructure.

**Portfolio scope is explicit:** a GitHub Actions CI/CD pipeline is deferred.
The current Render deploy-on-push and platform health check remain in place;
only small fixes needed to keep that flow working are in scope. The waves below
are validated locally and do not add CI workflow work.

### Facts from the current repository

- The four-project dependency direction is encoded in project references:
  Domain has no project reference; Application references Domain;
  Infrastructure references Application; Api references Application and
  Infrastructure
  (`project/src/CrudWorks.Application/CrudWorks.Application.csproj:3-5`,
  `project/src/CrudWorks.Infrastructure/CrudWorks.Infrastructure.csproj:3-5`,
  `project/src/CrudWorks.Api/CrudWorks.Api.csproj:18-21`; Domain has no
  `ProjectReference` in `project/src/CrudWorks.Domain/CrudWorks.Domain.csproj:1-9`).
- There are four existing test projects (Domain, Application, Infrastructure,
  Api; see `project/tests/CrudWorks.Domain.Tests/CrudWorks.Domain.Tests.csproj:10-23`,
  `project/tests/CrudWorks.Application.Tests/CrudWorks.Application.Tests.csproj:10-24`,
  `project/tests/CrudWorks.Infrastructure.Tests/CrudWorks.Infrastructure.Tests.csproj:10-24`,
  and `project/tests/CrudWorks.Api.Tests/CrudWorks.Api.Tests.csproj:10-26`).
  A repository search found no architecture-test package or architecture-test
  project under `project/tests/`.
- ASP.NET Core OpenAPI is registered, but the document endpoint is mapped only
  in Development (`project/src/CrudWorks.Api/Program.cs:24,131-134`).
- The API already uses typed controller results and response annotations
  (`project/src/CrudWorks.Api/Controllers/TasksController.cs:14-57`,
  `project/src/CrudWorks.Api/Controllers/AuthController.cs:13-47`).
- Frontend request/response contracts are partly maintained manually:
  `Task`, `TaskInput`, and task status are declared in
  `project/frontend/src/lib/tasks.ts:4-24`; login response is separately
  declared in `project/frontend/src/auth/AuthProvider.tsx:11-15`.
- Page components are imported eagerly by `AppRoutes`
  (`project/frontend/src/AppRoutes.tsx:1-8`) and rendered through `<Routes>`
  (`project/frontend/src/AppRoutes.tsx:10-40`); `App.tsx` has providers but no
  React error boundary (`project/frontend/src/App.tsx:20-29`).
- Expected application failures currently use typed exceptions; the API maps
  validation/conflict/auth/not-found exceptions to Problem Details centrally
  (`project/src/CrudWorks.Application/Tasks/TaskService.cs:77-82`,
  `project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs:22-57`).
- Problem Details already carries an activity/request `traceId`
  (`project/src/CrudWorks.Api/ErrorHandling/ApiProblemDetails.cs:16-28`), and
  unexpected exceptions are logged with method and path
  (`project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs:41-49`).
- API routes are unversioned (`api/tasks`, `api/auth`)
  (`project/src/CrudWorks.Api/Controllers/TasksController.cs:9-12`,
  `project/src/CrudWorks.Api/Controllers/AuthController.cs:9-11`).
- Domain factories already enforce important invariants for task title and
  description (`project/src/CrudWorks.Domain/Tasks/TaskItem.cs:57-79,96-122`)
  and user email/display name (`project/src/CrudWorks.Domain/Users/User.cs:39-73`).
- JWT access-token expiry is configurable (default 60 minutes)
  (`project/src/CrudWorks.Application/Auth/JwtOptions.cs:7-18`); the SPA handles
  `401` by clearing session and query cache (`project/frontend/src/auth/AuthProvider.tsx:69-80`).

## Decision

Adopt three small implementation waves, in this order:

1. **Wave 1 — architecture tests** (A1): make forbidden dependency directions
   executable and locally verifiable.
2. **Wave 2 — OpenAPI-generated TypeScript types** (A2): reduce manually
   duplicated API shapes.
3. **Wave 3 — route-level code splitting and a top-level error boundary** (A7):
   improve the SPA's initial loading and recovery from render-time failures.

The waves are independent enough to remain small, but must be implemented in
order so each change can be reviewed separately. No CI workflow is added; local
commands and tests are the evidence for these waves.

### Item decisions

| Item | Decision | Rationale |
| --- | --- | --- |
| **A1 Architecture tests** | **Adopt — Wave 1** | Project references enforce part of the rule at compile time, but do not explicitly guard forbidden framework dependencies/namespaces or explain the intended graph to future contributors. A focused test project makes the constitution executable. |
| **A2 OpenAPI → TypeScript types** | **Adopt — Wave 2** | OpenAPI support and typed controller metadata already exist, but frontend models are manually duplicated. Generate types from the API's OpenAPI document and consume those types at the fetch boundary. |
| **A3 Result pattern vs exceptions** | **Decision: keep typed exceptions for this application** | Existing expected failures are a small set of typed exceptions centrally mapped to Problem Details. Introducing `Result<T>` now would require broad signature and call-site churn without a demonstrated workflow that needs composition of expected alternatives. Revisit if use cases accumulate multiple expected branches or exception-driven flow becomes hard to test. |
| **A4 API versioning** | **Defer `/api/v1`** | There is one first-party SPA and no external consumer/versioning requirement. Keep `/api` routes and coordinate breaking changes through the API ADR/spec; add versions when compatibility between independently deployed clients is real. |
| **A5 Domain value objects** | **Defer `Email` / `TaskTitle` wrappers** | The current factories normalize/check values at the domain boundary. There is no demonstrated reuse or rule complexity sufficient to justify EF converters and broader mapping changes. Revisit when the same invariant is duplicated or a richer value-specific behavior appears. |
| **A6 Refresh tokens** | **Defer** | The portfolio login flow has a configurable expiring access token and clears the session on `401`. Refresh tokens add persistence, rotation, revocation, and theft/replay handling; no current product requirement justifies that security surface. |
| **A7 Code splitting + error boundary** | **Adopt — Wave 3** | Five statically imported screens make route-level lazy loading straightforward. A top-level boundary gives a controlled fallback for render-time exceptions; it does not replace normal loading/error handling for API queries. |
| **A8 Structured logging + correlation** | **Defer custom middleware; retain existing trace behavior** | RFC 7807 already includes a trace identifier and the exception handler already emits a structured error log. Do not add a second correlation mechanism until a real Render debugging gap is demonstrated. If needed, add trace ID to a logging scope rather than inventing a new request ID. |

## Implementation waves

All waves require an approved spec and ordered tasks under `specs/README.md
§1-2`; these acceptance criteria are planning inputs, not implementation tasks
approved by this ADR alone.

### Wave 1 — Architecture tests (A1)

**Scope:** Add one focused `CrudWorks.Architecture.Tests` project to the
solution. Use `NetArchTest.Rules` **1.3.2** to assert the intended assembly and
namespace boundaries. A temporary .NET 10.0.404 console smoke project restored
the package and executed a rule against the actual Domain assembly successfully;
this verifies package/API compatibility, not the future xUnit test project.
Keep existing `.csproj` references as the primary compile-time enforcement;
tests protect the architectural policy, not duplicate every compiler check.

**Ordered tasks:**

1. Add the architecture test project with the same target framework and xUnit
   conventions as the existing test projects.
2. Add rules for: Domain has no CrudWorks project or EF/ASP.NET dependency;
   Application depends on Domain only and has no EF/ASP.NET/HTTP dependency;
   Infrastructure does not depend on Api; Api is allowed to compose
   Application + Infrastructure.
3. Add a short test README/comment explaining why each rule exists and linking
   ADR-0001 / the constitution.
4. Run the project test locally, then the full `dotnet test` solution command.
   CI integration remains out of scope.

**Acceptance criteria:**

- Each allowed project-reference direction is stated by a named test.
- An intentionally introduced forbidden dependency makes the relevant test
  fail (prove the test is sensitive, then remove the mutation).
- The architecture test project runs with the existing local test suite and
  does not require a database or network.

**Risks / trade-offs:** A library can produce brittle rules if tests encode
private folder names rather than dependency policy. Test assembly references,
forbidden namespaces, and the constitution; do not freeze arbitrary class
layout. **Estimate:** small change (one test project and a few rules).

### Wave 2 — OpenAPI-generated frontend types (A2)

**Scope:** Make the API's OpenAPI document the machine-readable source for
frontend request/response types while retaining ADR-0004 as the human-readable
contract rationale and behavior guide. Do not generate API client functions in
this wave; keep the existing `fetch` wrapper.

**Ordered tasks:**

1. Review controller `[ProducesResponseType]` metadata and fill missing
   documented responses (including errors used by the contract); preserve the
   existing typed action results.
2. Define a local-only/reproducible generation command: run the API in
   Development and generate TypeScript schema types from its OpenAPI endpoint
   (recommended tool: `openapi-typescript`).
3. Commit generated types as generated output (clearly marked; never hand-edit)
   and replace duplicate `Task`, `TaskInput`, status, and login response shapes
   with generated schema types where applicable.
4. Keep `apiRequest` and the current API functions in place; this wave removes
   duplicated type declarations, not the application-level client abstraction.
5. Document the local regeneration command. No CI job or production OpenAPI
   endpoint is added.

**Acceptance criteria:**

- OpenAPI describes all current routes, success payloads, auth requirements,
  and contract error responses used by the SPA.
- Frontend task/auth payload types are derived from generated OpenAPI types;
  no parallel handwritten copy of the same wire shape remains.
- Regeneration is deterministic and the generated TypeScript passes the
  frontend type-check/build and existing tests.
- OpenAPI remains unavailable in Production unless separately approved.

**Risks / trade-offs:** Generated types can give false confidence if controller
metadata is incomplete or the API document is stale. The documented local
generation command is mandatory; because CI is deferred, a developer must run
it when changing API DTOs/routes. **Estimate:** small-to-medium change.

### Wave 3 — Frontend resilience (A7)

**Scope:** Load route screens on demand and provide one user-safe fallback for
unexpected render errors. Do not change the existing query/mutation error
states, auth flow, or route behavior.

**Ordered tasks:**

1. Convert page imports in `AppRoutes` to `React.lazy` and wrap route content in
   `Suspense` with an accessible loading fallback.
2. Add a small top-level React error boundary around the route tree with a
   retry/reset path and a safe message; do not expose stack traces.
3. Keep API errors in their existing component-level handling; an error
   boundary does not catch rejected event handlers or arbitrary async requests.
4. Test the loading fallback, route rendering, and boundary fallback/recovery;
   inspect the production build output for route chunks and manually smoke the
   login → task list → edit navigation.

**Acceptance criteria:**

- Each route renders the same content and auth guard behavior as before.
- Route chunks are emitted and the initial bundle excludes non-initial screens.
- A render-time exception produces a usable fallback instead of a blank page;
  the user can retry or navigate/reload.
- Loading/error states are accessible and no API error behavior regresses.

**Risks / trade-offs:** The app is small; code splitting may not materially
improve performance. Record bundle sizes before/after and keep the change only
if the split is clear and the fallback provides real recovery value.
**Estimate:** small-to-medium change.

## Consequences

### Positive

- The dependency rule becomes an executable local test, not just a project
  diagram.
- The API's types become reusable by the frontend without replacing the API
  client or adding a client-generation framework.
- Route-level loading and render failure have explicit, testable UX.
- Deferred architecture choices are written down with triggers for revisiting.

### Negative / trade-offs

- One more test project and an architecture-test dependency.
- OpenAPI type generation adds a local step whenever API shapes change; no
  automatic CI guard exists while pipeline work is deferred.
- Lazy loading and an error boundary add frontend code and may have negligible
  performance impact in a small SPA.
- Result pattern, API versioning, value objects, refresh tokens, and custom
  correlation middleware remain deferred; this is intentional scope control.

## Explicitly out of scope

- GitHub Actions or any new CI/CD pipeline, security scan, deploy promotion,
  automatic rollback, or build-once artifact workflow. The owner deferred these
  because this is a portfolio project, not a production service.
- Replacing SQLite, adding a production database, or changing Render's current
  deploy-on-push behavior.
- Rewriting all exception flow to `Result<T>`, adding `/api/v1`, or introducing
  domain value objects.
- Refresh-token persistence/rotation/revocation.
- Full generated API client, frontend framework migration, or a broad folder
  reorganization.

## Open questions

1. **Wave 2 — resolved:** The project owner chose committed/versioned generated
   TypeScript output plus a documented local regeneration command; see
   `specs/005-openapi-types/spec.md`.
2. **Wave 3:** Is the measured initial bundle reduction meaningful enough to
   retain route splitting? Compare actual build output before/after.

These questions do not block drafting the specs; answer them at the beginning
of their respective waves.

## Links

- `docs/adr/0001-clean-architecture.md`
- `docs/adr/0002-auth-strategy.md`
- `docs/adr/0004-api-contract.md`
- `docs/adr/0005-frontend-architecture.md`
- `specs/constitution.md` §2
- `specs/README.md` §1-2

## Change log

| Date | Change | Why |
| --- | --- | --- |
| 2026-11-18 | Initial proposal: validated A1–A8 against the current code; adopted A1/A2/A7 as ordered waves; recorded explicit deferrals for A3–A6/A8 and CI/CD. | Owner requested architectural analysis and documentation before implementation; implementation is not authorized by this proposal alone. |
| 2026-11-18 | Accepted by the project owner. | The owner approved the ADR and the A1 → A2 → A7 order; implementation remains gated on approved specs and ordered tasks. |
| 2026-11-18 | Selected `NetArchTest.Rules` 1.3.2 for Wave 1 after a temporary .NET 10.0.404 restore and rule-execution smoke test passed against the real Domain assembly. | The package compatibility question is resolved; the future test project still needs its own integration test. |
| 2026-11-18 | Closed the Wave 2 generated-types storage question: commit/version generated TypeScript and document local regeneration; linked SPEC-005. | Project owner decision at Wave 2/A2 launch; implementation remains gated on separate SPEC-005 approval. |
