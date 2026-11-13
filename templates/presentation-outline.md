# Presentation Outline (10–15 minutes)

## 1. Hook and user story (2 min)

- Who the user is and what problem they have.
- The informal user story.
- The demo promise: "By the end, you will see a user register, log in, and manage their own tasks."

## 2. Demo (4 min)

1. Open the app on a clean database.
2. Register a new user.
3. Log in with the seeded demo user.
4. Create a task.
5. Edit it.
6. Change its status.
7. Delete it.
8. Show the unauthorized state (logged out → protected route redirects).
9. Show the API docs and health endpoint.

## 3. Architecture (3 min)

- Four projects and the dependency rule.
- Why Clean Architecture for a small app: testability, explicit boundaries, reviewability.
- Data model: `Users`, `Tasks`, ownership.
- Auth flow: register → hash → login → JWT → authorized requests.

## 4. Clean Architecture walkthrough (2 min)

- Show the project references.
- Show a controller delegating to an application use case.
- Show the repository interface in `Application` and the EF implementation in `Infrastructure`.
- Show a domain invariant and where it is enforced.

## 5. TDD evidence (2 min)

- One red → green → refactor example.
- The test pyramid and coverage.
- What is tested at each layer and why.

## 6. GenAI workflow (2 min)

- The prompt used.
- A representative output.
- What was validated, corrected, and rejected.
- How edge cases, auth, and validation were handled.
- What the AI was not allowed to decide.

## 7. Trade-offs and lessons (1 min)

- What was intentionally left out.
- What would change in production.
- What was learned.

## 8. Q&A prep

### Likely questions

- Why Clean Architecture for a small app?
- Why not MediatR/CQRS?
- Why JWT in `sessionStorage`? What would change in production?
- How do you prevent one user from accessing another user's task?
- What did the AI get wrong, and how did you catch it?
- How do you know the tests are meaningful?
- What would you do with more time?
- How would you deploy this?

### Demo fallback

- If the live demo fails, use the recorded walkthrough or the API tests as evidence. Never pretend the demo ran.

## Seed credentials

- Email: <demo email>
- Password: <demo password>
- Note: development/demo only.
