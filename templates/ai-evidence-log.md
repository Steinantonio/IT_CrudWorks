# AI Evidence Log

> One file per meaningful AI-assisted change, named `docs/ai/NNN-<topic>.md`.

## Entry <NNN>

- **Date:** <YYYY-MM-DD>
- **Agent:** <orchestrator / architect / backend / frontend / reviewer / QA>
- **Task:** <what was being produced>
- **Tool/model:** <tool and model, if known>

## Prompt

<Paste the prompt verbatim, or a faithful summary plus a link to the full text. Include the constraints that mattered: architecture, security, tests, language.>

## Output summary

<What the model produced. Include a representative snippet or diff when possible. Do not paste the entire output if it is long; link to it.>

## Validation

- [ ] Build: <command and result>
- [ ] Unit tests: <command and result>
- [ ] Integration/API tests: <command and result>
- [ ] Manual behavior: <what was exercised>
- [ ] Security review: <auth, ownership, secrets, validation>
- [ ] Architecture review: <dependency rule, boundaries>

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| <suggestion> | <problem> | <fix> | <reason> |

## Edge cases considered

- <edge case and how it is handled>
- <edge case and how it is handled>

## Auth and validation decisions

- <decision and rationale>

## Files affected

- `<path>`
- `<path>`

## Final decision

<Accepted / partially accepted / rejected, and the reason.>
