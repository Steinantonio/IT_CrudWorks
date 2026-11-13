# Constitution — IT CrudWorks

This file holds the rules that no spec, plan, or task may violate. It is a
condensed, testable version of `AGENTS.md` for the specification layer.

**Status:** approved · **Owner:** orchestrator · **Last updated:** 2026-11-11

## Article 1 — Requirements before code

No implementation starts without an approved spec and an ordered task list.
When the brief and the code disagree, the brief wins until an ADR records a
deliberate deviation.

## Article 2 — One dependency direction

```
Domain ← Application ← Infrastructure
                 ↑
                Api (composition root)
```

- `Domain` references no other project and no framework.
- `Application` references `Domain` only.
- `Infrastructure` references `Application` (and transitively `Domain`).
- `Api` references `Application` and `Infrastructure`; controllers stay thin.

No EF Core, ASP.NET Core, HTTP, SQL, or provider types in `Domain` or
`Application`.

## Article 3 — Ownership is a security invariant

Every task belongs to exactly one user. A request may only read or mutate tasks
owned by the authenticated user. Another user's task returns `404`, never `403`,
so the API does not confirm that the resource exists.

## Article 4 — Tests are the executable spec

- Every acceptance criterion maps to at least one automated test or a named
  manual check.
- The test fails before the implementation and passes after it.
- Tests assert observable behavior, not private structure.
- No test depends on shared mutable state or on execution order.
- A skipped test is an accepted, recorded risk.

## Article 5 — Secure by default

- Passwords are hashed with a vetted implementation; plaintext is never stored
  or logged.
- Protected endpoints require a valid bearer token.
- Secrets come from environment variables or user-secrets, never from git.
- Input is validated at the boundary; error responses are safe ProblemDetails.
- CORS allows only the configured frontend origin.

## Article 6 — Honest evidence

- No claim without a command, a test name, a `file:line`, or captured output.
- Facts, assumptions, recommendations, and open questions are labeled.
- AI output is validated like any other contribution; wrong suggestions are
  recorded and corrected, not hidden.

## Article 7 — Small, reversible increments

- One logical change at a time; one owner per artifact.
- Prefer the simplest design that satisfies the spec.
- No MediatR, CQRS, AutoMapper, generic repository, or microservices.
- A timebox cut removes polish, never correctness or security.

## Article 8 — The demo is part of the product

A clean clone must run the app, apply migrations, seed demo data, and execute the
critical flow with the commands in the README. If the demo needs an undocumented
step, the product is not done.
