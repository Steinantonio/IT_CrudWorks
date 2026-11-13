# ADR-0005: Frontend Architecture

- **Status:** accepted
- **Date:** 2026-11-11
- **Decision owner:** architect
- **Related requirements:** R-14, R-15, R-16, R-17
- **Related spec:** `specs/003-web-client/spec.md`

## Context

The frontend must cover authentication and task CRUD, stay usable at 360/768/1280,
and remain reviewable in a two-day timebox. The scaffold already includes Vite 8,
React 19, TypeScript 7, TanStack Query, React Hook Form, Zod, and Vitest 5. The
question is how much structure to add on top without building a design system.

## Decision

Use a **small feature-first structure with one API client and one auth module**.

```
src/
├── auth/          # session storage, AuthProvider, RequireAuth
├── components/    # Layout, ErrorState, StatusBadge, ConfirmDialog
├── lib/           # api client, query keys
├── pages/         # Login, Register, TaskList, TaskForm, NotFound
├── test/          # setup
└── App.tsx        # router and providers
```

### State

- **Server state:** TanStack Query. Keys: `['tasks']`, `['tasks', id]`, `['me']`.
  Mutations invalidate the list and the affected item.
- **Auth state:** React context. The token and user summary are persisted in
  `sessionStorage` under `crudworks.session`. Only `auth/session.ts` reads or
  writes storage.
- **Form state:** React Hook Form + Zod. The schema mirrors ADR-0004 but the API
  remains authoritative.
- **No global state library.** The app has two kinds of state; a third would be
  ceremony.

### Data access

- One `lib/api.ts` owns the base URL (`VITE_API_BASE_URL`), the `Authorization`
  header, JSON handling, and ProblemDetails → `ApiError` mapping.
- A `401` on a protected request clears the session and redirects to `/login`
  once; the client must not retry in a loop.
- The client never invents fields or endpoints.

### Routing

`react-router-dom` with public routes (`/login`, `/register`), protected routes
(`/`, `/tasks/new`, `/tasks/:id/edit`), and a catch-all. `RequireAuth` preserves
the attempted path so login can return to it.

### Styling and responsiveness

Plain CSS with custom properties: one accent color, one spacing scale, one type
scale. No Tailwind, no component library. The layout is a single column at
360 px and two columns at 768 px and above; the list becomes cards on narrow
screens. Focus styles are never removed.

### Testing

Vitest + React Testing Library + `jsdom`. Tests assert behavior through roles and
labels. Network calls are mocked at the `fetch` boundary or by injecting a fake
API client; no test hits a real server. Minimum coverage is listed in SPEC-003.

## Alternatives considered

| Option | Pros | Cons | Why not |
| --- | --- | --- | --- |
| Feature folders per domain (`features/auth`, `features/tasks`) | Scales well | More indirection for five screens | The app is too small to earn it |
| Redux Toolkit | Predictable, familiar | Duplicates server state and adds boilerplate | TanStack Query already owns server state |
| Tailwind | Fast styling | Setup and class noise; another failure point | Plain CSS is enough and reviews cleanly |
| Component library | Polished quickly | Bundle weight, theming, accessibility unknowns | The brief needs a working flow, not a design system |
| Next.js | SSR, routing | Server rendering is unnecessary; more moving parts | Vite SPA is the right size |

## Consequences

### Positive

- The data flow is obvious: page → query/mutation → API client → contract.
- Auth and storage have exactly one owner, which makes the security review simple.
- Tests can run without a backend.
- The UI can be built in parallel with the API from ADR-0004.

### Negative / trade-offs

- `sessionStorage` is exposed to XSS; documented in ADR-0002. The app must not
  inject untrusted HTML.
- Plain CSS will not scale to a large product; it is appropriate for this demo.
- No offline cache, optimistic updates, or background refresh. Not required.

## Validation

- `npm run lint`, `npm run test`, and `npm run build` pass with captured output.
- The auth guard redirects an unauthenticated user and preserves the return path.
- A `401` during a protected call clears the session exactly once.
- 360/768/1280 checks: no horizontal scroll, no clipped actions.
- Keyboard-only pass: visible focus, labeled inputs, Escape closes the dialog.
- Browser console is clean during the demo flow.

## Links

- `specs/003-web-client/spec.md`
- `docs/adr/0002-auth-strategy.md`, `docs/adr/0004-api-contract.md`
- `project/frontend/`
