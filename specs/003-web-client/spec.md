# SPEC-003 — Web client experience

- **Status:** implemented
- **Owner:** architect
- **Approved by:** orchestrator
- **Requirements:** R-14, R-15, R-16, R-17
- **ADRs:** 0002, 0004, 0005
- **Last updated:** 2026-11-11

## 1. Problem

The API is only half the product. A reviewer must be able to register, log in, and
manage tasks from a browser without instructions, on a phone or a desktop, with
clear feedback when something is slow, empty, or broken.

## 2. Scope

**In scope**

- React + TypeScript + Vite application.
- Login, registration, task list, task create/edit, not-found.
- Centralized auth state and API client.
- Loading, empty, error, and success states for every async view.
- Responsive layout at 360, 768, and 1280 px.
- Accessible labels, errors, focus, and keyboard flow.
- Component tests for the critical behavior.

**Out of scope**

- Dark mode, i18n, animations, drag-and-drop, offline mode.
- A design system or component library.
- Rich text, attachments, comments.
- Server-side rendering.

## 3. Actors

| Actor | Goal |
| --- | --- |
| Visitor | Reach the login or registration screen and understand errors. |
| User | Complete the task flow quickly, on any screen size. |
| Reviewer | See a coherent, intentional interface with no console errors. |

## 4. Routes

| Route | Access | Screen |
| --- | --- | --- |
| `/login` | public | Login form |
| `/register` | public | Registration form |
| `/` | protected | Task list |
| `/tasks/new` | protected | Create task |
| `/tasks/:id/edit` | protected | Edit task |
| `*` | public | Not found |

An unauthenticated visit to a protected route redirects to `/login`. After login,
the user returns to the originally requested route.

## 5. Acceptance criteria

### AC-003-01 — The auth guard protects the task routes

- **Given** no stored session
- **When** the user opens `/`
- **Then** the app navigates to `/login`
- **And** no task request is sent.

### AC-003-02 — Login stores the session and returns to the target

- **Given** a valid email and password
- **When** the user submits the login form
- **Then** the token and user summary are stored through the auth module
- **And** the app navigates to `/` or the preserved route.

### AC-003-03 — Invalid login is explained

- **Given** wrong credentials
- **When** the form is submitted
- **Then** a `401` message is shown without clearing the email
- **And** the submit control is enabled again.

### AC-003-04 — Registration validates before and after submit

- **Given** the registration form
- **When** a field is invalid
- **Then** a field-level message is shown and linked to the input
- **And** a duplicate email (`409`) is shown as a safe form-level message.

### AC-003-05 — The task list has four states

- **Given** the task list route
- **When** the request is pending, empty, failing, or successful
- **Then** the UI shows a loading state, an empty state, an error state with retry,
  or the task rows respectively.

### AC-003-06 — Create and edit share one validated form

- **Given** the task form
- **When** the user submits valid data
- **Then** the app sends `POST /api/tasks` or `PUT /api/tasks/{id}` and returns to
  the list with the change visible.
- **And** server `400` field errors are mapped to the matching inputs.

### AC-003-07 — Delete asks for confirmation

- **Given** a task row
- **When** the user chooses delete
- **Then** the app asks for confirmation, sends `DELETE` only after confirmation,
  and removes the row on `204`.

### AC-003-08 — An expired session returns to login

- **Given** a protected request that returns `401`
- **When** the response arrives
- **Then** the auth module clears the session and redirects to `/login`
- **And** no retry loop occurs.

### AC-003-09 — The layout works at three widths

- **Given** 360, 768, and 1280 px viewports
- **When** the list and form are used
- **Then** there is no horizontal scroll and no clipped primary action.

### AC-003-10 — The flow is keyboard and screen-reader friendly

- **Given** a keyboard-only user
- **When** they move through the forms and dialogs
- **Then** focus is visible, labels are programmatic, errors are announced, and
  Escape closes the confirmation dialog.

## 6. Rules

- One API client module owns the base URL, authorization header, JSON handling,
  and error mapping.
- One auth module owns the session; components never read storage directly.
- TanStack Query owns server state; local state is for form and UI only.
- No `any`, no `console.log`, no secrets in the bundle.
- Token storage is `sessionStorage` (ADR-0002); the trade-off is documented.
- The client never invents fields or endpoints; the contract is ADR-0004.

## 7. Design constraints

- Calm, light interface; one accent color; one spacing and type scale.
- Task rows show title, status, due date, and actions; description is shown in the
  form and may be truncated in the list.
- Empty state explains the next action, not just "no data".
- Destructive actions are visually distinct and confirmed.

## 8. Non-functional requirements

- **Accessibility:** WCAG AA contrast, visible focus, real labels, `role="alert"`
  for errors, dialog focus management.
- **Performance:** first meaningful render under two seconds locally; no waterfall
  of avoidable requests.
- **Resilience:** network errors show a retry path; a missing task shows a safe
  not-found state.
- **Testability:** auth guard, login, list states, form validation, and delete
  confirmation are covered by component tests.

## 9. Dependencies and assumptions

- The API contract in ADR-0004 is stable before the UI is wired.
- The Vite dev server proxies `/api` to `http://localhost:5080`.
- The demo user from the seed is documented in the README.

## 10. Open questions

None. Visual polish is deliberately subordinate to the working flow.

## 11. Change log

| Date | Change | Reason | Approved by |
| --- | --- | --- | --- |
| 2026-11-11 | Initial spec | Required client behavior | Orchestrator |
| 2026-11-12 | Status moved from `approved` to `implemented` | Every acceptance criterion is covered by an automated test or a real-browser walkthrough; AC-003-10 is partially satisfied because no screen reader is available in this environment, and that limitation is recorded in `docs/ai/015` | Orchestrator |
| 2026-11-13 | Auth screens rebuilt on a centered `AuthLayout`, demo-access block added, task list/form polished | Owner request: the login card was left-aligned in an empty page; the demo should start in one click | Orchestrator |
