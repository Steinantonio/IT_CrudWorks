# PLAN-003 — Web client implementation plan

- **Spec:** `specs/003-web-client/spec.md`
- **Status:** approved
- **Owner:** architect
- **Last updated:** 2026-11-11

## 1. Architecture impact

| Area | Change |
| --- | --- |
| Shell | Router, providers, layout, global styles. |
| Auth | `auth/session.ts`, `AuthProvider`, `RequireAuth`, login/register pages. |
| Data | `lib/api.ts` client; TanStack Query keys and mutations. |
| Tasks | List page, form page, delete dialog, status badge. |
| Tests | Vitest + React Testing Library + mocked API client or `fetch`. |

## 2. Folder shape

```
src/
├── auth/
│   ├── AuthProvider.tsx
│   ├── RequireAuth.tsx
│   ├── session.ts
│   └── types.ts
├── components/
│   ├── Layout.tsx
│   ├── ErrorState.tsx
│   └── StatusBadge.tsx
├── lib/
│   └── api.ts
├── pages/
│   ├── LoginPage.tsx
│   ├── RegisterPage.tsx
│   ├── TaskListPage.tsx
│   ├── TaskFormPage.tsx
│   └── NotFoundPage.tsx
├── test/setup.ts
├── App.tsx
└── main.tsx
```

Feature-first only where it earns its keep; a single `pages/` folder is enough for
this size.

## 3. State and contract

- Server state: TanStack Query. Keys: `['tasks']`, `['tasks', id]`, `['me']`.
- Mutations invalidate `['tasks']` (and the individual key on edit/delete).
- Auth: React context with `sessionStorage` persistence under `crudworks.session`.
- 401 from any protected call clears the session and redirects to `/login`.
- API client maps ProblemDetails to a typed `ApiError`; field errors reach the form.

## 4. Test strategy

| Test | Proves |
| --- | --- |
| `RequireAuth` redirects without a session | AC-003-01 |
| Login stores the session and navigates | AC-003-02, AC-003-03 |
| Task list renders loading, empty, error, data | AC-003-05 |
| Form validates title/description and submits | AC-003-06 |
| Delete asks for confirmation | AC-003-07 |
| `401` handler clears the session | AC-003-08 |

Manual checks: 360/768/1280 widths, keyboard-only flow, browser console clean.

## 5. Task order

1. Router shell + session module + guard.
2. API client and query provider.
3. Login and register pages.
4. Task list states.
5. Create/edit form.
6. Delete confirmation.
7. Responsive and accessibility pass.
8. Full `lint`, `test`, `build`.

## 6. Risks and fallbacks

| Risk | Impact | Fallback |
| --- | --- | --- |
| Test setup fights Vite 8 / Vitest 5 | No frontend tests | Keep the guard and form tests; document the gap |
| Over-designed UI | Time lost | Plain CSS, one accent, three screens |
| Token handling scattered | Security drift | Only `session.ts` reads storage; lint/review it |
| API unavailable during UI work | Blocked | Mock `fetch` in tests; use the real API for the final pass |

## 7. Definition of done

- [ ] Every acceptance criterion in SPEC-003 has a test or a named manual check.
- [ ] `npm run lint`, `npm run test`, `npm run build` pass.
- [ ] 360/768/1280 checked; no horizontal scroll.
- [ ] No `any`, no `console.log`, no secrets.
- [ ] AI evidence entry exists.
- [ ] Reviewer has no open blockers.
