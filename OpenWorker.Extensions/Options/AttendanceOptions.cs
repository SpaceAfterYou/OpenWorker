using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// When the day of the attendance rewards starts, in the <c>Attendance</c> section. Without it the rules decide.
/// </summary>
public sealed class AttendanceOptions : IValidatableObject
{
    public const string SectionName = "Attendance";

    /// <summary>
    /// The hour of the server time the day starts at: 9 is nine o'clock, 9.5 half past nine.
    /// </summary>
    public double? ResetHour { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResetHour is { } hour && !(hour >= 0 && hour < 24))
        {
            yield return new ValidationResult("Attendance:ResetHour must be an hour from 0 up to 24, for example 9.", [nameof(ResetHour)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class AttendanceOptionsValidator : IValidateOptions<AttendanceOptions>;
