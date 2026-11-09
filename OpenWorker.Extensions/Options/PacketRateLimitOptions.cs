using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How many packets of each group a session may send, in the <c>Hotspot:RateLimits</c> section: a token bucket per group
/// and per session, which fills at <see cref="PacketRateLimit.PerSecond"/> up to <see cref="PacketRateLimit.Burst"/>.
/// A packet that finds its bucket empty is dropped.
/// </summary>
public sealed class PacketRateLimitOptions : IValidatableObject
{
    public const string SectionName = "Hotspot:RateLimits";

    /// <summary>
    /// The movement requests (group 05).
    /// </summary>
    public PacketRateLimit Movement { get; set; } = new() { PerSecond = 30, Burst = 60 };

    /// <summary>
    /// The chat (group 07). The chat keeps its own limit of lines on top of it.
    /// </summary>
    public PacketRateLimit Chat { get; set; } = new() { PerSecond = 5, Burst = 10 };

    /// <summary>
    /// Items, shops, trades, upgrades, drops and the item setups.
    /// </summary>
    public PacketRateLimit Items { get; set; } = new() { PerSecond = 20, Burst = 40 };

    /// <summary>
    /// Everything else, the burst of requests a client makes when it enters a server among them.
    /// </summary>
    public PacketRateLimit Other { get; set; } = new() { PerSecond = 60, Burst = 200 };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (name, limit) in new[] { (nameof(Movement), Movement), (nameof(Chat), Chat), (nameof(Items), Items), (nameof(Other), Other) })
        {
            if (!(float.IsFinite(limit.PerSecond) && limit.PerSecond > 0f))
            {
                yield return new ValidationResult($"Hotspot:RateLimits:{name}:PerSecond must be a number of packets above 0, for example 30.", [name]);
            }

            if (limit.Burst < 1)
            {
                yield return new ValidationResult($"Hotspot:RateLimits:{name}:Burst must be a number of packets from 1, for example 60.", [name]);
            }
        }
    }
}

/// <summary>
/// One bucket: <see cref="PerSecond"/> packets a second on average, <see cref="Burst"/> at once.
/// </summary>
public sealed class PacketRateLimit
{
    public float PerSecond { get; set; }

    public int Burst { get; set; }
}

[OptionsValidator]
internal sealed partial class PacketRateLimitOptionsValidator : IValidateOptions<PacketRateLimitOptions>;
