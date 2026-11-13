# AI Evidence Log

## Entry 022

- **Date:** 2026-11-13
- **Agent:** backend (it_crudworks/backend-implementer) + QA (it_crudworks/qa-engineer, work executed by the orchestrator after the QA subagent hit the OpenCode Go 5-hour usage limit)
- **Task:** Deployment to Render — flags in the API for migrations/seed and same-origin SPA serving, plus Dockerfile/render.yaml/deploy docs
- **Tool/model:** OpenCode subagent (GLM-5.3) for the backend slice; orchestrator for the Docker/Render artifacts after the QA subagent was cancelled by the quota

## Prompt

Adjust the app to publish on Render.com as a single Docker web service serving both the API and the built React SPA from the same origin: (1) allow migrations + seed outside Development via a flag; (2) serve the SPA from `wwwroot` behind a flag while keeping `/api/*` returning RFC 7807 problem+json; (3) add a multi-stage Dockerfile, `.dockerignore`, `render.yaml` blueprint and `deploy-render.md`. Constraints: default behavior unchanged (flags off), all existing tests green, no secrets in git, TDD for the backend slice.

## Output summary

- Backend: `Program.cs` gained `Spa:Serving` (default false) — `UseDefaultFiles`/`UseStaticFiles` + a single `MapFallback` that returns the same problem+json 404 for unknown `/api/*` (reuses `ApiProblemDetails.Create` + `StatusTitle`) and `index.html` otherwise; missing `wwwroot/index.html` fails startup. `Database:RunMigrationsAndSeed` (default false) — migrate+seed runs in any environment when set, crashing startup on DB errors. Defaults added to `appsettings.json`. 8 new API tests in `DeploymentApiFactories.cs`, `SpaServingTests.cs`, `DatabaseStartupTests.cs`.
- QA artifacts: `project/Dockerfile` (node → sdk → aspnet, `ASPNETCORE_HTTP_PORTS=10000`), `project/.dockerignore`, `render.yaml` (free web service, health check `/api/health`, `sync: false` for `Jwt__SigningKey`), `project/deploy-render.md`.

## Validation

- [x] Build: `dotnet build CrudWorks.slnx -warnaserror --nologo` → 0 warnings, 0 errors
- [x] Unit/integration tests: `dotnet test CrudWorks.slnx --nologo` → 174 passed (Domain 37, Application 57, Infrastructure 25, Api 55), 0 failed, 0 skipped (baseline 166 + 8 new)
- [x] Release publish: `dotnet publish src/CrudWorks.Api -c Release` → succeeded
- [x] Frontend build: `npm run build` → 188 modules, built in 302 ms (unchanged since the previous slice)
- [ ] Docker build: Docker is **not installed** on this machine — could not be verified locally. The Dockerfile is written from the documented .NET 10 multi-stage pattern; the Render build itself is the first real run (it will fail loudly if a stage is wrong).
- [x] Manual behavior: the backend slice was verified by the subagent with a RED→GREEN run (4 failures then green) plus a Production-environment test that logs in with the seeded demo credentials and confirms ≥5 tasks.

## Corrections

| What the AI suggested | What was wrong | What we changed | Why |
| --- | --- | --- | --- |
| In-memory `"webroot"` config key to point at a temp directory | Rejected by the .NET 10 minimal host — `WebHostDefaults.WebRootKey` must be set via `builder.UseSetting` | Use `builder.UseSetting(WebHostDefaults.WebRootKey, path)` in the test factory | Discovered with a 2-minute empirical probe (deleted afterward) |
| Single public constructor on the xunit fixture class | xunit class fixtures allow exactly one public constructor | Made the `SpaServingApiFactory(bool)` constructor `internal` | Discovered in the RED phase |

## Edge cases considered

- `WebRootPath` is `null` when no `wwwroot` exists → `ResolveSpaIndexPath` handles it.
- `Spa:Serving=true` but `wwwroot/index.html` missing → startup `InvalidOperationException` naming the flag and the path (fail-fast, matching the missing-signing-key contract).
- Unknown `/api/*` still returns the RFC 7807 problem+json 404 even in SPA mode (existing `ProblemDetails_ForAnUnknownRoute_Returns404AsProblemJson` stays green).
- `Database:RunMigrationsAndSeed=true` with a DB error → startup crash (no try/catch), same fail-fast contract.
- `Database:RunMigrationsAndSeed=false` outside Development → healthy host, no SQLite file created (proven by test).

## Auth and validation decisions

- `Jwt__SigningKey` is never baked into the image or the repo; it is set in the Render dashboard (`sync: false` in `render.yaml`). Missing key → fail-fast at startup, same as locally.
- SQLite is ephemeral on Render's free tier: migrations + seed run at boot, so the demo always starts from a known state (demo user + 5 tasks).
- `Cors__AllowedOrigin` is left unset because SPA and API share one origin.

## Files affected

- `project/src/CrudWorks.Api/Program.cs`
- `project/src/CrudWorks.Api/appsettings.json`
- `project/tests/CrudWorks.Api.Tests/Pipeline/DeploymentApiFactories.cs` (new)
- `project/tests/CrudWorks.Api.Tests/Pipeline/SpaServingTests.cs` (new)
- `project/tests/CrudWorks.Api.Tests/Pipeline/DatabaseStartupTests.cs` (new)
- `project/Dockerfile` (new)
- `project/.dockerignore` (new)
- `render.yaml` (new, at repo root)
- `project/deploy-render.md` (new)

## Final decision

Accepted. Backend fully green (174/174). Docker image could not be built locally (Docker unavailable); the Render build is the first real run and will fail loudly if a stage is wrong.
