# AI Evidence Log

## Entry 028

- **Date:** 2026-11-18
- **Agent:** architect
- **Task:** Draft SPEC-005, PLAN-005, and TASKS-005 for Wave 2/A2; close the ADR-0007 generated-output decision; record the documentation work.
- **Tool/model:** OpenCode, GPT-6 Luna

## Prompt

Faithful summary: The project owner explicitly launched Wave 2/A2 and chose committed generated TypeScript types plus a documented local regeneration command. Read the operating contract, SDD templates, ADRs, current API/OpenAPI/frontend files, and Wave 1 evidence. Produce only the three draft SPEC-005 documentation artifacts, update ADR-0007 only to close the generated-output question and link the spec, and add this evidence entry. Do not implement code, add packages/generated files, run builds/tests, modify CI, or treat the draft as implementation approval.

## Output summary

- Drafted testable Given/When/Then criteria for OpenAPI route/schema/security/error completeness, deterministic committed output, frontend type consumption, and Production non-exposure.
- Ordered implementation as API assertions → minimal metadata → generator/reproducibility → frontend adoption → local verification/review.
- Recorded that the exact emitted component names and generator release/CLI must be verified before implementation.
- Closed only ADR-0007's Wave 2 committed-vs-local question and linked SPEC-005.

## Validation

- [ ] Build: Not run — documentation-only task.
- [ ] Unit tests: Not run — documentation-only task.
- [ ] Integration/API tests: Not run — documentation-only task.
- [ ] Manual behavior: Not exercised; inspected source and existing API test declarations only.
- [ ] Security review: Reviewed scope for bearer metadata and Development-only OpenAPI; no runtime security change made.
- [ ] Architecture review: No production project or dependency changes; no generated API client proposed.
- Documentation review: Read `AGENTS.md`, spec lifecycle/constitution/templates, ADR-0004/0005/0007, SPEC-004, and relevant API/frontend sources.
- Review correction: Documentation-only; no code changed, and no tests or builds were run.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Use exact OpenAPI schema/component names in frontend type references. | The runtime document was not inspected, and existing API coverage only asserts document availability. | Spec and plan require semantic assertions and defer component names until the approved implementation inspects the document. | Avoid inventing schema names or falsely claiming metadata is complete. |
| Treat the recommended generator command/version as already verified. | The package is not in the current frontend manifest and no package compatibility check was authorized or run. | Kept `openapi-typescript` as the proposed generator, marked version/CLI syntax as an assumption, and required verification before implementation. | The task forbids assumed package versions and package changes. |
| Treat generated declarations as runtime response validation. | Type declarations provide compile-time types, not runtime parsing. | Explicitly excluded runtime validation and retained the API as runtime authority. | Avoid a security/reliability overclaim. |
| Place SPEC-005 open questions as bullets in §9 without the template’s §10 section. | The required Open questions table (Question/Owner/Deadline/Resolution) was missing. | After the orchestrator identified the gap, added §10 with the required table, owners, deadlines, and resolutions; renumbered Change log to §11. | Restore template compliance while preserving SPEC-005’s draft status. |

## Edge cases considered

- Protected operation bearer requirements versus anonymous register, login, and health routes.
- `204` delete responses must have no response body.
- Error metadata must distinguish generic ProblemDetails from validation's optional `errors` map and include `traceId`.
- No successful OpenAPI document may be served in Production.
- A missing local Development document must make regeneration fail visibly.
- Health-check unhealthy response behavior is not specified by ADR-0004 and remains an open question; no behavior was invented.
- Local session/form models may remain only as distinct frontend state, not duplicate wire contracts.

## Auth and validation decisions

- No authentication or runtime validation behavior changes in this wave.
- OpenAPI should describe existing bearer and anonymous requirements; generated declarations are compile-time only.
- `apiRequest`/`fetch` remains the data-access boundary; no generated client is proposed.

## Files affected

- `specs/005-openapi-types/spec.md`
- `specs/005-openapi-types/plan.md`
- `specs/005-openapi-types/tasks.md`
- `docs/adr/0007-architecture-enhancements.md`
- `docs/ai/028-openapi-types-spec.md`

## Final decision

Partially accepted as a draft specification package. The owner-approved storage and regeneration decision is recorded; SPEC-005 remains `draft`, and no implementation is authorized until separate approval.
