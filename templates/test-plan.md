# Test Plan: <Scope>

## Objective

<What behavior must be proven, and for which requirement IDs?>

## Test pyramid

| Layer | What it proves | Tools | Location |
| --- | --- | --- | --- |
| Domain unit | Invariants and business rules | xUnit | `tests/Domain.Tests` |
| Application unit | Use cases, validators, authorization | xUnit + NSubstitute | `tests/Application.Tests` |
| Infrastructure integration | EF mappings, repositories, migrations | xUnit + SQLite | `tests/Infrastructure.Tests` |
| API integration | Status codes, auth, validation, ProblemDetails | WebApplicationFactory | `tests/Api.Tests` |
| Frontend component | Forms, list, auth guard, error states | Vitest + RTL + MSW | `frontend/src/**/*.test.tsx` |
| End-to-end | Critical user flow | Playwright (optional) | `e2e/` |

## Coverage targets

- Domain + Application: ≥ 80% line coverage.
- Every endpoint: at least one happy path and one failure path.
- Every critical UI flow: at least one test.
- Coverage is a floor, not a goal.

## Test matrix

| ID | Scenario | Type | Given / When / Then | Priority |
| --- | --- | --- | --- | --- |
| T-01 | <scenario> | Unit | <Given/When/Then> | High |
| T-02 | <scenario> | Integration | <Given/When/Then> | High |
| T-03 | <scenario> | API | <Given/When/Then> | High |

## Fixtures and seed data

- <fixture, factory, or seed>
- <how test data is isolated>

## Commands

```bash
dotnet test
npm run test
npm run test:e2e   # optional
```

## Environments

- Local: <database, containers, ports>
- CI: <runner, services, env vars>

## Risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| <risk> | <impact> | <mitigation> |

## Exit criteria

- [ ] All high-priority scenarios pass.
- [ ] No flaky test without an owner and a ticket.
- [ ] Coverage report is attached to the handoff.
