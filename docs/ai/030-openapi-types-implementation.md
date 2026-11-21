# AI Evidence Log

## Entry 030

- **Date:** 2026-11-21
- **Agent:** orchestrator / GPT-6 Luna (after @codificador quota failure)
- **Task:** Implement SPEC-005 T-005-01 through T-005-04 (OpenAPI contract
  metadata, generated TypeScript types, frontend adoption).
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404; Node v26.13.1;
  npm 11.20.0.

## Prompt

Faithful summary: the project owner approved SPEC-005 and asked to start
implementation. The @codificador delegation failed immediately with the provider
error `OpenCode Go weekly usage limit reached...` and changed no files. The owner
then authorized Luna to implement. Scope: tests first, metadata-only API fixes,
committed generated TypeScript with a documented local regeneration command, and
frontend adoption without replacing `apiRequest`/`fetch`. No CI, no commit, no
runtime behavior change.

## Output summary

- Added `project/tests/CrudWorks.Api.Tests/Pipeline/OpenApiContractTests.cs` with
  nine semantic assertions covering the SPEC-005 acceptance criteria.
- Added `project/src/CrudWorks.Api/OpenApi/ContractOpenApiTransformers.cs` and
  registered document/operation transformers in `Program.cs`: health operation,
  bearer scheme/requirements, ProblemDetails extensions, string status enum,
  request requiredness/nullability, `Location` on 201, generic 500 responses.
- Added the isolated generator toolchain
  `project/frontend/tools/openapi-typescript/` (openapi-typescript 7.13.0 +
  TypeScript 5.9.3, own lockfile), the `generate:api-types` script, and the
  committed generated declaration `src/api/generated/openapi.d.ts`.
- Replaced handwritten task/auth wire types with generated aliases in
  `src/lib/tasks.ts`, `src/auth/AuthProvider.tsx`, `src/auth/authContext.ts`,
  and `src/auth/session.ts`; updated `TaskFormPage` to `TaskUpdateInput`.
- Documented regeneration in `project/frontend/README.md`.

## Validation

- [x] Red (T-005-01), before metadata:
  `dotnet test tests/CrudWorks.Api.Tests/CrudWorks.Api.Tests.csproj --filter "FullyQualifiedName~OpenApiContractTests" --verbosity minimal`
  → **8 failed, 1 passed**: 8 operations instead of 9; missing health, bearer,
  ProblemDetails extensions, string enum, request requiredness, Location, 500.
- [x] Green (T-005-02), after metadata: same command → **10/10 passed**.
- [x] Full backend suite: `dotnet test CrudWorks.slnx --verbosity minimal` →
  Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 65
  (**190 passed, 0 failed**).
- [x] Build: `dotnet build CrudWorks.slnx --verbosity minimal` → 0 warnings,
  0 errors.
- [x] Generation (T-005-03): `npm run generate:api-types` twice against the
  corrected Development document → identical SHA256
  `054F401FDFECE5BD822FC0C9EF212BBEE21CF02B57175F440D2D05C2ADA8911E`.
- [x] Frontend (T-005-04): `npm run build` succeeded; `npm run lint` clean;
  `npm run test` → **11 files, 88/88 tests passed**.
- [x] Production non-exposure remains covered by
  `OpenApiContractTests.Production_DoesNotServeTheOpenApiDocument`.
- [x] No CI workflow, no commit, no staging.

## Corrections

| What was attempted or suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Install `openapi-typescript@7.13.0` as a normal frontend devDependency | It declares a TypeScript `^5.x` peer; the app uses TypeScript 7 and the CLI fails with `ts.factory` undefined | Isolated the generator in `tools/openapi-typescript/` with TypeScript 5.9.3 and its own lockfile; the app keeps TypeScript 7 | Keep the generator locked/reproducible without downgrading the application toolchain |
| Reference the `ProblemDetails` schema with `JsonSchemaReference` in the 500 response | `JsonSchemaReference` does not implement `IOpenApiSchema` in Microsoft.OpenApi 2.13 | Inlined the ProblemDetails schema in the generic 500 response metadata | Compile with the actual 2.13 object model without changing runtime behavior |
| Test asserted a single `object` type for `ProblemDetails.errors` | The generated schema correctly exposes `null | object` because the field is optional | Assertion accepts `object` within the type set | Test the contract, not an over-strict representation |

## Edge cases considered

- Health is documented as the existing `200` response only; no DB probe and no
  unhealthy response were invented.
- The document transformer adds the health path because `MapHealthChecks` is not
  a controller operation; no runtime endpoint changed.
- Bearer security applies only to `/api/tasks*` and `/api/auth/me`; register,
  login, and health stay anonymous.
- `TaskItemStatus` and the request `status` fields are string enums matching the
  runtime `JsonStringEnumConverter`.
- `201` responses carry `Location`; generic `500` responses use
  `application/problem+json`.
- Generated declarations are compile-time only; no runtime validation was added.
- Generated output is deterministic for the same document; no CI freshness check
  exists (deferred).

## Auth and validation decisions

- No authentication, authorization, validation, route, or status-code behavior
  changed.
- OpenAPI remains Development-only.
- `apiRequest`/`fetch` remains the client; no generated client was added.
- The generator's TypeScript 5.x isolation is a tooling decision, recorded in
  SPEC-005's change log.

## Files affected

- `project/src/CrudWorks.Api/OpenApi/ContractOpenApiTransformers.cs`
- `project/src/CrudWorks.Api/Program.cs`
- `project/tests/CrudWorks.Api.Tests/Pipeline/OpenApiContractTests.cs`
- `project/frontend/package.json`
- `project/frontend/tools/openapi-typescript/package.json`
- `project/frontend/tools/openapi-typescript/package-lock.json`
- `project/frontend/src/api/generated/openapi.d.ts`
- `project/frontend/src/lib/tasks.ts`
- `project/frontend/src/auth/AuthProvider.tsx`
- `project/frontend/src/auth/authContext.ts`
- `project/frontend/src/auth/session.ts`
- `project/frontend/src/pages/TaskFormPage.tsx`
- `project/frontend/README.md`
- `specs/005-openapi-types/spec.md`
- `specs/005-openapi-types/tasks.md`

## Final decision

**Implementation complete for T-005-01..T-005-04; verification pending.**
T-005-05 (independent full verification and review) remains with Luna/QA. No
commit or CI work was performed.
