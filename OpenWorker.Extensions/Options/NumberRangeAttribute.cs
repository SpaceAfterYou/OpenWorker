using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace OpenWorker.Extensions;

/// <summary>
/// A whole number from <paramref name="minimum"/> to <paramref name="maximum"/>, both included; a number that is not set
/// (<see langword="null"/>) passes. It stands in for <see cref="RangeAttribute"/>, whose copy in the generated validators
/// ignores <see cref="ValidationAttribute.ErrorMessage"/>, and the message is what tells which setting is wrong.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class NumberRangeAttribute(long minimum, long maximum) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        var number = Convert.ToInt64(value, CultureInfo.InvariantCulture);

        return number >= minimum && number <= maximum;
    }
}
