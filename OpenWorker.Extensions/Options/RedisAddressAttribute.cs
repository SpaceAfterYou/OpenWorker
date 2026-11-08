using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OpenWorker.Extensions;

/// <summary>
/// A <c>redis://</c> or <c>rediss://</c> address with a host.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class RedisAddressAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is string text && TryParse(text, out _);
    }

    public static bool TryParse(string? text, [NotNullWhen(true)] out Uri? address)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out address)
            && address.Scheme is "redis" or "rediss"
            && address.Host.Length > 0;
    }
}
