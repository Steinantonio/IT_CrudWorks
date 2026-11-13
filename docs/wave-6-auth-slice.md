# Wave Summary — 6: Authentication slice

- **Date closed:** 2026-11-12
- **Agents:** backend-implementer, frontend-implementer, reviewer, architect, orchestrator
- **Scope:** SPEC-001 (all tasks) + SPEC-003 tasks T-003-01…T-003-06

## Delivered

- Backend auth: `User`, `AuthService`, validation with field errors, EF
  persistence + `InitialCreate` migration, `PasswordHasher<User>` adapter, JWT
  service, seed demo user, thin `AuthController`, ProblemDetails mapping,
  constant-time login verification, `ILoginAudit` domain-only logging.
- Frontend auth: session module (only owner of `sessionStorage`), `AuthProvider`,
  `RequireAuth` with return path, login/register pages with validation, 401/409
  handling, layout with logout, route extraction.
- Independent review with severity-ordered findings and adjudications.
- `project/README.md` (drafted; task section marked as in-progress contract).

## Verified (real output)

| Check | Result |
| --- | --- |
| `dotnet build CrudWorks.slnx -warnaserror` | 0 warnings, 0 errors |
| `dotnet test CrudWorks.slnx` | 58 passed, 0 failed (15/19/13/11) |
| `npm run lint` / `npm run test` / `npm run build` | clean / 23 passed / 177 modules |
| Live API smoke | register 201 + `Location: /api/auth/me`; duplicate 409; invalid 400; login 200; unknown/wrong 401 identical; `/me` 200/401 |
| Live UI smoke | demo login, reload, logout, duplicate 409, fresh register, wrong password |
| Review | 0 blockers; the 1 major (timing side-channel) fixed; minors fixed or recorded |

## Decisions

- Constant-time login: `IPasswordHasher.Verify(password, string? hash)`; null
  hash verifies against a precomputed dummy hash (ADR-0002 amendment).
- Failed-login audit: `ILoginAudit` port in Application, `ILogger` implementation
  in Api; logs the email domain only.
- `PasswordHasherAdapter` `null!` user argument accepted as a recorded risk.
- Register stores no token; the session is created only by login.
- `state.from` accepted only for internal paths; rejection now tested.

## Risks / open items

- T-003-11 (global protected 401) is being wired in Wave 7 with the task UI.
- Real keyboard traversal pass is still pending (T-003-12).
- No git repository; commits require explicit owner approval.

## Next

Wave 7: SPEC-002 backend (task CRUD, ownership, seed tasks, three IDOR tests) and
SPEC-003 task UI (list states, form, delete confirmation, global 401), then QA
clean-run and the final documentation pass.
