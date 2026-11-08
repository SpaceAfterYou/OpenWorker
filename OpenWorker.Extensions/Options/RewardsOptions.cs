using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How generous the game servers are, in the <c>Rewards</c> section: 1 gives what the tables say.
/// </summary>
public sealed class RewardsOptions : IValidatableObject
{
    public const string SectionName = "Rewards";

    /// <summary>
    /// What the experience of a kill is multiplied by.
    /// </summary>
    public float ExpRate { get; set; } = 1f;

    /// <summary>
    /// What the chances of a monster to drop something are multiplied by.
    /// </summary>
    public float DropRate { get; set; } = 1f;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!(float.IsFinite(ExpRate) && ExpRate >= 0f))
        {
            yield return new ValidationResult("Rewards:ExpRate must be a rate from 0, for example 1 or 2.5.", [nameof(ExpRate)]);
        }

        if (!(float.IsFinite(DropRate) && DropRate >= 0f))
        {
            yield return new ValidationResult("Rewards:DropRate must be a rate from 0, for example 1 or 2.5.", [nameof(DropRate)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class RewardsOptionsValidator : IValidateOptions<RewardsOptions>;
