# Handoff

Read this first when starting a fresh session. Spec lives outside this repo (career-os); the
summary of what matters is in `docs/stories.md` and `AGENTS.md`.

## Current state
- Block 0 done: .NET 10.0.401, Docker Desktop (Rosetta), SQL Server 2022 container `fieldcheck-sql` on 1433, Azurite `fieldcheck-azurite` on 10000 (run as amd64), repo public with secret scanning.
- Block 1 done pending CI: solution skeleton, smoke tests, CI workflow on branch `chore/block1-docs-skeleton-ci`.

## Next step
- Checkpoint (a) approval, then Block 2: entities, DbContext, migrations, seed data; T-SQL objects in a migration.

## Open issues
- Azurite arm64 image fails with `exec format error` on this Docker engine; using `--platform linux/amd64`.
- `az login` not done yet; needed only at checkpoint (d).
- `dotnet` is at `/usr/local/share/dotnet`; add to PATH in new shells if missing.
