using System.ComponentModel.DataAnnotations;

namespace OpenWorker.Extensions;

/// <summary>
/// A <see cref="Guid"/> that is set.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}
