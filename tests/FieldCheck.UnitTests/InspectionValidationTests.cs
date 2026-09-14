using FieldCheck.Api.Validation;

namespace FieldCheck.UnitTests;

public class InspectionValidationTests
{
    private readonly NotInFutureAttribute _attr = new();

    [Fact]
    public void FutureDate_Fails() => Assert.False(_attr.IsValid(DateTime.UtcNow.AddHours(1)));

    [Fact]
    public void PastDate_Passes() => Assert.True(_attr.IsValid(DateTime.UtcNow.AddMinutes(-5)));

    [Fact]
    public void WithinClockSkewTolerance_Passes() =>
        Assert.True(_attr.IsValid(DateTime.UtcNow + NotInFutureAttribute.Tolerance / 2));

    [Fact]
    public void Null_IsLeftToRequired() => Assert.True(_attr.IsValid(null));
}
