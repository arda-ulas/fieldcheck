using FieldCheck.Api.Domain;

namespace FieldCheck.UnitTests;

public class OverdueCalculatorTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NeverInspected_IsOverdue_WithNullDays()
    {
        Assert.True(OverdueCalculator.IsOverdue(null, 30, Now));
        Assert.Null(OverdueCalculator.DaysOverdue(null, 30, Now));
    }

    [Fact]
    public void WithinInterval_IsNotOverdue()
    {
        var last = Now.AddDays(-10);
        Assert.False(OverdueCalculator.IsOverdue(last, 30, Now));
        Assert.Equal(-20, OverdueCalculator.DaysOverdue(last, 30, Now));
    }

    [Fact]
    public void ExactlyAtInterval_IsNotOverdue() =>
        Assert.False(OverdueCalculator.IsOverdue(Now.AddDays(-30), 30, Now));

    [Fact]
    public void PastInterval_ReportsDaysOverdue()
    {
        var last = Now.AddDays(-45);
        Assert.True(OverdueCalculator.IsOverdue(last, 30, Now));
        Assert.Equal(15, OverdueCalculator.DaysOverdue(last, 30, Now));
    }
}
