using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// What the players of a channel see of each other, in the <c>World</c> section.
/// </summary>
public sealed class WorldOptions : IValidatableObject
{
    public const string SectionName = "World";

    public const float DefaultVisibilityRadius = 5000f;

    /// <summary>
    /// The side of a cell of the visibility grid, in game units (about a centimetre each). A player sees at least the
    /// players within this distance, and none further than twice its diagonal.
    /// </summary>
    public float VisibilityRadius { get; set; } = DefaultVisibilityRadius;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!(float.IsFinite(VisibilityRadius) && VisibilityRadius >= 100f))
        {
            yield return new ValidationResult("World:VisibilityRadius must be a distance from 100 game units, for example 5000.", [nameof(VisibilityRadius)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class WorldOptionsValidator : IValidateOptions<WorldOptions>;
