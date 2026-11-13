# Code Review: PDF requirement audit — IT CrudWorks (whole deliverable)

- **Reviewer:** reviewer agent (PDF-audit pass, separate from `docs/reviews/final-review.md`)
- **Date:** 2026-11-12
- **Scope:** the whole deliverable against the exercise PDF (`C:\Users\User\AppData\Local\Temp\opencode\pdfextract\exercise.txt`, 94 lines), `docs/00-brief.md`, the three specs, `project/README.md`, `docs/04-requirement-traceability.md`, `project/src/**`, `project/tests/**`, `project/frontend/src/**`, `docs/ai/**`, `docs/qa/**`.
- **Commands run:** **none.** This session has no shell permission (`Permission denied: shell`), so no `dotnet`/`npm` command was executed here. Every count below is either **Fact (read)** — statically counted from the source — or **Fact (recorded)** — quoted from a captured log in the repository. Nothing is inferred as passing.
- **Prior review:** `docs/reviews/final-review.md` (Approve with comments). This audit **does not repeat its closed findings**; it looks at PDF coverage, the demo surface, and what changed *after* that review.

---

## Verdict

**Request changes** — 0 blockers, 4 majors, 8 minors, 6 nits.

The engineering core is genuinely strong and I found **no auth bypass, no IDOR, no secret, and no dependency-rule violation**. The majors are not behaviour bugs: they are (1) the release evidence no longer describes the tree that is being handed over, (2)–(3) two PDF criteria that are claimed as done but are not, and (4) a deliverable that would commit `bin/`, `obj/`, `node_modules/` and a live SQLite database the moment `git init` runs.

---

## Resolution (2026-11-13)

The orchestrator re-ran the full gate on the current tree and closed the findings
that were actionable in this change:

| Finding | Status | Evidence |
| --- | --- | --- |
| M1 — stale release evidence | **Closed** | Full gate on the current tree: 166 backend + 80 frontend + 37 E2E + 6 self-test = **289 checks, all green**. Counts reconciled in `README.md`, `project/README.md`, `docs/02`, `docs/04`, `docs/presentation.md`, `docs/qa/test-plan.md` and `docs/qa/clean-run-report.md` (wave 5). |
| M2 — left-aligned login / no visual polish | **Closed** | `AuthLayout` centers the auth composition; measured at 360/768/1280 with no horizontal overflow; task list/form polished; screenshots captured during the wave-5 run. `docs/ai/020`. |
| M3 — no representative generated code | **Closed** | `docs/ai/007` now contains the delivered `TaskService.CreateAsync` and `TasksController` samples; `docs/ai/020` contains the UI/seed samples. |
| M4 — no root `.gitignore` | **Closed** | `.gitignore` added at the repository root (bin/obj/node_modules/dist/db/env/editor artifacts). |
| m1 — seed contradicts the user story | **Closed** | Five story-shaped tasks, all with description + due date; the fake "CI pipeline" task is gone. `docs/ai/019`. |
| m2 — stale Tailwind/MSW/Playwright claims | **Closed** | `docs/03-stack-baseline.md` now describes plain CSS, mocked `fetch`, and the API-level E2E script. |
| m3 — README API example not runnable | **Closed** | Replaced with a PowerShell `Invoke-RestMethod` example. |
| m4 — preview cannot reach the API | **Closed** | `vite.config.ts` now proxies `/api` for `npm run preview` too. |
| m5 — DoD claimed components never read tokens | **Closed** | DoD §7 now says components obtain the token from the auth context, never from storage, which matches the code. |
| m6 — routing-level 404 returned text/plain | **Closed** | `Program.cs` writes RFC 7807 `application/problem+json` from `UseStatusCodePages`; covered by `ProblemDetails_ForAnUnknownRoute_Returns404AsProblemJson`. |
| m7 — presentation marked draft | **Closed** | Status is now "ready — not yet rehearsed", with the wave-5 evidence pointers. |
| m8 — story promised "see what is due" with no due signal | **Closed** | The list now formats due dates and marks overdue tasks; completed tasks are excluded from the overdue label. Story wording unchanged (still truthful). |
| n1 — unreferenced `public/icons.svg` | **Closed** | Deleted. |
| n2 — PowerShell-serialized `package.json` | **Closed** | Rewritten as normal 2-space JSON with `&&`. |
| n3 — Escape dismissed the dialog while busy | **Closed** | `ConfirmDialog` ignores Escape while `busy`; regression test added. |
| n4 — raw ISO due dates | **Closed** | `lib/dates.ts` formats them as `Nov 14, 2026`. |
| n5 — traceability pointed at wave-1 logs | **Closed** | R-05 and the E2E row point at the wave-5 evidence. |
| n6 — no paging documented | **Closed** | ADR-0004 §4 has a "Non-goals" note explaining the deliberate absence of paging. |

Still open and honestly recorded: no git history, no CI, no screen-reader/axe
audit, and the demo has not been rehearsed twice. These are process gaps, not
behaviour defects.

---

## Findings

### Major

| # | File:line | Finding | Why it matters | Fix |
| --- | --- | --- | --- | --- |
| M1 | `docs/qa/clean-run-report.md:84-100,126-129`; `docs/ai/018-frontend-final-fixes.md:82-84`; `docs/02-definition-of-done.md:15-17`; `docs/04-requirement-traceability.md:19,22,32,93,96`; `README.md:25,146`; `project/README.md:78-79`; `docs/presentation.md:94-96,127`; `docs/qa/test-plan.md:190,306,557` | **The release evidence no longer describes the tree being handed over.** Two independent proofs: (a) the frontend suite is **70** cases, not 69 — `018` records `Tests 70 passed (70)`, and a static count agrees (69 `it(...)`/`test(...)` declarations in 10 files, one of them `it.each` with 2 rows at `project/frontend/src/pages/LoginPage.test.tsx:60-63` → 70 executed cases), while every other release artifact still says 69 and `277` in the totals; (b) the wave-4 block pins "the tree" by `SHA-256 39e5cf83…` over 116 source files and states "Nothing under `project/frontend/src/**` changed between the two runs" — but `018` changed `RegisterPage.tsx`, `RegisterPage.test.tsx`, `package.json` and `package-lock.json` **after** that gate. | The DoD and the traceability matrix are the artifacts the panel reads to decide "what is proven". Today they assert a single-tree gate with one fingerprint and one count, and both are stale; `docs/04:93`/`test-plan.md:557` present them as the authoritative closure of QA-07. There is no full gate (build + 165 backend + frontend + live 37/37) on the current tree — only a frontend-only re-run. | Owner: orchestrator. Either re-run the full gate once on the current tree and record the new fingerprint + 70/278 counts, **or** re-word the evidence as "wave-4 gate on tree X, then a frontend-only change in `018` verified with lint/test/build" and update 69→70 / 277→278 in every artifact listed in this row. Do not leave the old fingerprint presented as covering the current tree. |
| M2 | `project/frontend/src/index.css:65-69,98-104,137-139`; `project/frontend/src/pages/LoginPage.tsx:74-75`; `project/frontend/src/pages/RegisterPage.tsx:101-102`; `project/frontend/src/pages/TaskFormPage.tsx:174` | The auth pages and the task form are pinned to the **left edge of a 64 rem page**: `.page` is `width:min(64rem,100% - 2rem); margin:0 auto`, while `.panel`/`.auth-panel` only cap `max-width` (40 rem / 26 rem) with no `margin-inline`. At 1280 px the login card is a 416 px box starting at x≈128, with ≈736 px of empty viewport to its right, and no vertical centering (`padding:2.5rem 0`). The task list, by contrast, fills the full 64 rem, so the contrast is visible on every route change. | This is the **first screen the panel sees** and the PDF scores "responsive and user-friendly" and "Presentation". It reads as an unstyled scaffold, which is exactly the impression a working CRUD app does not deserve. It is also the one screen that was never measured (`docs/ai/015:129-131` measures the **list** only, while `docs/04:21` and `docs/02:118` claim "the list **and** the form"). | One CSS change: give the panel a centering wrapper (`.auth-panel, .task-form-panel { margin-inline:auto }`) and centre the auth card vertically (`min-height` + `place-items:center` on a `.page-centered`). Add the demo credentials under the login heading (currently only in `project/README.md:7`), so the presenter does not recite them. Re-measure 360/768/1280 on `/login` and `/register`, not just the list. |
| M3 | `docs/ai/003-auth-slice.md:22-28`, `docs/ai/007-task-backend-slice.md:25-36`, `docs/04-requirement-traceability.md:27` | The PDF asks: "**Show the output code (or a representative sample of it)**". The AI log answers with prose: every entry has an "Output summary" and no code sample. Grepping `docs/ai/**` for fenced `csharp`/`diff` blocks returns exactly one C# block (`docs/ai/018:47`), and it is a *quoted API rule*, not model output. R-21 is marked "Done — output summaries and **representative diffs** recorded per slice", which is not true. | GenAI fluency is an explicit evaluation criterion and this is its one missing artifact. The saved delegation briefs (`003:145-232`, `004:230-318`) are genuinely good and would satisfy R-20 on their own — but the panel's next question ("show me what the model produced") has no answer in the repo. | Add to `007` (and one to `003`): the real generated file as accepted (e.g. `TaskValidation.cs`), plus one before/after diff for the two corrections already listed (`Enum.TryParse` → whitelist, and the `AsNoTracking` update bug in `docs/ai/011:31-38`). Correct R-21 to say what is actually there. |
| M4 | repository root (no `.gitignore`); `project/src/CrudWorks.Api/crudworks.db` (+ `-shm`, `-wal`); `project/src/**/bin|obj`, `project/frontend/node_modules`; `docs/02-definition-of-done.md:26` | The tree that will be handed over has **no `.gitignore`**, while it contains `bin/`, `obj/`, `node_modules/` and a **live SQLite database** in the API folder. The DoD's first open item is to `git init` and commit. | The moment that item is closed, the first commit ships build output, a dependency tree of hundreds of megabytes, and a runtime database — an explicit `AGENTS.md` §12 violation and the first thing a reviewer looks at in `git status`. If the folder is zipped instead, the panel still receives `node_modules` and a DB with a seeded hash. | Add `.gitignore` before any `git init`: `bin/`, `obj/`, `node_modules/`, `*.db`, `*.db-shm`, `*.db-wal`, `.env`, `TestResults/`, `.vs/`, `dist/`. Decide explicitly whether the runtime DB ships (recommended: no — migrations + seed recreate it, `project/README.md:53-60`). |

### Minor

| # | File:line | Finding | Why it matters | Fix |
| --- | --- | --- | --- | --- |
| m1 | `project/src/CrudWorks.Infrastructure/Persistence/Seed/DatabaseSeeder.cs:76-86` | The five demo tasks are meta-project tasks: "Write the BLA technical exercise proposal", "Set up the CI pipeline" (status **Done**), "Prepare the demo scenario", "Review the API contract", "Send the weekly status update". | The user story (`docs/user-story.md:10-21`) is about a busy professional's real work; the seed illustrates the **build process**, not the product. Worse, "Set up the CI pipeline — Done" is false: the repository has no CI (`docs/04:120` lists it as a gap), so the first thing a panel can check in the seed is a task that lies. 3 of 5 also have no due date, which hides the one field the story cares about ("see what is due"). | Reseed with story-shaped tasks (e.g. "Send the Q4 invoice — InProgress — due +2d", "Book the dentist — Todo — due +1d", "Finish the onboarding doc — Done") and drop the CI one. Requires a clean-database reset; `node docs/qa/tools/api-ctl.js reset` already does it. |
| m2 | `docs/03-stack-baseline.md:50,52,72` | The stack baseline still says **Tailwind CSS 4**, **MSW**, and **Playwright (optional)**. None shipped: ADR-0005:59 says "No Tailwind, no component library"; `project/frontend/package.json` has no `msw` and no Playwright; tests mock `fetch` at the boundary, which the same document says correctly at `:71`. | The document contradicts itself two lines apart, and a panel reading the stack baseline will ask why three declared tools are absent. | Mark the rows "rejected/superseded — see ADR-0005" or delete them. |
| m3 | `project/README.md:133-137` | The only API example is a `powershell` block that cannot run in PowerShell: `^` is cmd.exe line continuation, `curl` is an alias for `Invoke-WebRequest`, and `-d '{\"email\":…}'` is cmd quoting. | The README is the setup artifact for R-18, and this is what a panelist copies to prove the API works. | Use `Invoke-RestMethod -Method Post -Uri … -ContentType 'application/json' -Body '{"email":"demo@crudworks.local","password":"Demo123!"}'`, or drop the fence language to `cmd`. |
| m4 | `project/frontend/vite.config.ts:6-14`; `project/src/CrudWorks.Api/appsettings.json:12-14`; `project/src/CrudWorks.Api/Program.cs:71-79` | `npm run preview` cannot reach the API: only `server.proxy` is configured (preview serves on 4173 with no proxy), and CORS allows exactly `http://localhost:5173`. There is also no `.env.example` for `VITE_API_BASE_URL` (`project/frontend/src/lib/api.ts:1`), although DoD §7 claims the base URL "comes from an environment variable". | A panelist who runs `npm run build && npm run preview` — the obvious thing after seeing `npm run build` — gets a login screen that reports "Could not reach the server", and the CORS block prevents the obvious workaround. | Either add `preview: { port: 5173 }` (same origin allow-list) or document "dev server only" in `project/README.md` and add `.env.example` with `VITE_API_BASE_URL=http://localhost:5080`. |
| m5 | `project/frontend/src/pages/TaskListPage.tsx:21`; `project/frontend/src/pages/TaskFormPage.tsx:57`; `project/frontend/src/lib/tasks.ts:52-66`; vs `docs/02-definition-of-done.md:121` and `docs/reviews/final-review.md:200-203` | The DoD ticks "Auth state is centralized; **components do not read tokens ad hoc**", and the prior review asserted it. In fact both task pages read `session?.accessToken ?? ''` and thread the token through every call (`listTasks(token)`, `createTask(token, …)`). | The claim is what the panel checks against the code; the code shows the token being passed by hand at four call sites. It is not a security bug (the token never leaves the auth module's storage), but it is a false tick in the DoD. | Either move the token inside `lib/tasks.ts` (it already imports `readSession`, so the parameter is redundant) and delete the two reads, or change DoD §7 to "components obtain the token from the auth module, never from storage". |
| m6 | `project/src/CrudWorks.Api/Program.cs:87` vs `docs/adr/0004-api-contract.md:38-40` | ADR-0004 says "**All** errors use RFC 7807 `application/problem+json`". With `UseStatusCodePages()` and no handler, a routing-level 404 (unknown URL) and a 405 return the default `text/plain` "Status Code: 404; Not Found" body. | The contract sentence is absolute; the first thing a curious panelist does is `GET /api/nope` and look at the content type. | Either drop `UseStatusCodePages()` or give it a writer that emits `ApiProblemDetails` like the exception handler does. |
| m7 | `docs/presentation.md:4` vs `docs/04-requirement-traceability.md:29`; `docs/02-definition-of-done.md:27` | The presentation file's own status is "**draft**" and says its evidence comes from "the measured wave-1 clean run", while R-23 is marked "Done — outline, demo script and Q&A sheet" and the rehearsal is still an open DoD item. It also still carries the 69/277 figures (`:94-96,127`). | The presenter opens this file live; "draft, wave-1" next to "Done" invites the question of which artifacts are current. | Set the status to "ready — not yet rehearsed" and refresh the counts with M1. |
| m8 | `docs/user-story.md:18-21`; `project/frontend/src/pages/TaskListPage.tsx:95-131`; `specs/002-task-crud/spec.md:66` | The story's value sentence is "so that I can **see what is due** and track progress", but the list is ordered by `createdAtUtc` (spec-pinned) and the only due-date signal is a raw `Due 2026-11-14` line with no overdue emphasis or sorting. | The panel reads the story first and then asks it to be demonstrated. The app cannot show "what is due" better than by eye. | Cheapest honest fix before Friday: align the story wording with what ships ("newest first, with the due date visible") in the presentation. If there is time, add an overdue marker client-side, which is a spec change (`specs/003`) and must be recorded in the change log first. |

### Nit

| # | File:line | Finding | Why it matters | Fix |
| --- | --- | --- | --- | --- |
| n1 | `project/frontend/public/icons.svg` | Unreferenced asset (no match for `icons.svg` anywhere in `src/`, `index.html` or CSS), but Vite copies `public/` into `dist/`. | DoD §7 ticks "no dead code"; the prior review checked unused dependencies but not unused assets. | Delete it or wire it into `StatusBadge`. |
| n2 | `project/frontend/package.json:8` | The file is PowerShell-serialized: aligned keys, and the build script reads `"tsc -b \u0026\u0026 vite build"`. It is valid JSON and npm sees `&&`, but a reviewer opening `package.json` sees an escaped ampersand and inconsistent 4-space indentation. | First file a frontend reviewer opens; looks corrupted. | Re-format with `npm pkg fix` or a normal 2-space rewrite (no behaviour change). |
| n3 | `project/frontend/src/components/ConfirmDialog.tsx:47-51,84-92` | Escape calls `onCancel` even while `busy` (the two buttons are disabled at `:100,:108`, so the keyboard can dismiss a delete that is in flight), and the backdrop is not `inert`/scroll-locked, so a mouse click can move focus behind the modal. | Small a11y inconsistency in the one dialog the panel will exercise with the keyboard. | Guard Escape with `if (!busy)`, and add `overflow:hidden` on body or `inert` on the page while open. |
| n4 | `project/frontend/src/pages/TaskListPage.tsx:107` | Due date renders as the raw ISO string (`Due 2026-11-14`). | "User-friendly" is a PDF criterion; ISO dates are a developer format. | `toLocaleDateString` with a fixed `en-GB`/`en-US` locale so tests stay deterministic. |
| n5 | `docs/04-requirement-traceability.md:11,34` | R-05/R-28 cite `docs/qa/logs/e2e-run.log` (wave 1) while `docs/qa/logs/e2e-run-wave4-run1.log` is the current evidence. | Minor, but the matrix should point at the same gate the DoD calls authoritative. | Repoint to the wave-4 log. |
| n6 | `project/src/CrudWorks.Infrastructure/Persistence/TaskRepository.cs:9-17` | `ListByOwnerAsync` has no paging or cap; the endpoint returns every task for the user. | Unbounded response is a standard panel probe. There is a good answer ("demo scale, no pagination in scope, ADR non-goal") — but it must be in the ADR, not improvised. | One sentence in ADR-0004 §"Non-goals" plus a one-line Q&A answer. |

---

## Requirement coverage (PDF → evidence)

| PDF requirement | Verdict | Evidence |
| --- | --- | --- |
| Database + a second store for users; PK + ≥2 fields | **Met** | `project/src/CrudWorks.Infrastructure/Persistence/Migrations/2026111…_InitialCreate.cs`, `…_AddTasks.cs`; `TaskItem.cs:40-55`; tests pin FK/index (`docs/04:13`) |
| CRUD API with correct verbs/params/returns | **Met** | `TasksController.cs:14-58`; 200/201/204/400/401/404; `TasksApiTests.cs` (18 facts + a 5-route 401 theory = 23 cases) |
| Second API: create user, login, authorized + non-authorized endpoints | **Met (single host, separate route group)** | `AuthController.cs:13-48`; `/api/health` anonymous. See Open question 1 |
| Data access layer | **Met** | `ITaskRepository`/`IUserRepository` in Application, EF impls in Infrastructure; integration-tested against real SQLite |
| Independent business layer | **Met** | `TaskService.cs`, `AuthService.cs`, `TaskValidation.cs` — no EF/HTTP types; project references confirm |
| Unit tests for data, business, API | **Met** | 166 backend cases (37/57/25/47) recorded in wave 5 (`docs/qa/clean-run-report.md`); IDOR read/update/delete pinned at `TasksApiTests.cs:272-328` |
| React frontend, CRUD, structured state | **Met** | `AppRoutes.tsx`, `pages/`, `lib/tasks.ts`, TanStack Query; 70 frontend cases (see M1) |
| Responsive and user-friendly | **Partly** | No horizontal overflow measured **on the list only** (`docs/ai/015:129-131`); the auth/form screens are left-pinned (M2) |
| README with setup and docs | **Met** — in `project/README.md` (prereqs, run, reset, tests, demo credentials, API summary); the root `README.md:1-5,148` is a process playbook that only points at it | `project/README.md:22-51,62-73,111-137` |
| Seeded data/credentials | **Met** | `DatabaseSeeder.cs:10,14,76-86`; `project/README.md:7`; seeding is Development-only, documented `README.md:36-39` |
| GenAI prompt / output / validation / corrections | **Prompt: strong. Output: missing** | Briefs verbatim `docs/ai/003:145-232`, `004:230-318`; corrections tables throughout — but no output code sample (M3) |
| Informal user story | **Met** | `docs/user-story.md`; wording/UI drift in m8 |
| "ASP.NET MVC" mentioned in the PDF | **Interpretation** | Delivered as Web API + React SPA; `docs/00-brief.md:160` records the reading. See Open question 2 |

---

## Checks

**Architecture — pass.** `Domain` has no references; `Application` → `Domain`; `Infrastructure` → `Application`; `Api` is the composition root (`project/src/*/*.csproj`). Controllers are thin (`TasksController.cs:17-58` is bind/delegate/map only). No MediatR/CQRS/AutoMapper/generic repository. Manual mapping lives in `TaskMapping.cs`/`AuthMapping.cs`.

**Security — pass, no findings.** Every task route is `[Authorize]` at the controller (`TasksController.cs:10`); anonymous is limited to register/login (`AuthController.cs:14,27`) and health (`Program.cs:103`). Ownership is `TaskRepository.FindAsync(id, userId)` (`TaskRepository.cs:22-24`) with the caller from `sub` (`HttpCurrentUser.cs:13`), never from the body (`CreateTaskRequest.cs` has no `UserId`; pinned by `TasksApiTests.cs:174`). Foreign id → 404, never 403. PBKDF2 via `PasswordHasher<User>`; identical 401 for unknown email/wrong password (`AuthService.cs:49-62`). No key in source; Development ephemeral key, non-Development fails fast (`JwtSigningKey.cs:16-31`). CORS is a fixed origin, no credentials (`Program.cs:71-79`). 500s return a generic title (`ProblemDetailsExceptionHandler.cs:41-49`). **Note:** the live `crudworks.db` in the tree (see M4) contains only the seeded demo user's PBKDF2 hash, not a plaintext secret.

**Tests — pass on quality.** Tests assert behaviour and status codes, not implementation; 401/400/404/409 paths exist per endpoint; IDOR is covered for read, update and delete; SQLite files are per-factory and deleted on dispose (`CrudWorksApiFactory.cs:19-47`); no skipped tests (grep for `Skip=` returns nothing). The one caveat a panel will press: `docs/ai/013:198` states the 12 already-implemented lines had **no genuine RED** (a temporary mutation was used), while R-03 is marked "Done". The doc is honest; the checkbox is generous.

**Frontend — mostly pass.** Strict TypeScript, no `any`, no `console.*`; all four list states exist; labelled inputs, `role="alert"`, focus management, keyboard tests; 401 clears the session once with no retry loop (`App.tsx:13-14`, `tasks.ts:44-50`). Two gaps: the layout/centering defect (M2) and the token threading (m5).

**Docs / GenAI honesty — mixed.** The corrections narrative is the best part of the deliverable: specific, falsifiable, and it records a real bug found by the review follow-up (`docs/ai/011:29-38`, a silent no-op PUT). Against that, M1/M3/m2 show the evidence set drifting out of sync with the tree — which is precisely the failure mode this workspace's `AGENTS.md` §7 exists to prevent.

---

## What I could not verify

- **No command was run** (shell denied). `dotnet test`, `npm run test`, `npm run lint` and `npm run build` were not executed in this session. Counts and results above are quoted from `docs/qa/clean-run-report.md`, `docs/qa/coverage-summary.md`, `docs/ai/015`, `016`, `018`.
- **No browser.** The M2 layout finding is derived from the CSS cascade, not from a screenshot: `.page` 64 rem, `.auth-panel` 26 rem, no `margin-inline`. I could not start the dev server to capture it.
- **The 116-file SHA-256** could not be recomputed here. The second half of M1 rests on the recorded wave-4 output (69 tests) versus `018`'s recorded output (70 tests) plus `018`'s list of changed files — that is already sufficient to prove the tree changed after the fingerprint was taken.
- I did not re-read all 19 AI entries; the M3 claim is based on a grep of every fenced code block in `docs/ai/**`.

**Open questions for the orchestrator (not findings):**
1. The PDF says "a **second API**". The solution ships one host with an `/api/auth` route group. Are you ready to answer "that is the second API surface, one deployable, documented in ADR-0004"? If not, add one sentence to `docs/presentation.md` §3.
2. The PDF names "ASP.NET MVC, Web API". You ship Web API + React SPA; `docs/00-brief.md:160` already records the reading. Say it out loud in the first minute of the presentation.

---

## Smallest set of fixes before the demo (in order, ~90 minutes)

1. **M2** — centre the auth/form panels and put the demo credentials on the login screen. One CSS block + one JSX block. Buys the most visual credibility per minute.
2. **M1** — 69→70 and 277→278 in every artifact listed under M1; re-word the wave-4 fingerprint paragraph as "gate on tree X, then a frontend-only change verified in `018`" (or re-run the gate once, which also closes it properly).
3. **M4** — add `.gitignore` (`bin/`, `obj/`, `node_modules/`, `*.db*`, `.env`, `.vs/`, `TestResults/`) **before** anything else touches `git init`; decide whether the runtime DB ships.
4. **M3** — paste one real generated file + two before/after diffs into `docs/ai/007`; correct R-21.
5. **m1** — reseed with story-shaped tasks and remove the false "CI pipeline — Done".
6. **m3** — fix the README API example so it actually runs in PowerShell.

Everything else (m2, m4–m8, n1–n6) can be accepted with a recorded risk if the deadline bites; none of it changes behaviour.

---

## Summary

- **Good:** the dependency rule holds and is visible; ownership/IDOR is enforced in the Application layer and tested for read, update and delete; secrets, hashing, CORS and error leakage are all clean; the test suite is behavioural and the correction log is unusually honest (it records a real bug it found).
- **Must change before the demo:** M1 (evidence no longer matches the tree), M2 (the first screen looks unfinished), M3 (the PDF's "show the output code" is missing), M4 (no `.gitignore` with build output and a DB in the tree).
- **Can wait:** the minors and nits, which are documentation drift, seed flavour, and polish.
- **Do not** present the current 69/277/one-tree claims as measured facts until M1 is resolved; that is the single fastest way to lose credibility in a code review.
