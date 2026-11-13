# AI Evidence — 006: Recording the Review Outcomes for the Auth Slice

- **Date:** 2026-11-12
- **Agent:** architect
- **Task:** Record the review outcomes for SPEC-001 (backend auth slice) in the artifacts I own, after the reviewer's report and the orchestrator's adjudication.
- **Tool/model:** OpenCode subagent (GLM-5.3-Flash Free)

## Prompt summary

The orchestrator instructed me to read `docs/reviews/review-auth-slice.md` and
record the decisions: FIX the login timing side-channel and the failed-login
logging (SPEC-001 §8); ACCEPT the `PasswordHasherAdapter` `null!` user argument as
a recorded risk. I was restricted to `docs/adr/**`, `docs/ai/**`, and the
`specs/**/spec.md` + `plan.md` change logs — no code, tests, tasks statuses, or
frontend.

## What I read

`docs/reviews/review-auth-slice.md` (findings table, security checks, verdict),
`docs/adr/0002-auth-strategy.md`, `specs/001-auth/spec.md` (§8 rules, §11 change
log), `docs/ai/005-review-auth-slice.md` (adjudication recommendations), and the
`null!` correction already recorded in `docs/ai/003` (corrections table).

## What I changed

- `docs/adr/0002-auth-strategy.md` — added a "Review amendments (2026-11-12)"
  section documenting the constant-time login verification (single
  `IPasswordHasher.Verify` call on both branches; `null` hash means the precomputed
  dummy hash), the `ILoginAudit` port with domain-only failed-login logging, and
  the accepted `null!` risk. Status was already `accepted`; unchanged.
- `specs/001-auth/spec.md` — one change-log row dated 2026-11-12. Acceptance
  criteria were not rewritten; they already match the implementation.
- This entry.

## Why the two fixes were chosen over recording risks

The reviewer graded the timing side-channel **Major**: an unknown email returned
immediately while a known email ran PBKDF2 (`AuthService.cs:46-52`), so response
*time* distinguished "email exists" from "email unknown" even though the body was
identical — SPEC-001 §8 requires "no user enumeration", and account enumeration is
what the panel probes on an auth exercise (`review-auth-slice.md`, findings row 1).
The §8 logging rule had no implementation and no task, leaving an approved spec
rule and the code in disagreement (findings row 2). Both fixes were small
(roughly a 5-line adapter change plus one port and one logger call) relative to
the risk of the panel observing a live enumeration channel or a dead spec row.
The `null!` finding, by contrast, is safe with the shipped default
`PasswordHasher<User>` (it never reads the user), was already documented in code
and in `docs/ai/003`, and its cheapest honest resolution was recording it in
ADR-0002 — exactly what was done.

## Validation

Documentation-only change: no build or test run was needed and none was run. The
edits cite the review's `file:line` evidence; no claims about code or commands
were newly made.
