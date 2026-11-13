# Code Review: <Scope>

## Verdict

<Approve / Approve with comments / Request changes / Block>

## Findings

| Severity | File:line | Finding | Why it matters | Suggested fix |
| --- | --- | --- | --- | --- |
| Blocker | `<path:line>` | <finding> | <impact> | <fix> |
| Major | `<path:line>` | <finding> | <impact> | <fix> |
| Minor | `<path:line>` | <finding> | <impact> | <fix> |
| Nit | `<path:line>` | <finding> | <impact> | <fix> |

## Architecture checks

- [ ] Dependency rule is respected.
- [ ] Business logic is not in controllers or `DbContext`.
- [ ] No EF Core / ASP.NET Core types in `Domain` or `Application`.
- [ ] Repository interfaces are in `Application`; implementations are in `Infrastructure`.
- [ ] The composition root is in `Api`.

## Security checks

- [ ] Protected endpoints require authentication.
- [ ] Ownership checks prevent IDOR.
- [ ] Passwords are hashed; no plaintext in logs or responses.
- [ ] Secrets are not in source.
- [ ] Input validation is present at the boundary.
- [ ] CORS is restricted.
- [ ] Error responses do not leak internals.

## Test checks

- [ ] Unit tests cover business rules.
- [ ] Integration tests cover persistence and API behavior.
- [ ] Edge cases and failure paths are covered.
- [ ] Tests assert behavior, not implementation.
- [ ] No test depends on shared mutable state.

## Frontend checks

- [ ] TypeScript is strict; no `any`.
- [ ] Loading, empty, error, and success states exist.
- [ ] Responsive at mobile, tablet, and desktop.
- [ ] Forms are accessible and keyboard-friendly.
- [ ] No console errors or warnings.

## Summary

- <what is good>
- <what must change before merge>
- <what can wait>
