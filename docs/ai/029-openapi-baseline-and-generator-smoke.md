# AI Evidence Log

## Entry 029

- **Date:** 2026-11-18
- **Agent:** orchestrator / verifier
- **Task:** Resolve the two SPEC-005 investigation questions by inspecting the
  actual Development OpenAPI document and smoke-testing the proposed TypeScript
  generator, without changing repository code or dependencies.
- **Tool/model:** GPT-6 Luna (OpenCode); .NET SDK 10.0.404; Node v26.13.1;
  npm 11.20.0.

## Prompt

Faithful summary: the project owner approved (1) inspecting the actual
Development OpenAPI document and documenting current health behavior without
changing it, and (2) verifying the generator release/CLI before locking it.
The API was started against a temporary SQLite file, the OpenAPI document was
saved under the approved temp directory, and `openapi-typescript` was run twice
against that snapshot. No repo code, package, or generated file was changed.

## Output summary

- Observed the emitted OpenAPI 3.1.1 document: it contains eight operations,
  omits `/api/health`, and does not describe bearer security.
- Identified contract metadata mismatches: ProblemDetails lacks `traceId` and
  `errors`; `TaskItemStatus` is integer instead of the string enum used on the
  wire; request requiredness/nullability differs from ADR-0004; `201 Location`
  headers and generic `500` responses are not documented.
- Observed `/api/health` returning `200` with `status` and `totalDurationMs`.
  Source search found only `AddHealthChecks()` and no `IHealthCheck`,
  `AddCheck`, or `AddDbContextCheck` registration; this endpoint does not prove
  database reachability. Startup migration/seed remains a separate path.
- Confirmed `openapi-typescript` **7.13.0** runs under Node 26.13.1 / npm 11.20.0.
  CLI help succeeded, generation from the actual OpenAPI snapshot succeeded,
  and two runs produced the same SHA256.
- Corrected the earlier unsupported claim that `/api/health` checks the
  database in the local interview-prep and improvement-plan documents.

## Validation

- [x] Temporary API inspection: `powershell -NoProfile -File
  C:\Users\User\AppData\Local\Temp\opencode\inspect-openapi-wave2.ps1` started
  the built API on `127.0.0.1:5218` with a temporary SQLite database. The
  document request returned `200`; operation inventory count was **8**.
- [x] Health response observed: `HTTP 200`,
  `{"status":"Healthy","totalDurationMs":0.0374}` (duration is runtime-specific).
- [x] Generator package: `npm view openapi-typescript@latest version` returned
  `7.13.0`; `npm exec --yes --package=openapi-typescript@7.13.0 -- openapi-typescript --help`
  returned CLI usage and exit `0`.
- [x] Generation: `npm exec --yes --package=openapi-typescript@7.13.0 -- openapi-typescript <temp-openapi.json> --output <temp-openapi.d.ts>`
  succeeded twice. SHA256 both runs:
  `DC79E736D6D8138BD14EFD5A410CCEBB0EAC52D31A4A1C9E61CFEA78A13BFFFD`.
- [x] No repo package manifest, lockfile, generated declaration, or production
  source changed. The API was stopped by the temporary inspection script.
- [ ] No build or test suite was run; this was an OpenAPI/generator inspection,
  not implementation.

## Corrections

| What the AI previously claimed or first tried | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| `/api/health` includes a database check and proves DB reachability/migrations | `Program.cs` registers `AddHealthChecks()` but no actual health probes; the route writer only serializes the report | Corrected the local interview-prep, improvement-plan, and DoD wording; SPEC-005 is being updated to document the current HTTP-only health result | Avoid claiming a readiness guarantee that the code does not implement |
| First generator attempt passed PowerShell's automatic `$input` variable as the file path and `npx` output was treated as an error | The CLI received an empty input; this was a shell invocation issue, not package incompatibility | Re-ran with a named `$openapiFile` variable and `npm exec --package=... -- openapi-typescript ...`; generation then succeeded deterministically | Validate the tool with the actual input and a correctly formed command |

## Edge cases considered

- The OpenAPI document is served only in Development; Production was not enabled
  for this inspection.
- Health HTTP success after a healthy startup is not a database readiness probe;
  no health-check behavior was added.
- Generated declarations are compile-time types only; no runtime response
  validation is implied.
- The current OpenAPI document is incomplete, so the temp generated output is
  evidence for generator compatibility only and is not suitable to commit or
  adopt before the metadata tests/fixes in SPEC-005.

## Auth and validation decisions

- Owner decision remains: generated declarations will be versioned and regenerated
  locally; `apiRequest`/`fetch` stays unchanged; no CI/CD.
- The candidate generator is `openapi-typescript` 7.13.0; no package was added to
  the repository during this smoke.
- Database health probing is explicitly outside Wave 2 and would require a
  separate behavior decision/spec.

## Files affected

- `docs/improvement-plan.md`
- `docs/interview-prep.md`
- `docs/02-definition-of-done.md`
- `docs/architecture-adr-plan.md`
- `docs/ai/029-openapi-baseline-and-generator-smoke.md`
- Temp evidence: `C:\Users\User\AppData\Local\Temp\opencode\openapi-wave2\openapi.json`
  and `openapi.d.ts` (not in the repository).

## Final decision

**Investigation complete; SPEC-005 remains draft.** Wave 2 must test/fix the
documented contract mismatches before consuming generated types. Health endpoint
behavior remains unchanged and no DB readiness guarantee is claimed.
