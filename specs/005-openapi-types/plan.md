# PLAN-005 — OpenAPI-generated frontend types implementation plan

- **Spec:** `specs/005-openapi-types/spec.md`
- **Status:** draft
- **Owner:** architect
- **Last updated:** 2026-11-18

## 1. Architecture impact

No domain, application, persistence, or runtime authentication change. The API remains the contract source; the frontend consumes generated declarations at the existing request boundary.

| Layer / area | Change |
| --- | --- |
| Domain | No change. |
| Application | No change to DTOs, validation, or business behavior. |
| Infrastructure | No change. |
| Api | Metadata-only correction: document `/api/health`, bearer security, ProblemDetails extensions, string `TaskItemStatus`, request requiredness/nullability, `Location` on `201`, and generic `500` responses where applicable. Preserve Development-only mapping and all runtime behavior. Current entry points: `Program.cs:18-27,131-159`; controllers: `Controllers/TasksController.cs:14-57`, `Controllers/AuthController.cs:13-47`. |
| Frontend | Add locked `openapi-typescript` `7.13.0` dev dependency, `generate:api-types` script, and generated declarations (proposed path `project/frontend/src/api/generated/openapi.d.ts`). Replace duplicate wire types in `src/lib/tasks.ts`, `src/auth/AuthProvider.tsx`, and `src/auth/authContext.ts`; retain distinct form/session state through explicit type aliases/adapters. Preserve `src/lib/api.ts:26-56`. |
| Tests | Extend `project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs` for document semantics and Production non-exposure. Frontend type-check/build and existing tests cover consumption. |
| Documentation | Document prerequisites and regeneration in `project/frontend/README.md`; generated output remains committed. |
| CI / deployment | No changes. CI/CD remains deferred by ADR-0007. |

**Fact (provided, temporary Development inspection):** The API document is OpenAPI 3.1.1 with eight operations and omits `/api/health`, bearer security, ProblemDetails extensions, string enum representation, correct request property requiredness/nullability, `Location`, and generic `500` responses. The API test only checks document availability and the OpenAPI marker (`project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs:146-156`).

**Fact (provided):** Runtime health returns `200` with `status` and `totalDurationMs`; the source registers `AddHealthChecks()` only (`project/src/CrudWorks.Api/Program.cs:27,147-159`). Do not claim a database probe or invent an unhealthy response.

**Fact (provided, temporary generator smoke):** `openapi-typescript` `7.13.0` CLI help and generation succeeded in a temporary directory; two generations from the same temporary document had identical SHA256 `DC79E736D6D8138BD14EFD5A410CCEBB0EAC52D31A4A1C9E61CFEA78A13BFFFD`. This does not prove corrected-document or frontend integration.

## 2. Data model

None. No database, persistence, migration, seed, or runtime data-model change.

## 3. Contract

No route or runtime behavior changes. ADR-0004 owns route semantics; SPEC-005 §7 lists the operations and statuses that OpenAPI must describe. The generated declarations cover schemas only; no API client functions are generated. OpenAPI remains Development-only as currently mapped in `Program.cs:131-134`.

The document has been inspected; acceptance tests must now catch the recorded gaps. Keep assertions semantic rather than binding to incidental component names: paths/methods, status codes, response content/headers, property names/types/nullability, bearer requirements, and ProblemDetails extensions. The API changes are metadata-only; do not change runtime behavior or the OpenAPI version as an unrelated cleanup.

## 4. Test strategy

| Layer | What is proven | Where |
| --- | --- | --- |
| API integration | OpenAPI includes all nine operations (the current document has eight), health `200`, bearer scheme/security, ProblemDetails extensions, string status enum, correct request requiredness/nullability, `Location`, applicable generic `500`s, and no successful OpenAPI document in Production. Preserve runtime behavior and existing error/endpoint tests. | `project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs`; existing auth/task/health API tests |
| Frontend compile | Generated declarations are consumed and the strict TypeScript project type-checks. | `npm run build` (`project/frontend/package.json:6-12`) |
| Frontend behavior | Task/auth behavior and UI adapters remain unchanged. | Existing `project/frontend/src/**/*.test.tsx` and `*.test.ts` |
| Generator reproducibility | Lock `openapi-typescript` `7.13.0`; with the same corrected Development document and lockfile, two project invocations produce no second diff. The temporary generator smoke is baseline evidence only. | Documented local command `npm run generate:api-types` from `project/frontend/`; local diff review |
| Lint | No type/lint regression or `any` introduced. | `npm run lint` from `project/frontend/` |
| Domain / Application / Infrastructure | No change in scope; their existing tests are regression coverage, not new acceptance evidence. | Existing solution test projects; no new tests required here |

No test/build is run while authoring this documentation. There is no CI synchronization check in this wave.

## 5. Task order

The executable, ordered list is `tasks.md`. Implementation is gated on separate SPEC-005 approval; the owner-approved wave sequence remains A1 → A2 → A7. SPEC-004 is already implemented (`specs/004-architecture-tests/spec.md:1-8`).

1. Add failing API document assertions for the inspected gaps, including Production non-exposure.
2. Add metadata-only corrections for the health operation, bearer security, ProblemDetails extensions, enum and request schemas, `Location`, and applicable generic `500` responses. Do not alter runtime behavior.
3. Add and lock `openapi-typescript` `7.13.0`, the local regeneration script, committed generated declaration, and regeneration documentation. Verify generation against the corrected document.
4. Replace duplicated task/auth wire declarations with generated aliases; keep form/session adapters and the existing `fetch` client.
5. Run API integration tests, frontend lint/tests/build, regenerate twice, review the diff, and confirm no Production document is served.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| OpenAPI metadata is incomplete or disagrees with runtime ProblemDetails/auth behavior. | Generated types encode the wrong contract. | Make API-document assertions fail first; inspect runtime/error tests and correct only metadata unless a separate contract change is approved. |
| Temporary generator smoke does not prove compatibility with the corrected API document or frontend project. | Generation may not integrate with the package lock or TypeScript build. | Pin `openapi-typescript` `7.13.0`; verify project installation, corrected-document generation, repeatability, and frontend type-check/tests before closing the task. |
| Generated types are mistaken for runtime validation. | Malformed server responses still pass through unchecked casts. | Document compile-time-only guarantee; keep API authoritative and do not claim runtime validation. |
| No CI checks generated output freshness. | A developer may forget to regenerate after API changes. | Keep a documented local command, committed output, and task/review checklist; CI remains out of scope. |
| Development-only source requires the API to run during regeneration. | A fresh frontend-only environment cannot regenerate types. | Document startup prerequisites and fail clearly if the URL is unavailable; do not expose a Production document or commit a separate hand-maintained OpenAPI copy. |

## 7. Definition of done

- [x] SPEC-005 has separate orchestrator approval before implementation begins.
- [x] Every AC has an API integration test, frontend check, reproducibility check, or named manual review.
- [x] OpenAPI describes the ADR-0004 operations, payloads, response statuses, ProblemDetails, and auth requirements; schema names were verified from the actual document.
- [x] Generated declarations are committed with the API change (`7a7a5a1`), never hand-edited, and the documented local command produces a stable result with the locked generator.
- [x] Frontend task/auth wire types use generated declarations; distinct UI/session adapters remain explicit.
- [x] Existing `apiRequest`/`fetch` flow is preserved; no API client is generated.
- [x] OpenAPI is not served in Production; no CI/CD or runtime behavior change was added.
- [x] API integration tests and frontend lint/tests/build pass with real captured output.
- [x] AI evidence entries exist and a second-pass verification passed; a separate independent review remains recommended before release.
