# AI Evidence Log

## Entry 009

- **Date:** 2026-11-12
- **Agent:** architect
- **Task:** SPEC-002 contract clarification — ADR-0004 `userId` handling
- **Tool/model:** OpenCode subagent (GLM-5.3-Flash Free)

## Prompt (summary)

The backend handoff (`docs/ai/007-task-backend-slice.md`) reported that ADR-0004 §5
said a client-supplied `userId` must be rejected with `400`
(`UserId is assigned by the server.`), but §1 said unknown fields are ignored by the
JSON binder. The implementation follows the ownership model: request DTOs
(`CreateTaskRequest`, `UpdateTaskRequest`) contain no `userId` property, so a
client-sent `userId` is silently ignored and the owner always comes from the `sub`
claim — the §5 row was unreachable as written. The orchestrator decided the
server-assigned owner is the security property and a `400` on an extra field is
noise; the ADR was to be updated to match the implementation.

## What changed

- `docs/adr/0004-api-contract.md` §1: "Unknown fields" row now explicitly includes a
  client-supplied `userId`; the owner is always derived from the token's `sub`
  claim, never from the body.
- §4 Tasks intro: replaced "must be absent / returns `400`" with "ignored (no
  request model binds it), never trusted"; added one line that status names are
  parsed case-sensitively (`"todo"` → `400`).
- §5 validation matrix: `userId` row behavior replaced with "Ignored (no request
  model binds it); the owner always comes from the `sub` claim, never from the
  body"; no error message returned. The ownership-invariant sentence is kept.
- New "Change log" section at the end of the ADR with a dated 2026-11-12 row and
  rationale.

## Why

Ownership is a server invariant: the owner comes from the token's `sub` claim.
Returning `400` on a client-sent `userId` would only confirm that the field is
read, contradicting the ownership model. The contract now documents the
implemented behavior instead of an unreachable rule.

## Validation

- [x] Diff checked against `docs/ai/007-task-backend-slice.md` (edge cases: "Unknown
  fields (a client-sent `userId`) are ignored by the JSON binder per ADR-0004 §1").
- [x] Docs-only change; no code, tests, or migrations touched.
- [x] `docs/ai/008-task-ui-slice.md` (frontend owner) untouched.
