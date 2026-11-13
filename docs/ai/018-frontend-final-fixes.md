# AI Evidence — 018: Frontend final fixes (coverage dependency and password rule)

- **Date:** 2026-11-12
- **Agent:** frontend implementer (subagent session, ran out of usage budget before
  completing); the two changes below were applied by the orchestrator from the
  subagent's written report and verified with the real commands.
- **Task:** Close the two findings the final review left against the frontend:
  an unused `@vitest/coverage-v8` dependency, and a client-side password rule
  that was stricter than the API's.
- **Tool/model:** OpenCode session (MiniMax M3.1 Flash Free).

## Prompt summary

The final review (`docs/reviews/final-review.md`) left two findings against the
frontend, both Minor/Nit level and both owned by the frontend implementer:

1. **Minor — `project/frontend/package.json:31` lists `@vitest/coverage-v8` in
   `devDependencies`, but there is no `coverage` script, no vitest coverage
   configuration and no coverage run anywhere in the repository.** `AGENTS.md` §4
   forbids unused dependencies and `docs/03-stack-baseline.md` §2 forbids adding
   them. The DoD coverage floor (`docs/02-definition-of-done.md` §6) is measured
   on the **backend** with coverlet; there is no frontend coverage floor and no
   committed frontend coverage report.

2. **Nit — `project/frontend/src/pages/RegisterPage.tsx:28-29`.** The client
   required `/[A-Za-z]/` and `/[0-9]/`; the API required `password.Any(char.IsLetter)`
   and `password.Any(char.IsDigit)`. `char.IsLetter` is true for Unicode letters
   (`é`, `ã`, `Ж`) while the regex is not, so a password with a non-ASCII letter
   and a digit passed the API and was rejected by the client.

## Disposition and changes

### 1. Unused dependency — Option A (remove)

The disposition is to **remove** `@vitest/coverage-v8` rather than wire it up.
The DoD coverage floor is a backend floor measured with coverlet; there is no
frontend coverage floor, and no frontend coverage report is committed. Wiring it
up would have created a new artifact and a new claim to defend for no
requirement. `npm install` removed 11 packages; `npm ls` reports no missing
dependency.

### 2. Password rule — change the client to mirror the API

The API rule (read from `project/src/CrudWorks.Application/Auth/AuthValidation.cs`,
lines 76-80) is:

```csharp
private static bool IsStrongPassword(string? password) =>
    password is not null
    && password.Length is >= PasswordMinLength and <= PasswordMaxLength
    && password.Any(char.IsLetter)
    && password.Any(char.IsDigit);
```

with `PasswordMinLength = 8` and `PasswordMaxLength = 128`. `char.IsLetter` is
true for the Unicode categories `Lu`, `Ll`, `Lt`, `Lm`, `Lo`, and `char.IsDigit`
is true for category `Nd`. The client-side equivalent is therefore
`/\p{L}/u` and `/\p{Nd}/u`, not `/[A-Za-z]/` and `/[0-9]/`.

The client was changed to mirror the API exactly. The server was not touched —
narrowing the server's auth rule to ASCII would have been a product-behaviour
change to the auth contract, which is the architect's call, not the frontend's.

### 3. Test pinning the new behaviour

A test was added to `RegisterPage.test.tsx`:

> `it('accepts a password that contains a non-ASCII letter and a digit (mirrors the API rule)')`

It types `senhaé12` (8 characters, one non-ASCII letter, two digits) and asserts
that no field error is shown and the submit reaches the API. The test would have
failed against the old `/[A-Za-z]/` regex, because `é` is not in that range.

## Validation

```text
=== npm run lint ===
> oxlint
LINT_EXIT=0

=== npm run test ===
 Test Files  10 passed (10)
      Tests  70 passed (70)
   Duration  6.97s (tests 42%, environment 40%, setup 9%, import 8%, transform 1%, worker 1%)

=== npm run build ===
✓ 183 modules transformed.
✓ built in 183ms
BUILD_EXIT=0

=== backend (unchanged) ===
dotnet test CrudWorks.slnx
Aprovado!  – Com falha: 0, Aprovado: 37, Ignorado: 0, Total: 37 - CrudWorks.Domain.Tests.dll
Aprovado!  – Com falha: 0, Aprovado: 57, Ignorado: 0, Total: 57 - CrudWorks.Application.Tests.dll
Aprovado!  – Com falha: 0, Aprovado: 25, Ignorado: 0, Total: 25 - CrudWorks.Infrastructure.Tests.dll
Aprovado!  – Com falha: 0, Aprovado: 46, Ignorado: 0, Total: 46 - CrudWorks.Api.Tests.dll
```

## Corrections

| What the AI asserted | What was wrong | What changed | Why |
| --- | --- | --- | --- |
| The client's password rule was ASCII-only while the API's was Unicode-aware | Two different rules for the same requirement | The client now mirrors the API exactly, with a test pinning the non-ASCII case | The client must not reject what the API would accept, and both must state one rule |
| `@vitest/coverage-v8` was a dependency with nothing to show for it | No script, no configuration, no run | Removed, and the gate re-verified afterwards | An unused dependency is dead weight the DoD forbids |

## Files affected

- `project/frontend/package.json` — `@vitest/coverage-v8` removed.
- `project/frontend/package-lock.json` — updated by `npm install`.
- `project/frontend/src/pages/RegisterPage.tsx` — the password rule now mirrors the API.
- `project/frontend/src/pages/RegisterPage.test.tsx` — a test pinning the non-ASCII case.

No backend file was touched. No dependency was added. No commit was made.
