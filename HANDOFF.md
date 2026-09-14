# Handoff

Read this first when starting a fresh session. Spec lives outside this repo (career-os); the
summary of what matters is in `docs/stories.md` and `AGENTS.md`.

## Current state
- Block 0 done: .NET 10.0.401, Docker Desktop (Rosetta), SQL Server 2022 container `fieldcheck-sql` on 1433, Azurite `fieldcheck-azurite` on 10000 (run as amd64), repo public with secret scanning.
- Block 1 merged (PR #1): solution skeleton, smoke tests, CI.
- Block 2 merged (PR #2): entities, DbContext, migrations (InitialSchema + TsqlObjects), T-SQL in `src/FieldCheck.Api/Data/Sql/`, demo seeder.
- Block 3 merged (PR #3): S1–S3 endpoints. Block 4 merged (PR #4): S4 report, S5 OData, /health, Scalar.
- Block 5 on branch `story/tests` (PR #5): 13 unit + 24 integration tests, all green locally.

## Next step
- Checkpoint (c) approval, then checkpoint (d) plan for Azure (needs `az login`), then S6 photos (Azurite locally), README, evidence.

## Open issues
- Azurite arm64 image fails with `exec format error` on this Docker engine; using `--platform linux/amd64`.
- `az login` not done yet; needed only at checkpoint (d).
- Local connection string is in `dotnet user-secrets` for `src/FieldCheck.Api` (key `ConnectionStrings:FieldCheck`). Apply schema with `dotnet ef database update -p src/FieldCheck.Api`; seed runs on Development startup when `Seed:Demo` is true.
- `dotnet` is at `/usr/local/share/dotnet`; add to PATH in new shells if missing.
