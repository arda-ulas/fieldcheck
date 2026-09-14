using FieldCheck.Api.Domain;

namespace FieldCheck.UnitTests;

public class SeverityRuleTests
{
    [Theory]
    [InlineData(Severity.None)]
    [InlineData(Severity.Minor)]
    [InlineData(Severity.Major)]
    public void NonCritical_KeepsAssetInService(Severity severity) =>
        Assert.Equal(AssetStatus.InService, InspectionRules.StatusAfter(AssetStatus.InService, severity));

    [Fact]
    public void Critical_TakesAssetOutOfService() =>
        Assert.Equal(AssetStatus.OutOfService, InspectionRules.StatusAfter(AssetStatus.InService, Severity.Critical));

    [Fact]
    public void NonCritical_DoesNotReturnAssetToService() =>
        Assert.Equal(AssetStatus.OutOfService, InspectionRules.StatusAfter(AssetStatus.OutOfService, Severity.None));
}
