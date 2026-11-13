# AI Evidence Log

## Entry 020

- **Date:** 2026-11-13
- **Agent:** orchestrator + frontend implementer + backend implementer
- **Task:** UI polish (centered auth screens, demo access, task list/form), story-shaped demo seed, and the PDF re-audit findings.
- **Tool/model:** OpenCode subagents / DeepSeek V4.1 Flash

## Prompt

Two delegation briefs were sent after the independent PDF audit
(`docs/reviews/review-pdf-audit.md`):

1. **Frontend:** the login card was left-aligned in a 64 rem page (at 1280 px it
   started at x≈128 with ~736 px of empty space to its right). Build a shared
   `AuthLayout` with a centered composition, add a "Use demo account" action with
   the seeded credentials, polish the task list (summary counts, formatted due
   dates, status accents), polish the task form, and keep every existing label,
   id, `aria-*` hook, route, keyboard order, and test assertion. Run lint, test,
   and build.
2. **Backend:** the seed described the project itself, including "Set up the CI
   pipeline" marked Done although no CI exists. Replace the five tasks with
   realistic example stories a busy professional could own, each with a
   description and a due date; keep exactly five, keep the 2 Todo / 2 InProgress
   / 1 Done mix, keep idempotency, update the seed-pinning tests, and run the
   build and suite.

## Output summary

- `project/frontend/src/components/AuthLayout.tsx` is shared by `/login` and
  `/register`; it renders one `<main className="auth-page" tabIndex={-1}>`, a
  desktop intro column, and the form card. Below 860 px the intro is removed and
  the card is centered.
- `LoginPage` has a "Demo access" block with `demo@crudworks.local` / `Demo123!`
  and a **Use demo account** button wired through `setValue`.
- `TaskListPage` has a three-card summary, status-accent rows, and a formatted
  due date. A completed task is never labelled "Overdue".
- `TaskFormPage` has a "Back to tasks" link and a two-column status/due-date row
  on wide screens.
- `DatabaseSeeder` now seeds five story-shaped tasks (quarterly report, vendor
  contract, dentist, passport, tax documents), all with a description and a due
  date.

Representative delivered code:

```tsx
// project/frontend/src/components/AuthLayout.tsx:26-54
<main className="auth-page" tabIndex={-1}>
  <div className="auth-shell">
    <aside className="auth-intro" aria-label="About CrudWorks">
      <p className="auth-intro-kicker">Small tasks, clear progress</p>
      <p className="auth-intro-title">Keep the work moving.</p>
      {/* ... three value points ... */}
    </aside>

    <section className="auth-card" aria-labelledby={headingId}>
      <p className="eyebrow">{eyebrow}</p>
      <h1 id={headingId}>{heading}</h1>
      {children}
    </section>
  </div>
</main>
```

```tsx
// project/frontend/src/pages/TaskListPage.tsx:132-150
const due = task.dueDate ? formatDueDate(task.dueDate) : null
const overdue = due?.overdue === true && task.status !== 'Done'

// ...
{due && (
  <p className={`task-due${overdue ? ' task-due-overdue' : ''}`}>
    {overdue ? `Overdue · ${due.date}` : `Due ${due.date}`}
  </p>
)}
```

```csharp
// project/src/CrudWorks.Infrastructure/Persistence/Seed/DatabaseSeeder.cs:77-94
(string Title, string Description, TaskItemStatus Status, DateOnly DueDate, int MinutesAgo)[] seed =
[
    ("Prepare the quarterly report",
        "Pull the sales numbers, write the summary, and send the draft to finance.",
        TaskItemStatus.InProgress, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), 30),
    ("Review the vendor contract",
        "Compare the renewal terms with the budget and note the questions for legal.",
        TaskItemStatus.InProgress, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), 60),
    ("Book the dentist appointment",
        "Call the clinic and confirm the insurance coverage before the visit.",
        TaskItemStatus.Todo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), 120),
    ("Renew the passport",
        "Gather the photo, proof of address, and the old passport for the appointment.",
        TaskItemStatus.Todo, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), 180),
    ("Send the tax documents to the accountant",
        "Upload the receipts and confirm the accountant received the files.",
        TaskItemStatus.Done, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), 240),
];
```

## Validation

- [x] Backend build: `dotnet build CrudWorks.slnx -warnaserror` → `0 Aviso(s) 0 Erro(s)`.
- [x] Backend tests: `dotnet test CrudWorks.slnx --no-build` → Domain 37, Application 57,
      Infrastructure 25, Api 47 — **166 passed, 0 failed, 0 skipped**.
- [x] Frontend lint: `npm run lint` → exit 0.
- [x] Frontend tests: `npm run test` → **11 files, 80 tests passed**.
- [x] Frontend build: `npm run build` → 185 modules, `dist/` produced.
- [x] Live API: clean reset, demo login 200, `GET /api/tasks` 5 tasks in the new
      order, anonymous `GET /api/tasks` 401.
- [x] Live E2E: `node docs/qa/e2e-api-check.js` → **37/37**, cleanup restored the
      seeded baseline (1 user, 5 tasks).
- [x] Leak detector self-test: `node --test docs/qa/e2e-api-check.test.js` → 6/6.
- [x] Real browser (headless Chrome over the DevTools Protocol), with the live API:
      `/login` and `/tasks` at 360/768/1280 with `documentElement.scrollWidth <=
      innerWidth` at every width; login card centered at 360 (16–344 px) and the
      desktop composition centered at 1280; task rows 336 px at 360; console
      errors/exceptions **0**.
- [x] Accessibility: labels, ids, `aria-describedby`, `role="alert"`, focus order,
      and the single `main` landmark are still asserted by the 79-test suite.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| Keep the marketing brand inside the auth intro | The header already shows the same "CrudWorks" brand; the page showed it twice | Removed the duplicate `auth-brand` block from `AuthLayout` | Declutter the first screen |
| Keep the page-wide focus ring on `<main>` | After a route change the ring drew a 3 px green border around the whole content area | Set `outline: none` on `.page:focus-visible` / `.auth-page:focus-visible`; `main` is not an interactive control | The ring looked like a rendering bug in the task-list screenshot |
| Compare `due.overdue` directly | A `Done` task with a past due date showed "Overdue" in red | Only mark overdue when `task.status !== 'Done'`; added a regression test | Completed work is not overdue |
| Reseed through a migration or delete the database in the seeder | Not required; it would change schema history or destroy user data | Kept the empty-table guard and documented the reset command | Content change, not a schema change |
| Seed tasks about the build process | "Set up the CI pipeline" was Done although no CI exists; the content contradicted the user story | Five story-shaped tasks, all with description + due date | Honest demo content |
| Leave `UseStatusCodePages()` with no writer | Routing-level 404/405 returned `text/plain`, contradicting ADR-0004's "all errors are RFC 7807" | Wrote `ApiProblemDetails` from `UseStatusCodePages`; added `ProblemDetails_ForAnUnknownRoute_Returns404AsProblemJson` | Contract consistency |
| Let Escape dismiss the dialog while the delete is in flight | The buttons were disabled but the keyboard could still cancel a pending mutation | `ConfirmDialog` ignores Escape while `busy`; regression test added | Accessibility and state consistency |
| Keep the unreferenced `public/icons.svg` and the PowerShell-serialized `package.json` | Dead asset; escaped `\u0026\u0026` and inconsistent indentation in the first file a reviewer opens | Deleted the asset; rewrote `package.json` as normal 2-space JSON | Clean handover |

## Rejected suggestions

- **Tailwind or a component library:** no new dependency; the delivered CSS uses
  variables, grid, and media queries, consistent with ADR-0005.
- **A full page transition/animation layer:** rejected as unnecessary; only the
  existing short button transitions remain, with a `prefers-reduced-motion` guard.
- **Changing the API contract to return a preformatted due date:** rejected; the
  API keeps `yyyy-MM-dd` and the client formats it, so the contract stays stable.
- **Showing the demo credentials only in the README:** rejected for the demo; the
  login screen now shows them and offers a one-click fill. They are documented
  Development credentials, not secrets.

## Edge cases considered

- `YYYY-MM-DD` is parsed as a local calendar date, not UTC midnight, so a due date
  does not shift a day in a negative-offset timezone (`src/lib/dates.ts`).
- Invalid due-date strings return `null` and render no due line.
- A completed task with a past due date renders "Due …" without the overdue style.
- The login demo block is before the fields in DOM order, so the existing keyboard
  walk (Email → Password → Sign in → Create an account) still passes.
- Below 860 px the auth intro is hidden; at 360 px the card is 328 px wide inside
  the page padding and has no horizontal overflow.
- The seed keeps the 30/60/120/180/240-minute stagger, so newest-first ordering
  is deterministic and the 2/2/1 status mix survives.

## Files affected

- `project/frontend/src/components/AuthLayout.tsx` (new)
- `project/frontend/src/components/Layout.tsx`
- `project/frontend/src/pages/LoginPage.tsx`, `RegisterPage.tsx`,
  `TaskListPage.tsx`, `TaskFormPage.tsx`
- `project/frontend/src/lib/dates.ts` (new), `dates.test.ts` (new)
- `project/frontend/src/index.css`
- `project/frontend/src/pages/LoginPage.test.tsx`, `TaskListPage.test.tsx`,
  `ConfirmDialog.test.tsx`
- `project/frontend/src/components/ConfirmDialog.tsx`
- `project/frontend/package.json`, `project/frontend/public/icons.svg` (deleted)
- `project/frontend/vite.config.ts` (preview proxy)
- `project/src/CrudWorks.Infrastructure/Persistence/Seed/DatabaseSeeder.cs`
- `project/src/CrudWorks.Api/Program.cs` (RFC 7807 for routing-level failures)
- `project/tests/CrudWorks.Api.Tests/Tasks/TasksApiTests.cs`,
  `project/tests/CrudWorks.Api.Tests/Pipeline/ApiPipelineTests.cs`,
  `project/tests/CrudWorks.Infrastructure.Tests/Persistence/TaskRepositoryTests.cs`
- `specs/003-web-client/spec.md`, `specs/003-web-client/tasks.md`
- `docs/adr/0004-api-contract.md` (paging non-goal),
  `docs/04-requirement-traceability.md`, `docs/02-definition-of-done.md`,
  `docs/presentation.md`, `docs/03-stack-baseline.md`, `docs/qa/*`, `project/README.md`
- `.gitignore` (new, repository root)

## Final decision

Accepted. The change is UI/content plus two small contract/accessibility fixes
(routing-level errors are now RFC 7807, and Escape no longer cancels a pending
delete); no endpoint, schema, migration, auth, ownership, or API-contract shape
changed. The full gate is green on the current
tree: 166 backend + 80 frontend + 37 E2E + 6 self-tests = **289 automated checks**.
