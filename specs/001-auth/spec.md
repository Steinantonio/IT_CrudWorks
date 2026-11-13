# SPEC-001 — Authentication and user identity

- **Status:** implemented
- **Owner:** architect
- **Approved by:** orchestrator
- **Requirements:** R-06, R-10
- **ADRs:** 0002, 0004
- **Last updated:** 2026-11-11

## 1. Problem

Tasks must belong to someone. Without registration and login, every task would be
public and ownership could not be enforced. The application needs a minimal,
explainable identity flow that a reviewer can exercise in under a minute.

## 2. Scope

**In scope**

- Register with email, display name, and password.
- Log in with email and password; receive a JWT access token.
- Read the current user via `GET /api/auth/me`.
- Unique email, normalized for case and surrounding whitespace.
- Password hashing with ASP.NET Core `PasswordHasher<User>`.

**Out of scope**

- Refresh tokens, logout on the server, token revocation.
- Password reset, email verification, MFA, social login.
- Roles, claims beyond the user id and email.
- User profile editing or deletion.

## 3. Actors

| Actor | Goal |
| --- | --- |
| Visitor | Create an account or obtain a token. |
| Authenticated user | Identify themselves to the API. |

## 4. User stories

| ID | Story |
| --- | --- |
| US-001-01 | As a visitor, I want to register with my email and a password, so that I can own tasks. |
| US-001-02 | As a registered user, I want to log in, so that I receive a token for protected calls. |
| US-001-03 | As an authenticated user, I want to read my profile, so that the client can show who I am. |

## 5. Acceptance criteria

### AC-001-01 — Registration succeeds

- **Given** no user exists with `new@example.com`
- **When** the visitor posts a valid email, display name, and password
- **Then** the API returns `201`, a `Location` header for `/api/auth/me`, and a
  user body with `id`, `email`, `displayName`, and `createdAtUtc`
- **And** the password hash is stored; the password never appears in the response.

### AC-001-02 — Duplicate email is rejected

- **Given** a user already exists with `demo@crudworks.local`
- **When** a visitor registers with `DEMO@crudworks.local` or with surrounding spaces
- **Then** the API returns `409` with a safe ProblemDetails message
- **And** no second user is created.

### AC-001-03 — Invalid registration is rejected

- **Given** an anonymous visitor
- **When** the email is malformed, the display name is shorter than 2 or longer
  than 100 characters, or the password is shorter than 8 characters or lacks a
  letter or a digit
- **Then** the API returns `400` with field-level `errors`
- **And** no user is created.

### AC-001-04 — Login returns a token

- **Given** a registered user
- **When** they post the correct email and password
- **Then** the API returns `200` with `accessToken`, `expiresAtUtc`, and a `user`
  summary
- **And** the token authenticates a subsequent protected request.

### AC-001-05 — Invalid credentials are indistinguishable

- **Given** either an unknown email or a wrong password
- **When** login is attempted
- **Then** the API returns `401` with the same generic message in both cases
- **And** the response contains no hint about which factor failed.

### AC-001-06 — Current user requires a token

- **Given** no token, an expired token, or a token with an invalid signature
- **When** `GET /api/auth/me` is called
- **Then** the API returns `401`
- **And** no user data is returned.

### AC-001-07 — Current user returns the caller

- **Given** a valid token for user A
- **When** `GET /api/auth/me` is called
- **Then** the API returns `200` with user A's summary
- **And** never another user's data.

## 6. Rules and validation

| Field | Rule | Violation |
| --- | --- | --- |
| `email` | Required, trimmed, lowercased, valid format, max 256, unique | `400`; duplicate → `409` |
| `displayName` | Required, trimmed, 2–100 characters | `400` |
| `password` | Required, 8–128 characters, at least one letter and one digit; never trimmed | `400` |
| `passwordHash` | Produced by `PasswordHasher<User>`; never returned or logged | — |

Normalization: email is trimmed and lowercased before validation and lookup.
Display name is trimmed. Password is used exactly as supplied.

## 7. API contract

Owned by `docs/adr/0004-api-contract.md`. Summary:

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | anonymous | `201` + user | `400`, `409` |
| POST | `/api/auth/login` | anonymous | `200` + token | `400`, `401` |
| GET | `/api/auth/me` | bearer | `200` + user | `401` |

JWT: HMAC-SHA256, issuer `CrudWorks`, audience `CrudWorks.Client`, 60-minute
expiry, `sub` = user id, `email` claim. The signing key comes from configuration
(`Jwt:SigningKey`). When it is absent in Development, the API generates an
ephemeral per-process key and logs a warning; tokens do not survive a restart.
Production fails fast without an explicit key. No signing key is committed.

## 8. Non-functional requirements

- **Security:** generic `401`; no password or hash in logs; no user enumeration.
- **Performance:** registration and login are single indexed lookups.
- **Observability:** failed logins log the email domain only, never the password.
- **Testability:** every criterion above has an API integration test.

## 9. Dependencies and assumptions

- `User` is the only aggregate in this spec.
- Email uniqueness is enforced by a unique index, not only by an application check.
- The client stores the token in `sessionStorage` (ADR-0002).

## 10. Open questions

None. Refresh tokens and revocation are deliberately out of scope and documented
as production hardening in ADR-0002.

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-11 | Initial spec | Required for ownership | Orchestrator |
| 2026-11-12 | Implemented §8 observability (domain-only failed-login log) and constant-time login verification per review-auth-slice; approved by orchestrator. | Review findings (`docs/reviews/review-auth-slice.md`) — Major timing side-channel and unimplemented §8 rule | Orchestrator |
| 2026-11-12 | Status moved from `approved` to `implemented` | Every acceptance criterion has automated coverage, the independent review has no open findings, and the clean-database run proves the live behaviour | Orchestrator |
