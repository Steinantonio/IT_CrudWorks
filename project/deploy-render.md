# Deploying CrudWorks to Render

A single Render web service (Docker) serves both the ASP.NET Core API and the
built React SPA from the same origin, so relative `/api` paths keep working and
no CORS configuration is needed.

## Prerequisites

- A Render account (free) and access to this GitHub repository.
- A strong JWT signing key. Generate one locally:
  ```powershell
  # PowerShell
  [Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
  ```
  ```sh
  # bash / WSL / Git Bash
  openssl rand -base64 64
  ```

## Option A — Blueprint deploy (recommended)

1. In the Render Dashboard: **New → Blueprint**, select this repository.
2. Render reads `render.yaml` at the repo root and creates the `crudworks` web
   service.
3. When prompted, paste the value for `Jwt__SigningKey` (the secret marked
   `sync: false`). Do not put it in git.
4. The other variables (`Database__RunMigrationsAndSeed=true`,
   `Spa__Serving=true`, `ASPNETCORE_ENVIRONMENT=Production`) are set by the
   blueprint.

## Option B — Manual web service

1. **New → Web Service**, select the repository.
2. Runtime: **Docker**. Dockerfile path: `project/Dockerfile`. Health check path:
   `/api/health`.
3. Plan: **Free**. Region: any.
4. Environment variables (add them all):
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `Jwt__SigningKey` = (your generated key)
   - `Database__RunMigrationsAndSeed` = `true`
   - `Spa__Serving` = `true`
5. Create. First deploy builds the Docker image (~a few minutes).

## First boot

On the first successful boot the app applies EF Core migrations and runs the
seeder, so the database is created from scratch and the demo account is ready:

- Email: `demo@crudworks.local`
- Password: `Demo123!`

Log in at the deployed URL and use the **Use demo account** button.

## Free-tier behavior (read before the demo)

- **Spin down after 15 minutes idle**, **~1 minute cold start** on the next
  request. Render shows a loading page while it wakes.
- **SQLite is ephemeral**: every spin-down, redeploy, or restart wipes the
  database. The seeder recreates the demo user and the five seeded tasks, so the
  demo always starts from a known state — but any task you create during a
  session is gone after the service sleeps.
- **Warm it up** before presenting: open the URL about a minute before the
  interview and click through login + the task list once.
- 750 free instance-hours per month; bandwidth and build minutes also count
  against the included monthly amounts.

## Redeploying

Push to `main` — Render auto-builds (Docker) and redeploys. The database is
recreated and reseeded on boot.
