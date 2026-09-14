# Handoff

Read this first when starting a fresh session. Spec lives outside this repo (career-os); the
summary of what matters is in `docs/stories.md` and `AGENTS.md`.

## Current state
- Block 0 done: .NET 10.0.401, Docker Desktop (Rosetta), SQL Server 2022 container `fieldcheck-sql` on 1433, Azurite `fieldcheck-azurite` on 10000 (run as amd64), repo public with secret scanning.
- Block 1 merged (PR #1): solution skeleton, smoke tests, CI.
- Block 2 merged (PR #2): entities, DbContext, migrations (InitialSchema + TsqlObjects), T-SQL in `src/FieldCheck.Api/Data/Sql/`, demo seeder.
- Block 3 merged (PR #3): S1–S3 endpoints. Block 4 merged (PR #4): S4 report, S5 OData, /health, Scalar.
- Block 5 merged (PR #5): 13 unit + 24 integration tests.
- S6 merged (PR #6), README merged (PR #7), idempotent-migration fix merged (PR #8).
- Azure deployed 2026-09-14: see README *Deployment record* and `docs/evidence/smoke-live-2026-09-14.md`. Live URL https://app-fieldcheck-dcb438.azurewebsites.net

## Next step
- Redeploy from final `main` SHA, clean-clone test run, spec §12 evidence checklist; later: delete `rg-fieldcheck` and record the date in `docs/evidence/`.

## Open issues
- Azurite arm64 image fails with `exec format error` on this Docker engine; using `--platform linux/amd64`.
- Azure SQL admin password and storage key exist only in App Service configuration (and the SQL password in a local scratch file outside the repo). Not in user-secrets; if needed locally, reset via `az sql server update --admin-password`.
- App Service is F1: cold starts of ~30 s are normal; the SQL free offer auto-pauses when the monthly limit is exhausted.
- Local connection string is in `dotnet user-secrets` for `src/FieldCheck.Api` (key `ConnectionStrings:FieldCheck`). Apply schema with `dotnet ef database update -p src/FieldCheck.Api`; seed runs on Development startup when `Seed:Demo` is true.
- `dotnet` is at `/usr/local/share/dotnet`; add to PATH in new shells if missing.
