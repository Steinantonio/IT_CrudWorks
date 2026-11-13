# Delegation Brief

> Use this template when the orchestrator assigns work to an agent. Keep it short; link to files instead of pasting them. No implementation is delegated without an approved spec.

## Objective

<One sentence. What must be true when this is done?>

## Spec and tasks

- Spec: `specs/NNN-<name>/spec.md` — status: <approved>
- Plan: `specs/NNN-<name>/plan.md`
- Tasks: `specs/NNN-<name>/tasks.md` — <T-NNN-01 … T-NNN-05>
- Acceptance criteria in scope: <AC-NNN-01, AC-NNN-02>

## Context to read

- `AGENTS.md`
- `specs/constitution.md`
- `docs/00-brief.md` — requirement IDs: <R-xx, R-yy>
- `docs/02-definition-of-done.md`
- `docs/04-requirement-traceability.md`
- <relevant ADR>
- <relevant code path>

## Constraints

- Architecture: <dependency rule / layer / contract>
- Security: <auth / ownership / secrets>
- Scope: do not touch <paths/features>.
- Tests: <what must be proven>.
- Language: English.

## Deliverable

- <artifact path or "in response">
- Update the task status in `specs/NNN-<name>/tasks.md`.
- <update traceability row(s)>

## Acceptance criteria

- [ ] <observable criterion from the spec>
- [ ] <test/command that proves it>
- [ ] <documentation/AI log updated>

## Format

Return `templates/handoff.md`. Include evidence, assumptions, open questions, and the AI evidence entry.

## Stop conditions

Stop and ask if:

- <ambiguity that changes the design>
- <missing decision or unapproved spec>
- <security or architecture violation>
