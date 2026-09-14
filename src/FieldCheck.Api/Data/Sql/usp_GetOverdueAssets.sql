-- Overdue assets report.
-- An asset is overdue when it has never been inspected, or its latest inspection is older than
-- its InspectionIntervalDays. Out-of-service assets are excluded because they are not expected
-- to be inspected on the normal cycle.
--
-- Why OUTER APPLY: for each asset we need only the single latest inspection. The correlated
-- TOP (1) ... ORDER BY InspectedAtUtc DESC is satisfied directly by the index
-- IX_Inspections_AssetId_InspectedAtUtc (AssetId, InspectedAtUtc DESC): the engine seeks to the
-- asset's range and reads the first row, no sort and no scan of the asset's other inspections.
-- OUTER (not CROSS) APPLY keeps assets with no inspections, which is exactly the "never
-- inspected" case the report must include.
--
-- Why OPTION (RECOMPILE): "@SiteId IS NULL OR a.SiteId = @SiteId" is a catch-all predicate.
-- Recompiling per call lets the optimizer pick a seek on IX_Assets_SiteId_Status when a site is
-- given and a scan when it is not, instead of reusing one plan for both shapes. The report is
-- called rarely, so the compile cost is acceptable.
CREATE OR ALTER PROCEDURE dbo.usp_GetOverdueAssets
    @SiteId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NowUtc DATETIME2(0) = SYSUTCDATETIME();

    SELECT
        a.Id                          AS AssetId,
        a.SiteId,
        s.Name                        AS SiteName,
        a.Tag,
        a.Name,
        a.Category,
        a.InspectionIntervalDays,
        li.InspectedAtUtc             AS LastInspectedAtUtc,
        li.Severity                   AS LastSeverity,
        CASE WHEN li.InspectedAtUtc IS NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
                                      AS NeverInspected,
        -- Days past the due date. NULL when never inspected: there is no date to count from.
        DATEDIFF(DAY, li.InspectedAtUtc, @NowUtc) - a.InspectionIntervalDays
                                      AS DaysOverdue
    FROM dbo.Assets AS a
    INNER JOIN dbo.Sites AS s
        ON s.Id = a.SiteId
    OUTER APPLY (
        SELECT TOP (1) i.InspectedAtUtc, i.Severity
        FROM dbo.Inspections AS i
        WHERE i.AssetId = a.Id
        ORDER BY i.InspectedAtUtc DESC
    ) AS li
    WHERE a.Status = 'InService'
      AND (@SiteId IS NULL OR a.SiteId = @SiteId)
      AND (
            li.InspectedAtUtc IS NULL
         OR DATEDIFF(DAY, li.InspectedAtUtc, @NowUtc) > a.InspectionIntervalDays
      )
    ORDER BY
        NeverInspected DESC,   -- never-inspected assets first: unknown state is the most urgent
        DaysOverdue DESC,
        a.Tag
    OPTION (RECOMPILE);
END
