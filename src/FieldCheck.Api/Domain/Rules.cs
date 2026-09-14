namespace FieldCheck.Api.Domain;

/// <summary>Pure business rules, kept free of EF so they can be unit tested directly.</summary>
public static class InspectionRules
{
    /// <summary>A critical finding takes the asset out of service; anything else leaves it as is.</summary>
    public static AssetStatus StatusAfter(AssetStatus current, Severity severity) =>
        severity == Severity.Critical ? AssetStatus.OutOfService : current;
}

public static class OverdueCalculator
{
    /// <summary>
    /// Mirrors dbo.usp_GetOverdueAssets: days since the last inspection minus the interval.
    /// Null when never inspected (no date to count from). Whole days, like DATEDIFF(DAY, ...).
    /// </summary>
    public static int? DaysOverdue(DateTime? lastInspectedAtUtc, int intervalDays, DateTime nowUtc) =>
        lastInspectedAtUtc is null
            ? null
            : (nowUtc.Date - lastInspectedAtUtc.Value.Date).Days - intervalDays;

    public static bool IsOverdue(DateTime? lastInspectedAtUtc, int intervalDays, DateTime nowUtc) =>
        lastInspectedAtUtc is null || DaysOverdue(lastInspectedAtUtc, intervalDays, nowUtc) > 0;
}
