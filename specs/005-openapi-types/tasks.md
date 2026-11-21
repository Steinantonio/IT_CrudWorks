# TASKS-005 — OpenAPI-generated frontend types

- **Spec:** `specs/005-openapi-types/spec.md`
- **Plan:** `specs/005-openapi-types/plan.md`
- **Status:** implemented — committed in `7a7a5a1`
- **Last updated:** 2026-11-21

## Status legend

`todo` · `red` (failing test written) · `green` (test passes) · `done` (refactored,
verified, documented) · `blocked`

## Tasks

| ID | Task | Owner | Depends on | Test / evidence | Status |
| --- | --- | --- | --- | --- | --- |
| T-005-01 | After SPEC-005 approval, add failing API-document assertions for the observed baseline: nine operations including health; bearer scheme/security; ProblemDetails extensions; string TaskItemStatus; request requiredness/nullability; 201 Location; applicable generic 500 responses; Production non-exposure. | Backend implementer; QA reviews | SPEC-005 approval; A1/SPEC-004 complete | Red run captured: 8 failed / 1 passed on the old document (`Document_ContainsAllNineOperationsIncludingHealth` expected 9, actual 8; missing health/security/ProblemDetails fields/enum/requiredness/Location/500). New `OpenApiContractTests.cs` covers all nine criteria. | done |
| T-005-02 | Correct OpenAPI metadata only: include `/api/health` with its existing 200 body; describe bearer auth, ProblemDetails extensions, string enum, contract request schemas, Location, and applicable generic 500 responses. Do not add a DB health probe, invent an unhealthy health response, or change runtime behavior. | Backend implementer | T-005-01 | Added `project/src/CrudWorks.Api/OpenApi/ContractOpenApiTransformers.cs` and registered the document/operation transformers in `Program.cs`. Contract tests: 10/10 pass; full backend solution: 190/190 pass (was 180). Existing endpoint/error tests unchanged; 4xx/5xx now documented as `application/problem+json`. | done |
| T-005-03 | Add and lock `openapi-typescript` `7.13.0` as a frontend dev dependency; add `npm run generate:api-types`, generated declaration output, and local regeneration instructions. The temporary smoke verified CLI syntax only; verify it against the corrected Development document. Commit/version generated output; never hand-edit it. | Frontend implementer | T-005-02; SPEC-005 approval | Isolated tool package `project/frontend/tools/openapi-typescript/` with locked `openapi-typescript` 7.13.0 + TypeScript 5.9.3 (TS7 incompatibility, see Notes). `npm run generate:api-types` produced `src/api/generated/openapi.d.ts` twice with identical SHA256 `054F401FDFECE5BD822FC0C9EF212BBEE21CF02B57175F440D2D05C2ADA8911E`; regeneration documented in `project/frontend/README.md`. | done |
| T-005-04 | Replace duplicate task/auth HTTP wire types with generated declaration aliases. Keep the existing `apiRequest`/`fetch` client and explicit, distinct form/session adapters. | Frontend implementer | T-005-03 | `src/lib/tasks.ts`, `auth/AuthProvider.tsx`, `auth/authContext.ts`, and `auth/session.ts` now alias generated schemas; `TaskFormPage` uses `TaskUpdateInput`; `apiRequest`/`fetch` unchanged. `npm run build`, `npm run lint`, `npm run test` → 11 files, 88/88 tests pass; no `any`. | done |
| T-005-05 | Run API integration tests and frontend lint/tests/build; regenerate types, inspect the final diff, and verify the document is unavailable as an OpenAPI document in Production. Record outputs and request independent review. Do not add CI/CD. | Backend + frontend implementers; QA verifies | T-005-04 | Luna verification: `dotnet build CrudWorks.slnx` 0 warnings/0 errors; `dotnet test CrudWorks.slnx` 190/190; `npm run lint` clean; `npm run test` 88/88; `npm run build` succeeded; two regenerations identical SHA256 `054F401FDFECE5BD822FC0C9EF212BBEE21CF02B57175F440D2D05C2ADA8911E`; Production non-exposure test included in 10/10 OpenAPI tests; no `any`; committed in `7a7a5a1`; no CI. | done |

## Notes

- SPEC-005 was approved by the project owner on 2026-11-21; T-005-01..T-005-04 are done and T-005-05 awaits independent verification.
- **Deviation (documented):** `openapi-typescript` 7.13.0 declares a TypeScript `^5.x` peer and fails under the app's TypeScript 7 (`ts.factory` is undefined). The generator is therefore isolated in `project/frontend/tools/openapi-typescript/` with its own lockfile and TypeScript 5.9.3; the app keeps TypeScript 7.
- **Fact (provided):** Temporary Development inspection found OpenAPI 3.1.1 with eight operations, missing `/api/health`, security scheme/requirements, ProblemDetails extensions, correct enum/request schemas, `Location`, and generic `500` responses. T-005-01 encoded these observed gaps; T-005-02 fixed metadata only.
- **Fact (provided):** The existing test only asserts `/openapi/v1.json` returns an OpenAPI marker (`project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs:146-156`).
- **Fact (provided):** The generator CLI smoke succeeded with `openapi-typescript` 7.13.0 in a temporary directory and produced identical SHA256 on two runs (`DC79E736D6D8138BD14EFD5A410CCEBB0EAC52D31A4A1C9E61CFEA78A13BFFFD`). It did not change the project dependency or prove frontend integration.
- Runtime `GET /api/health` is documented as a successful 200 response only for this wave. No database probe exists in the provided source search; health probing is outside A2.
- The project script invokes the locked local `openapi-typescript` 7.13.0 CLI from the isolated tool package; the CLI form is `openapi-typescript <input> --output <output>`.
- The local API launch profile uses Development on `http://localhost:5080` (`project/src/CrudWorks.Api/Properties/launchSettings.json:4-12`).
- T-005-01..T-005-04 evidence includes real red/green output; no CI changes and no commit were made. See `docs/ai/030-openapi-types-implementation.md`.
