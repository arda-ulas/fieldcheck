using System.ComponentModel.DataAnnotations;

namespace FieldCheck.Api.Validation;

/// <summary>Rejects UTC timestamps later than now (with a small clock-skew allowance).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    public static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(1);

    public NotInFutureAttribute() : base("{0} must not be in the future.") { }

    public override bool IsValid(object? value) =>
        value is not DateTime dt || dt <= DateTime.UtcNow + Tolerance;
}
