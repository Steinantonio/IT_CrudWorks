# ADR-0002: Authentication and Authorization Strategy

- **Status:** accepted
- **Date:** 2026-11-11
- **Decision owner:** architect
- **Related requirements:** R-06, R-10, R-22, R-28

## Context

The exercise requires user creation, login, persisted user data, authorized and non-authorized endpoints, and a frontend that exercises the API. The panel will review security decisions. The deadline is Friday 2026-11-13, so the flow must be simple enough to implement, test, and explain.

The frontend is a React SPA on a different development origin (`http://localhost:5173`) from the API (`http://localhost:5000` or `https://localhost:5001`). The auth mechanism must work across that boundary without a week of CORS and CSRF debugging.

## Decision

Use a custom `User` table, ASP.NET Core's `PasswordHasher<User>` for password hashing, and JWT bearer access tokens for the API.

### User and password

- `User`: `Id` (Guid), `Email` (normalized, unique), `DisplayName`, `PasswordHash`, `CreatedAtUtc`.
- Password hashing: `Microsoft.AspNetCore.Identity.PasswordHasher<User>` (PBKDF2). Never roll a custom hasher.
- Password policy: minimum 8 characters, at least one letter and one digit. Maximum 128 characters to bound input.
- Email is trimmed and lowercased before storage and lookup; a unique index enforces it.
- Login failure returns `401` with the generic message `Invalid email or password`. Do not reveal whether the email exists.

### Token

- JWT bearer, HMAC SHA-256, 60-minute expiry for the demo.
- Claims: `sub` = user id, `email`, `name`, `jti`.
- Issuer, audience, lifetime, and signing key are validated.
- Signing key: at least 32 bytes. Production requires `Jwt__SigningKey` from the
  environment and fails fast if it is missing. Development generates an ephemeral
  per-process key when the setting is absent and logs a warning; tokens then do not
  survive a restart. No signing key is committed. Tests inject a fixed key.
- The API resolves the current user from `sub`; it never trusts a user id from the request body or query string.

### Authorization

- `[Authorize]` on all task endpoints.
- `[AllowAnonymous]` only on `POST /api/auth/register`, `POST /api/auth/login`, and `GET /api/health`.
- `GET /api/auth/me` requires authentication.
- Ownership is enforced in the Application layer: every task query is scoped by the current user id.
- A task that exists but belongs to another user returns `404`, not `403`, to avoid leaking existence.
- A missing or invalid token returns `401`.

### Frontend token storage

- Store the access token in `sessionStorage` for the demo.
- This is a deliberate trade-off: it is simple and survives a page refresh, but it is readable by JavaScript and therefore exposed to XSS.
- The production path is documented below; the demo does not pretend `sessionStorage` is best practice.

### CORS

- Allow only the configured frontend origin (`http://localhost:5173` in development).
- Allow credentials only if the final frontend uses cookies; with a bearer token, keep `AllowCredentials` off unless needed.
- No `AllowAnyOrigin`.

## Alternatives considered

| Option | Pros | Cons | Why not chosen |
| --- | --- | --- | --- |
| Full ASP.NET Core Identity + UI | Batteries included; mature | Heavy for a two-day exercise; UI and email flows are not in the brief | Too much surface for the timebox |
| Cookie auth + antiforgery | httpOnly cookie is safer against XSS; no token in JS | CORS credentials, CSRF token plumbing, SPA refresh complexity | More setup and more failure modes under deadline |
| JWT in `localStorage` | Simple; survives tab close | Longer exposure window than `sessionStorage` | `sessionStorage` is the smaller demo risk |
| Opaque session tokens in the database | Revocable; server-side control | Extra table, lookup, cleanup, and tests | Not needed for the demo; JWT is enough |

## Consequences

### Positive

- Register, login, and authorized CRUD are straightforward to implement and test.
- The token is stateless; no session table or sticky sessions.
- Ownership checks are explicit and testable.
- The security trade-offs are documented, which is what the panel is evaluating.

### Negative / trade-offs

- `sessionStorage` is vulnerable to XSS; the app must avoid injecting untrusted HTML.
- No refresh token, revocation, or logout-everywhere in the demo.
- A 60-minute token is a compromise between demo convenience and exposure.
- No email confirmation, password reset, account lockout, or rate limiting in the demo.

### Production path

- httpOnly, Secure, SameSite cookie for the access token, or a short-lived access token in memory plus a refresh token in an httpOnly cookie.
- Refresh-token rotation and revocation.
- CSRF protection for cookie-based flows.
- Rate limiting and account lockout on login.
- Email verification and password reset.
- Security headers and a Content Security Policy.

## Validation

- API tests:
  - register returns `201`; duplicate email returns `409`;
  - login returns `200` with a token; wrong password returns `401`;
  - no token on a task endpoint returns `401`;
  - a valid token returns `200`;
  - reading, updating, or deleting another user's task returns `404`;
  - validation errors return `400` with field details.
- Security checklist:
  - no password, hash, token, or authorization header in logs;
  - no secret in source;
  - CORS restricted;
  - `[Authorize]` present on every task endpoint.
- Reviewer checks the token storage trade-off is explained in the README/presentation.

## Review amendments (2026-11-12)

From `docs/reviews/review-auth-slice.md`; adjudicated by the orchestrator (FIX the
two findings below, ACCEPT the `null!` as a recorded risk).

- **Constant-time login verification (fixes the Major timing side-channel,
  `AuthService.cs:46-52`).** Unknown email and wrong password both run exactly one
  `IPasswordHasher.Verify(password, string? hash)` call. On the unknown-email path
  the adapter verifies against a precomputed dummy hash: a `null` hash argument
  means "verify against the dummy". Consequence: slightly more code in the adapter
  (`PasswordHasherAdapter`). Benefit: no timing-based account enumeration; AC-001-05
  now holds for response time, not only response body.
- **Failed-login audit logging (implements SPEC-001 §8 observability).** An
  `ILoginAudit` port in Application with `FailedLogin(string emailDomain)`,
  implemented in Api with `ILogger`. The log contains the email **domain only** —
  never the local part or the password — preserving the no-enumeration rule.
- **Accepted risk (unchanged):** `PasswordHasherAdapter` passes `null!` as the
  `TUser` argument because the default PBKDF2 implementation never reads the user.
  If the hasher is ever swapped for a user-aware implementation this must be
  revisited. Previously recorded in `docs/ai/003` (corrections table).

## Links

- `docs/00-brief.md` — R-06, R-10, R-22, R-28.
- `docs/03-stack-baseline.md` — auth and frontend stack.
- `docs/02-definition-of-done.md` — auth checklist.
