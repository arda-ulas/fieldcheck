# FieldCheck

A small web API for tracking equipment inspections at industrial sites. Sites own assets (pumps,
conveyors, transformers). Each asset has an inspection interval. Inspectors log inspections with a
severity; a critical finding takes the asset out of service. Supervisors query inspections through
OData and pull an overdue-assets report backed by a T-SQL stored procedure. Photos go to Azure Blob
Storage.

This is a one-day personal portfolio project (built 2026-09-14) whose purpose is to show working,
tested code on this stack: **C# / ASP.NET Core (.NET 10), SQL Server and hand-written T-SQL, EF Core,
OData, Azure (App Service, Azure SQL, Blob Storage), xUnit + Testcontainers, GitHub Actions.** It has
no users and makes no claims about production readiness or scale.

## Architecture

```
client ──HTTP/JSON──▶ ASP.NET Core Web API (controllers, ProblemDetails, OpenAPI + Scalar)
                        ├── EF Core ──▶ SQL Server (local Docker) / Azure SQL
                        │     ├── migrations own the schema
                        │     ├── dbo.usp_GetOverdueAssets   (hand-written T-SQL, called with parameters)
                        │     └── dbo.vw_AssetInspectionSummary
                        ├── ASP.NET Core OData ──▶ /odata/Inspections ($filter/$orderby/$top/$count)
                        └── Azure.Storage.Blobs ──▶ Azurite (local) / Blob Storage (private container, SAS links)

Hosting: Azure App Service (Linux) · CI: GitHub Actions (build + unit + integration tests on real SQL Server)
```

Layout: `src/FieldCheck.Api` (API), `tests/FieldCheck.UnitTests`, `tests/FieldCheck.IntegrationTests`,
`docs/` (stories, AI log), `AGENTS.md` (rules for AI-assisted work), `HANDOFF.md` (session state).

## Run locally

Prerequisites: .NET 10 SDK, Docker (on Apple Silicon enable *Use Rosetta for x86_64/amd64 emulation*;
the SQL Server image is amd64-only).

```bash
# SQL Server 2022 (pick your own password; it stays on your machine)
docker run --platform linux/amd64 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<strong-password>' \
  -p 1433:1433 --name fieldcheck-sql -d mcr.microsoft.com/mssql/server:2022-latest

# Azurite (blob emulator). --platform is a workaround for an arm64 image that failed to exec on Docker 29.
docker run --platform linux/amd64 -p 10000:10000 --name fieldcheck-azurite -d \
  mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0

# Connection string via user-secrets (never in the repo)
dotnet user-secrets set "ConnectionStrings:FieldCheck" \
  "Server=localhost,1433;Database=FieldCheck;User Id=sa;Password=<strong-password>;TrustServerCertificate=True" \
  -p src/FieldCheck.Api

# Apply the schema deliberately (the app never migrates on startup), then run
dotnet tool install -g dotnet-ef
dotnet ef database update -p src/FieldCheck.Api
dotnet run --project src/FieldCheck.Api
```

In Development the app seeds one demo site with 10 assets (`Seed:Demo` in
`appsettings.Development.json`) and serves the OpenAPI UI at `/scalar/v1`. Blob storage points at
Azurite through `UseDevelopmentStorage=true`, which is a well-known emulator string, not a secret.

Try it:

```bash
curl "http://localhost:5000/api/reports/overdue-assets?siteId=1"
curl "http://localhost:5000/odata/Inspections?\$filter=Severity eq 'Critical'&\$orderby=InspectedAtUtc desc&\$top=10&\$count=true"
curl http://localhost:5000/health
```

## Run tests

```bash
dotnet test
```

Unit tests need nothing. Integration tests start a real SQL Server 2022 container through
Testcontainers (Docker required), apply all migrations including the T-SQL objects, and run the API
in-process with `WebApplicationFactory`. Blob storage is replaced by an in-memory double in tests.
The same suite runs in GitHub Actions on every push and pull request; `main` only changes through
pull requests with CI green.

## Stories and tests

Full acceptance criteria with the test that covers each one: [`docs/stories.md`](docs/stories.md).

| Story | Endpoint | Key behaviour | Tests |
|---|---|---|---|
| S1 Register assets | `POST /api/sites/{siteId}/assets` | 201 + Location; duplicate tag per site → 409 (unique index); interval outside 1–365 → 400 naming the field | `AssetsTests` |
| S2 Log inspection | `POST /api/assets/{id}/inspections` | Critical severity sets the asset `OutOfService` in the same transaction; future date → 400 | `SeverityRuleTests`, `InspectionsTests` |
| S3 Return to service | `PUT /api/assets/{id}/status` | Optimistic concurrency on `rowversion`; stale token → 409 | `AssetStatusTests` |
| S4 Overdue report | `GET /api/reports/overdue-assets?siteId=` | Stored procedure; never-inspected first, then by `DaysOverdue`; out-of-service excluded | `OverdueCalculatorTests`, `OverdueReportTests` |
| S5 Query inspections | `GET /odata/Inspections` | `$filter/$orderby/$top/$skip/$count/$select`; `$top` ≤ 100; other options → 400 | `ODataTests` |
| S6 Photos | `POST /api/inspections/{id}/photos`, `GET /api/photos/{id}` | JPEG/PNG ≤ 5 MB with magic-byte check; private container; 10-minute read SAS | `PhotosTests` |
| Ops | `GET /health` | Database connectivity | `HealthTests` |

## Data model and T-SQL

Tables: `Sites`, `Assets`, `Inspections`, `InspectionPhotos` (see `Data/FieldCheckDbContext.cs`).
Enums (`Status`, `Severity`) are stored as **strings with CHECK constraints**, so the database rejects
values the application does not know about and the data reads clearly in any SQL client. EF Core
migrations are the source of truth for the schema; the T-SQL objects are applied by the
`TsqlObjects` migration from the `.sql` files in
[`src/FieldCheck.Api/Data/Sql/`](src/FieldCheck.Api/Data/Sql/) so they version with the schema.

**`dbo.usp_GetOverdueAssets @SiteId INT = NULL`** — for each in-service asset, `OUTER APPLY` a
`TOP (1) ... ORDER BY InspectedAtUtc DESC` subquery to get the latest inspection, compute
`DaysOverdue` with `DATEDIFF`, keep rows where there is no inspection or the gap exceeds the interval,
and order never-inspected assets first, then most overdue. `OUTER` rather than `CROSS` APPLY is what
keeps the never-inspected assets. `OPTION (RECOMPILE)` lets the optimizer choose a seek or a scan per
call for the optional `@SiteId` filter instead of reusing one plan for both shapes.

**`dbo.vw_AssetInspectionSummary`** — asset + last inspection date + last severity + inspection
count, using the same APPLY pattern.

**Indexes.** `IX_Inspections_AssetId_InspectedAtUtc` on `(AssetId, InspectedAtUtc DESC)` makes the
"latest inspection per asset" lookup a range seek that reads one row: the index is already ordered
the way the `TOP (1)` wants it, so there is no sort and no scan of the asset's other inspections.
`IX_Assets_SiteId_Status` serves the report's `WHERE Status = 'InService' AND SiteId = @SiteId`
predicate. A unique index on `(SiteId, Tag)` enforces the per-site tag rule and is what produces the
409 on duplicates (the API catches SQL errors 2601/2627 rather than pre-checking, which would race).

The procedure is called from C# with `Database.SqlQuery<T>($"EXEC dbo.usp_GetOverdueAssets @SiteId = {siteId}")`;
the interpolated value becomes a SQL parameter, never string concatenation.

## Deployment (Azure)

Target: resource group `rg-fieldcheck` in Canada Central with Azure SQL Database, a Storage account
with a private `inspection-photos` container, and a Linux App Service running the API. Connection
strings live in App Service configuration, not in the repo. The schema is applied deliberately with
`dotnet ef migrations script --idempotent` against Azure SQL; the app never migrates on startup.

Status and smoke-test output: see the *Deployment record* section below once the deployment exists.

## AI-assisted workflow

This project was built with Claude Code (Claude Fable 5.1) under the rules in [`AGENTS.md`](AGENTS.md):
one story per branch, a plan approved before code, no secrets in prompts or code, every diff read
before commit, tests green before merge, package APIs verified rather than guessed, and the T-SQL
hand-written and explained. [`docs/ai-log.md`](docs/ai-log.md) records, per story, what was asked,
what the agent produced, what was changed or rejected and why, and how it was verified.

## Known limitations

- No authentication or authorization; every endpoint is open. Out of scope for a one-day project.
- Photos are validated by content type, size, and magic bytes only; no image decoding.
- Orphan blobs are possible if the metadata insert fails after a successful upload (documented in
  `PhotosController`); there is no cleanup job.
- The overdue rule uses whole UTC days (`DATEDIFF(DAY, ...)`), so an inspection late in the day and
  a check early the next day count as one day apart.
- Integration tests share one SQL Server container per run and isolate by creating a site per test;
  they do not run in parallel across classes.
- The Azurite arm64 image failed to start on this machine's Docker engine; the README uses the amd64
  image under emulation.
- Deployed to Azure only for the evaluation period noted in the deployment record.
