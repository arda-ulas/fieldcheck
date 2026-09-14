-- One row per asset with its latest inspection and a running count.
-- Uses the same OUTER APPLY + TOP (1) pattern as usp_GetOverdueAssets so the
-- (AssetId, InspectedAtUtc DESC) index serves the "latest" lookup; the count is a second
-- correlated subquery over the same index (a range seek on AssetId).
CREATE OR ALTER VIEW dbo.vw_AssetInspectionSummary
AS
SELECT
    a.Id                       AS AssetId,
    a.SiteId,
    a.Tag,
    a.Name,
    a.Category,
    a.Status,
    a.InspectionIntervalDays,
    li.InspectedAtUtc          AS LastInspectedAtUtc,
    li.Severity                AS LastSeverity,
    c.InspectionCount
FROM dbo.Assets AS a
OUTER APPLY (
    SELECT TOP (1) i.InspectedAtUtc, i.Severity
    FROM dbo.Inspections AS i
    WHERE i.AssetId = a.Id
    ORDER BY i.InspectedAtUtc DESC
) AS li
CROSS APPLY (
    SELECT COUNT(*) AS InspectionCount
    FROM dbo.Inspections AS i
    WHERE i.AssetId = a.Id
) AS c;
