using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// What the players of a channel see of each other, how far one move may reach and how fast the players are expected to
/// move, in the <c>World</c> section.
/// </summary>
public sealed class WorldOptions : IValidatableObject
{
    public const string SectionName = "World";

    public const float DefaultVisibilityRadius = 5000f;

    public const float DefaultSpeedTolerance = 1.5f;

    public static readonly TimeSpan DefaultMoveBurst = TimeSpan.FromSeconds(5);

    public const float DefaultMaxMoveStep = 3000f;

    public static readonly TimeSpan DefaultPlaceGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The side of a cell of the visibility grid, in game units (about a centimetre each). A player sees at least the
    /// players within this distance, and none further than twice its diagonal.
    /// </summary>
    public float VisibilityRadius { get; set; } = DefaultVisibilityRadius;

    /// <summary>
    /// How much faster than the run speed of its hero a player may cover ground before a move is counted as too fast: 1.5
    /// lets it go half as fast again. Only counted, as the original server does: no move is refused for it.
    /// </summary>
    public float SpeedTolerance { get; set; } = DefaultSpeedTolerance;

    /// <summary>
    /// How much time of running a player may save up while it stands or while its moves are late, and spend at once, for
    /// the count of the moves that are too fast.
    /// </summary>
    public TimeSpan MoveBurst { get; set; } = DefaultMoveBurst;

    /// <summary>
    /// The furthest one move may take a player from the place the server keeps, in game units added over both axes. A move
    /// further than that is refused, as the original server refuses it; a dash, an evade or a jump is far below it.
    /// </summary>
    public float MaxMoveStep { get; set; } = DefaultMaxMoveStep;

    /// <summary>
    /// How long after the server told a player a place (the entry of a map, a warp) the moves the client sent before it
    /// took the place are dropped without a fault: they are from the place it left.
    /// </summary>
    public TimeSpan PlaceGrace { get; set; } = DefaultPlaceGrace;

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

        if (!(float.IsFinite(MaxMoveStep) && MaxMoveStep >= 500f))
        {
            yield return new ValidationResult("World:MaxMoveStep must be a distance from 500 game units, for example 3000.", [nameof(MaxMoveStep)]);
        }

        if (PlaceGrace < TimeSpan.Zero || PlaceGrace > TimeSpan.FromMinutes(1))
        {
            yield return new ValidationResult("World:PlaceGrace must be a time from 00:00:00 to 00:01:00, for example 00:00:05.", [nameof(PlaceGrace)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class WorldOptionsValidator : IValidateOptions<WorldOptions>;
