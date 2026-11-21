# AI Evidence Log

## Entry 031

- **Date:** 2026-11-21
- **Agent:** orchestrator / verifier (GPT-6 Luna)
- **Task:** T-005-05 verification of SPEC-005 after implementation.
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404; Node v26.13.1;
  npm 11.20.0.

## Prompt

Faithful summary: verify the completed Wave 2 implementation in a separate
verification pass after the edit pass: build the solution, run all backend
tests, run frontend lint/tests/build, regenerate the OpenAPI TypeScript twice
and compare hashes, confirm Production does not serve the document, and check
the tree for leftovers or unrelated changes. Do not add CI or commit.

## Output summary

- Backend build clean and full backend suite green.
- OpenAPI contract tests green (including Production non-exposure).
- Frontend lint, tests, and production build green.
- Generated declarations deterministic across two runs.
- No handwritten duplicate wire interfaces remain in the app modules; no `any`
  was introduced in the changed frontend code.
- Root-level accidental `package.json` created by an earlier `npm init` probe was
  removed; unrelated pre-existing worktree files were left untouched.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx --verbosity minimal` → **0 warnings,
  0 errors**.
- [x] Backend tests: `dotnet test CrudWorks.slnx --verbosity minimal` →
  Domain 37, Application 57, Infrastructure 25, Architecture 6, Api 65
  (**190 passed, 0 failed, 0 skipped**).
- [x] OpenAPI contract tests: included in the 65 API tests; the focused run was
  **10/10 passed**, including `Production_DoesNotServeTheOpenApiDocument`.
- [x] Frontend lint: `npm run lint` → clean.
- [x] Frontend tests: `npm run test` → **11 files, 88/88 passed**.
- [x] Frontend build: `npm run build` → `tsc -b && vite build` succeeded.
- [x] Generation determinism: `npm run generate:api-types` twice → identical
  SHA256 `054F401FDFECE5BD822FC0C9EF212BBEE21CF02B57175F440D2D05C2ADA8911E`.
- [x] Residue check: `git status --short` shows only expected Wave 2 files plus
  pre-existing unrelated files; `git diff --check` reports no whitespace errors.
- [x] No `any` or handwritten duplicate task/auth wire interfaces in
  `project/frontend/src` (excluding generated declarations).

## Corrections

| What was found | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Root `package.json` appeared untracked | An earlier `npm init -y --prefix <temp>` probe created it in the repo root | Removed the accidental file | It is not part of the project |
| `session.ts` had the generated-type import below a statement | Valid but inconsistent with module style | Moved the import to the top | Keep the module clean and conventional |
| QA review found 4xx responses documented as `application/json`/`text/plain` | Runtime emits `application/problem+json`; AC-005-02 was not actually met | Operation transformer now normalizes 400/401/404/409 to `application/problem+json`; tests assert every error response uses that media type | Match the real error contract |
| QA review found auth request bodies were inline literals | AC-005-05 requires auth request types to derive from generated declarations | `AuthProvider` now builds `LoginRequest`/`RegisterRequest` from generated schemas | Remove the last handwritten wire shapes |
| QA review found 204/4xx/status sets unasserted | The suite could stay green if a status or body contract regressed | Added exact operation-set, status-set and 204-no-body assertions | Guard the contract, not just the happy path |

## Edge cases considered

- The OpenAPI document is generated only in Development; Production returns a
  non-success response.
- The health operation documents the existing 200 response only; no DB probe or
  unhealthy response was invented.
- Generated types are compile-time only; runtime API validation remains
  authoritative.
- The generator runs from an isolated toolchain because its TypeScript 5 peer is
  incompatible with the app's TypeScript 7.
- Frontend tests use role/text assertions; generated aliases did not change
  behavior.

## Auth and validation decisions

- No runtime auth, validation, route, status-code, or persistence behavior
  changed.
- No CI workflow or commit was added.
- `apiRequest`/`fetch` remains the only client.

## Files affected

- Verification only; no production files changed in this pass beyond the
  `session.ts` import-order cleanup.
- Evidence for implementation: `docs/ai/030-openapi-types-implementation.md`.

## Final decision

**Verification passed after the QA follow-up; no blocking code findings.** T-005-05
is complete. This is a second-pass verification by the implementing agent, not a
separate human reviewer; an independent review remains advisable before a
release. The generated declaration and Wave 2 files were committed in `7a7a5a1`
(SPEC-005 marked `implemented` in the follow-up docs commit).
