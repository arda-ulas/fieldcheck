# User stories and acceptance criteria

Each criterion names the test(s) that cover it. Unit tests live in `tests/FieldCheck.UnitTests`,
integration tests (real SQL Server via Testcontainers) in `tests/FieldCheck.IntegrationTests`.

## S1 — Register assets
As a site coordinator, I register an asset with an inspection interval.

| Given / When / Then | Test |
|---|---|
| Given a site exists, when I POST a valid asset, then `201` with a `Location` header | `AssetsTests.Post_ValidAsset_Returns201WithLocation` |
| Given an asset tag already exists at that site, when I POST the same tag, then `409` | `AssetsTests.Post_DuplicateTagAtSameSite_Returns409` |
| Given `InspectionIntervalDays` is 0 or > 365, then `400` ProblemDetails naming the field | `AssetsTests.Post_IntervalOutOfRange_Returns400NamingField` (0 and 366) |

## S2 — Log an inspection
As an inspector, I log an inspection against an asset.

| Given / When / Then | Test |
|---|---|
| Given an in-service asset, when I log a `Minor` inspection, then it is saved and the asset stays `InService` | `SeverityRuleTests.NonCritical_KeepsAssetInService` (unit), `InspectionsTests.Post_Minor_KeepsAssetInService` |
| Given an in-service asset, when I log a `Critical` inspection, then it is saved **and** the asset becomes `OutOfService` in the same transaction | `SeverityRuleTests.Critical_TakesAssetOutOfService` (unit), `InspectionsTests.Post_Critical_SavesAndTakesAssetOutOfService_Atomically` |
| Given `InspectedAtUtc` is in the future, then `400` | `InspectionValidationTests.FutureDate_Fails` (unit), `InspectionsTests.Post_FutureDate_Returns400` |

## S3 — Return an asset to service
As a supervisor, I return an asset to service.

| Given / When / Then | Test |
|---|---|
| Given an `OutOfService` asset and the current `RowVersion`, when I PUT status `InService`, then `200` | `AssetStatusTests.Put_WithCurrentRowVersion_Returns200` |
| Given a stale `RowVersion`, then `409` | `AssetStatusTests.Put_WithStaleRowVersion_Returns409` |

## S4 — Overdue report
As a supervisor, I see which assets are overdue for inspection.

| Given / When / Then | Test |
|---|---|
| An asset is overdue if never inspected, or its latest inspection is older than `InspectionIntervalDays` | `OverdueCalculatorTests.*` (unit), `OverdueReportTests.NeverInspectedAsset_IsOverdue`, `OverdueReportTests.Report_WithoutSiteId_SpansSites` |
| `GET /api/reports/overdue-assets?siteId=` returns overdue assets with `DaysOverdue`, most overdue first, via `dbo.usp_GetOverdueAssets` | `OverdueReportTests.Report_OrdersMostOverdueFirst_WithDaysOverdue` |
| Out-of-service assets are excluded | `OverdueReportTests.OutOfServiceAsset_IsExcluded` |

## S5 — Flexible inspection queries
As an analyst, I query inspections.

| Given / When / Then | Test |
|---|---|
| `GET /odata/Inspections?$filter=Severity eq 'Critical'&$orderby=InspectedAtUtc desc&$top=10&$count=true` works | `ODataTests.FilterOrderTopCount_Works`, `ODataTests.OrderByDesc_ReturnsNewestFirst` |
| `$top` is capped at 100; unsupported query options return `400` | `ODataTests.TopAboveCap_Returns400`, `ODataTests.UnsupportedOption_Returns400` ($expand, $search, $apply) |

## S6 — Inspection photos
As an inspector, I attach a photo.

| Given / When / Then | Test |
|---|---|
| `POST /api/inspections/{id}/photos` accepts JPEG/PNG ≤ 5 MB, stores the blob, records metadata | `PhotosTests.Post_Jpeg_StoresBlobAndMetadata` |
| Other content types or oversize files return `400`; unknown inspection returns `404` | `PhotosTests.Post_UnsupportedContentType_Returns400`, `PhotosTests.Post_Oversize_Returns400`, `PhotosTests.Post_UnknownInspection_Returns404` |
| `GET /api/photos/{id}` returns a short-lived read SAS URL; container is never public | `PhotosTests.Get_ReturnsShortLivedSasUrl` |

## Ops
| Given / When / Then | Test |
|---|---|
| `GET /health` returns `Healthy` when the database is reachable | `HealthTests.Health_ReportsHealthy_WhenDatabaseReachable` |
