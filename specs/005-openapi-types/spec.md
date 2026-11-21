# SPEC-005 — OpenAPI-generated frontend types

- **Status:** implemented
- **Owner:** architect
- **Approved by:** project owner
- **Requirements:** R-09, R-14, R-17, R-27
- **ADRs:** 0004, 0005, 0007
- **Last updated:** 2026-11-21

## 1. Problem

The API already exposes OpenAPI in Development, but the frontend separately declares request and response wire shapes. That duplication can drift from the API contract. Use the OpenAPI document as the source for frontend wire types while retaining the current fetch client and Development-only document exposure.

## 2. Scope

**In scope**

- Complete OpenAPI response, payload, and bearer-auth metadata for the current API contract.
- Generate and commit TypeScript schema declarations from the Development OpenAPI document; document a local regeneration command.
- Replace handwritten duplicate wire types in task/auth modules with generated types. Keep UI/form and session adapters where they serve a distinct purpose.
- Verify OpenAPI contract coverage, generation reproducibility, frontend type-checking, and Production non-exposure.

**Out of scope**

- Generating API client functions or replacing `fetch`/`apiRequest`.
- Changing routes, runtime status codes, authentication, validation, UI behavior, or domain rules.
- Exposing OpenAPI in Production, adding CI/CD, or reorganizing the frontend.
- Runtime response validation: generated declarations provide compile-time types only.

## 3. Actors and glossary

| Term | Meaning |
| --- | --- |
| Wire type | A request or response shape crossing the HTTP boundary. |
| Generated declarations | Committed TypeScript declarations generated from the API OpenAPI document; not hand-edited. |
| Adapter | A frontend-local form/session representation kept distinct from the HTTP contract. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-005-01 | As a frontend maintainer, I want wire types generated from the API contract so that API changes do not silently leave handwritten duplicates behind. |
| US-005-02 | As a reviewer, I want the generated output committed with a local regeneration command so that I can inspect and reproduce contract changes without CI. |

## 5. Acceptance criteria

### AC-005-01 — OpenAPI documents the API contract

- **Given** the API runs in Development and the API integration test reads its OpenAPI document
- **When** the document is parsed against ADR-0004
- **Then** it contains all nine operations in §7, including `GET /api/health` with its existing `200` response schema (`status` string and `totalDurationMs` number), plus the route/path/request/response schemas and success/error statuses in ADR-0004.
- **And** the `201` register/create responses describe the `Location` header, generic `500` responses are documented where applicable, and `204` has no response body.
- **And** no health failure response or database probe is invented; A2 documents the existing healthy response only.

### AC-005-02 — Security and errors are represented accurately

- **Given** the OpenAPI document contains the routes in AC-005-01
- **When** an API integration test inspects operation security and error responses
- **Then** `components.securitySchemes` describes bearer authentication and task routes plus `GET /api/auth/me` declare it; register, login, and health have no operation security requirement.
- **And** documented API errors use `application/problem+json` and describe ProblemDetails fields plus `traceId` and optional `errors: Record<string, string[]>`.
- **And** task status is a string enum (`Todo`, `InProgress`, `Done`), matching the runtime JSON representation.

### AC-005-03 — Request schemas reflect contract requiredness and nullability

- **Given** the API integration test reads the OpenAPI request schemas
- **When** it compares required properties and nullability with ADR-0004
- **Then** create-task requires `title` while `description`, `status`, and `dueDate` are optional; register and login fields are required and non-null; and task-update follows full replacement with required `title` and `status`.
- **And** nullable/clearable task fields match the contract, including `description` and `dueDate` clearing on update.

### AC-005-04 — Generated output is committed and reproducible

- **Given** the API is running at `http://localhost:5080` in Development and frontend dependencies are installed
- **When** a developer runs the documented `npm run generate:api-types` command from `project/frontend/` twice against the same OpenAPI document
- **Then** the generated declaration file is refreshed at the documented path, is committed/versioned, and the second generation leaves it unchanged.
- **And** the command uses the local locked `openapi-typescript` `7.13.0` dependency and fails visibly if its input document is unavailable.

### AC-005-05 — Frontend consumes generated wire types without replacing its client

- **Given** generated declarations match the current API document
- **When** the frontend type-check/build and existing frontend tests run
- **Then** task and auth HTTP request/response types derive from generated declarations, no parallel handwritten copy of those wire shapes remains, and UI/form/session adapters remain only where they represent distinct frontend state.
- **And** requests continue through the existing `apiRequest`/`fetch` path; no generated API client is introduced.

### AC-005-06 — OpenAPI remains unavailable in Production

- **Given** the API is hosted with the Production environment
- **When** a request is made to the Development OpenAPI document URL
- **Then** the response is not a successful OpenAPI document response.
- **And** the Development document remains available for local regeneration.

## 6. Rules and validation

| Field / rule | Constraint | Behavior on violation |
| --- | --- | --- |
| API operations | Match the routes, methods, auth, payloads, and statuses in ADR-0004. | OpenAPI integration assertion fails. |
| Error response | `application/problem+json`; core ProblemDetails fields, `traceId`, optional `errors: Record<string, string[]>`; generic unexpected failure is safe and documented. | OpenAPI integration assertion fails; runtime error behavior remains governed by ADR-0004. |
| Request schemas | Required, optional, and nullable properties reflect ADR-0004; task create/update semantics remain distinct. | OpenAPI integration assertion fails; do not change runtime validation. |
| Enum schema | Task status is a string enum with the three contract values. | OpenAPI integration assertion fails; runtime JSON behavior remains unchanged. |
| Generated declarations | Generated from the Development document, committed, never hand-edited, and reproducible using locked `openapi-typescript` `7.13.0`. | Generation check/diff fails; regenerate from the API document. |
| Frontend wire types | Use generated declarations; form and persisted-session validation remain frontend concerns. | Frontend type-check or behavior tests fail; do not weaken types with `any`. |
| Exposure | OpenAPI document is Development-only. | Production integration check fails if a document is served. |

## 7. API contract

ADR-0004 remains the behavioral contract; this spec adds no route. The OpenAPI document must reflect these operations and the response payloads defined there. Unexpected `500` errors use the generic ProblemDetails contract where applicable.

| Method | Route | Auth | Success | Contract errors |
| --- | --- | --- | --- | --- |
| GET | `/api/health` | Anonymous | `200` health resource | No additional error response defined in ADR-0004 |
| POST | `/api/auth/register` | Anonymous | `201` user + `Location` | `400`, `409`, `500` |
| POST | `/api/auth/login` | Anonymous | `200` token + user | `400`, `401`, `500` |
| GET | `/api/auth/me` | Bearer | `200` user | `401`, `500` |
| GET | `/api/tasks` | Bearer | `200` task array | `401`, `500` |
| GET | `/api/tasks/{id}` | Bearer | `200` task | `401`, `404`, `500` |
| POST | `/api/tasks` | Bearer | `201` task + `Location` | `400`, `401`, `500` |
| PUT | `/api/tasks/{id}` | Bearer | `200` task | `400`, `401`, `404`, `500` |
| DELETE | `/api/tasks/{id}` | Bearer | `204`, no body | `401`, `404`, `500` |

## 8. Non-functional requirements

- **Performance:** Generation is a local developer step; no runtime request is added.
- **Security:** No auth behavior changes; do not expose the OpenAPI document in Production.
- **Accessibility:** No UI changes.
- **Observability:** Generation failures must be visible to the developer; no new runtime logging.
- **Reproducibility:** The generator dependency is locked, generated output is committed, and CI remains deferred.

## 9. Dependencies and assumptions

- **Fact (provided):** Wave 1/SPEC-004 is implemented and verified. `SPEC-004` is marked implemented (`specs/004-architecture-tests/spec.md:1-8`).
- **Fact:** `AddOpenApi` is registered and `MapOpenApi` is inside a Development check (`project/src/CrudWorks.Api/Program.cs:24,131-134`); an existing API test checks document availability at `/openapi/v1.json` (`project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs:146-156`).
- **Fact (provided, temporary Development inspection):** At `http://127.0.0.1:5218`, the generated document is OpenAPI 3.1.1 with eight operations; `/api/health` is absent. No operation has `security`, no `components.securitySchemes` exists, ProblemDetails omits `traceId`/`errors`, `TaskItemStatus` is integer, and request requiredness/nullability conflicts with ADR-0004. `201` omits `Location`, and generic `500` responses are absent. This is a temporary runtime inspection, not a checked-in document.
- **Fact:** The code registers typed controller responses, but metadata is incomplete against the above inspection (`project/src/CrudWorks.Api/Controllers/TasksController.cs:14-57`, `project/src/CrudWorks.Api/Controllers/AuthController.cs:13-47`). The health route is mapped separately (`project/src/CrudWorks.Api/Program.cs:147-159`).
- **Fact (provided):** Runtime `GET /api/health` returned `200` with `status` and `totalDurationMs`. The code registers `AddHealthChecks()` (`project/src/CrudWorks.Api/Program.cs:27`) and maps the response (`project/src/CrudWorks.Api/Program.cs:147-159`); the provided search found no `AddCheck`, `AddDbContextCheck`, or `IHealthCheck` registration under `project/src`. A2 must not claim that health probes the database or invent an unhealthy response.
- **Fact:** Runtime errors use `ProblemDetails`, add `traceId`, and add `errors` only when supplied (`project/src/CrudWorks.Api/ErrorHandling/ApiProblemDetails.cs:8-30`); validation/conflict/auth/not-found and unexpected failures are mapped centrally (`project/src/CrudWorks.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs:22-57`).
- **Fact:** Runtime JSON uses string enums (`project/src/CrudWorks.Api/Program.cs:18-22`); the temporary OpenAPI document incorrectly describes `TaskItemStatus` as integer.
- **Fact:** Task/status, task input, and login response shapes are handwritten in the frontend (`project/frontend/src/lib/tasks.ts:4-24`, `project/frontend/src/auth/AuthProvider.tsx:11-15`); the register response is also declared locally (`project/frontend/src/auth/authContext.ts:4-9`).
- **Fact:** `apiRequest` owns the current `fetch` call and accepts caller-supplied TypeScript generics (`project/frontend/src/lib/api.ts:26-56`). Frontend scripts currently include build, lint, and tests, but no generation command (`project/frontend/package.json:6-12`).
- **Fact:** The documented Development API URL is `http://localhost:5080` (`project/README.md:28-39`).
- **Fact (provided, temporary generator smoke):** With Node `v26.13.1` and npm `11.20.0`, `npm view openapi-typescript@latest version` returned `7.13.0`. CLI help and generation succeeded using `npm exec --yes --package=openapi-typescript@7.13.0 -- openapi-typescript <input> --output <temp-output>`. Two runs against the same temporary Development document produced SHA256 `DC79E736D6D8138BD14EFD5A410CCEBB0EAC52D31A4A1C9E61CFEA78A13BFFFD`. This validates the temporary CLI/determinism smoke only, not a corrected API document or frontend integration.
- **Decision (owner-approved):** Commit generated declarations and document local regeneration; keep `apiRequest`; use the Development OpenAPI document; do not add CI/CD or expose OpenAPI in Production.
- **Decision for implementation:** Pin `openapi-typescript` `7.13.0` as a frontend development dependency and lock it. The smoke result does not establish frontend integration.

## 10. Open questions

| Question | Owner | Deadline | Resolution |
| --- | --- | --- | --- |
| Can the observed OpenAPI defects be corrected with metadata only, preserving all runtime behavior? | Backend implementer | Before T-005-02 | Add semantic API-document assertions first; correct metadata only and keep existing runtime tests unchanged. |
| Does generated output from the corrected document integrate with the frontend when `openapi-typescript` `7.13.0` is locked? | Frontend implementer | Before T-005-04 | Verify project dependency/lockfile integration, regenerate twice, then type-check and run frontend tests; the temporary smoke is not sufficient evidence. |

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-18 | Initial draft for Wave 2/A2: defined OpenAPI coverage, committed generated declarations, local regeneration, frontend consumption, and Production non-exposure. | Owner launched Wave 2 with a separate spec-approval gate; no implementation is authorized by this draft. | — |
| 2026-11-21 | Approved SPEC-005 and authorized Wave 2 implementation. | The project owner accepted the metadata-only scope, the versioned generated types decision, and the two implementation gates recorded in §10. | Project owner |
| 2026-11-21 | Recorded the generator-toolchain deviation: `openapi-typescript` 7.13.0 is isolated under `project/frontend/tools/openapi-typescript/` with TypeScript 5.9.3 and its own lockfile. | The app uses TypeScript 7; the generator's TypeScript 5 peer is incompatible at runtime (`ts.factory` undefined). The generator remains locked and reproducible without downgrading the app. | Orchestrator (technical deviation; no contract or behavior change) |
| 2026-11-21 | Implemented and verified Wave 2; committed in `7a7a5a1`. | Metadata-only OpenAPI corrections, including `application/problem+json` for all 4xx/5xx responses; 10/10 contract tests; 190/190 backend tests; generated declarations deterministic and committed; frontend build/lint/88 tests green; Production non-exposure verified. | Luna verification + QA follow-up |
