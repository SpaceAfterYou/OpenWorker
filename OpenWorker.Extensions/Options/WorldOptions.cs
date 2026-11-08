using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// What the players of a channel see of each other and how fast they may move, in the <c>World</c> section.
/// </summary>
public sealed class WorldOptions : IValidatableObject
{
    public const string SectionName = "World";

    public const float DefaultVisibilityRadius = 5000f;

    public const float DefaultSpeedTolerance = 1.5f;

    public static readonly TimeSpan DefaultMoveBurst = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The side of a cell of the visibility grid, in game units (about a centimetre each). A player sees at least the
    /// players within this distance, and none further than twice its diagonal.
    /// </summary>
    public float VisibilityRadius { get; set; } = DefaultVisibilityRadius;

    /// <summary>
    /// How much faster than the run speed of its hero a player may cover ground before a move is refused: 1.5 lets it
    /// go half as fast again.
    /// </summary>
    public float SpeedTolerance { get; set; } = DefaultSpeedTolerance;

    /// <summary>
    /// How much time of running a player may save up while it stands or while its moves are late, and spend at once: the
    /// longest a client may run without saying so, and the longest jump a still player may make.
    /// </summary>
    public TimeSpan MoveBurst { get; set; } = DefaultMoveBurst;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!(float.IsFinite(VisibilityRadius) && VisibilityRadius >= 100f))
        {
            yield return new ValidationResult("World:VisibilityRadius must be a distance from 100 game units, for example 5000.", [nameof(VisibilityRadius)]);
        }

        if (!(float.IsFinite(SpeedTolerance) && SpeedTolerance >= 1f))
        {
            yield return new ValidationResult("World:SpeedTolerance must be a factor from 1, for example 1.5.", [nameof(SpeedTolerance)]);
        }

        if (MoveBurst < TimeSpan.FromSeconds(1) || MoveBurst > TimeSpan.FromMinutes(1))
        {
            yield return new ValidationResult("World:MoveBurst must be a time from 00:00:01 to 00:01:00, for example 00:00:05.", [nameof(MoveBurst)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class WorldOptionsValidator : IValidateOptions<WorldOptions>;
