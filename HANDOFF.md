# Handoff

Read this first when starting a fresh session. Spec lives outside this repo (career-os); the
summary of what matters is in `docs/stories.md` and `AGENTS.md`.

## Current state
- Block 0 done: .NET 10.0.401, Docker Desktop (Rosetta), SQL Server 2022 container `fieldcheck-sql` on 1433, Azurite `fieldcheck-azurite` on 10000 (run as amd64), repo public with secret scanning.
- Block 1 merged (PR #1): solution skeleton, smoke tests, CI.
- Block 2 on branch `story/schema-migrations`: entities, DbContext, migrations (InitialSchema + TsqlObjects), T-SQL in `src/FieldCheck.Api/Data/Sql/`, demo seeder. Applied and verified against local SQL Server.

## Next step
- Checkpoint (b) approval, then Block 3: S1–S3 endpoints (DTOs, validation, ProblemDetails, critical-inspection transaction, rowversion concurrency), then S4 report + S5 OData, then tests.

## Open issues
- Azurite arm64 image fails with `exec format error` on this Docker engine; using `--platform linux/amd64`.
- `az login` not done yet; needed only at checkpoint (d).
- Local connection string is in `dotnet user-secrets` for `src/FieldCheck.Api` (key `ConnectionStrings:FieldCheck`). Apply schema with `dotnet ef database update -p src/FieldCheck.Api`; seed runs on Development startup when `Seed:Demo` is true.
- `dotnet` is at `/usr/local/share/dotnet`; add to PATH in new shells if missing.
