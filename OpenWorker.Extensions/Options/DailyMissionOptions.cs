using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// When the special daily missions are drawn again, in the <c>DailyMission</c> section. Without it the rules decide.
/// </summary>
public sealed class DailyMissionOptions : IValidatableObject
{
    public const string SectionName = "DailyMission";

    /// <summary>
    /// The hour of the server time the day starts at: 11 is eleven o'clock, 11.5 half past eleven.
    /// </summary>
    public double? ResetHour { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResetHour is { } hour && !(hour >= 0 && hour < 24))
        {
            yield return new ValidationResult("DailyMission:ResetHour must be an hour from 0 up to 24, for example 11.", [nameof(ResetHour)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class DailyMissionOptionsValidator : IValidateOptions<DailyMissionOptions>;
