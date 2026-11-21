# CrudWorks.Architecture.Tests

Local, self-contained architecture policy tests for the four-project backend.

## Purpose

Make the dependency rule from ADR-0001 and `specs/constitution.md` Article 2
executable and reviewable without changing production behavior.

The existing `.csproj` project references remain the primary compile-time
enforcement. These tests add an explicit type/namespace policy layer:

- **Domain** has no dependency on another `CrudWorks.*` project and no
  dependency on EF Core, ASP.NET Core, HTTP, or database namespaces.
- **Application** may depend on **Domain** only and must not depend on
  **Infrastructure**, **Api**, or the forbidden framework namespaces.
- **Infrastructure** must not depend on **Api**.
- **Api** is the composition root and depends on **Application** and
  **Infrastructure**.

## Tooling

- `NetArchTest.Rules` 1.3.2 (smoke-tested by the orchestrator in a temporary
  .NET 10.0.404 project before this test project was created).
- xUnit, matching the existing test-project conventions.

## Running

```bash
cd project
dotnet test tests/CrudWorks.Architecture.Tests
```

No database or network service is required; the tests inspect assemblies and
namespaces only.

## Links

- `docs/adr/0001-clean-architecture.md` — four-project Clean Architecture decision.
- `docs/adr/0007-architecture-enhancements.md` — Wave 1 (architecture tests).
- `specs/constitution.md` Article 2 — the non-negotiable dependency direction.
- `specs/004-architecture-tests/spec.md` — acceptance criteria for this wave.
