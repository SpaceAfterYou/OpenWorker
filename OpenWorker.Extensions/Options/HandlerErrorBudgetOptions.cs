using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How many packets of a session may fail to parse or to handle before the server closes the connection, in the
/// <c>Hotspot:ErrorBudget</c> section: <see cref="MaxErrors"/> errors within <see cref="Window"/> close it.
/// </summary>
public sealed class HandlerErrorBudgetOptions : IValidatableObject
{
    public const string SectionName = "Hotspot:ErrorBudget";

    [NumberRange(1, int.MaxValue, ErrorMessage = "Hotspot:ErrorBudget:MaxErrors must be a number of errors from 1, for example 10.")]
    public int MaxErrors { get; set; }

    /// <summary>
    /// The window opens with the first error. Written as a time span: <c>00:01:00</c> is one minute.
    /// </summary>
    public TimeSpan Window { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Window <= TimeSpan.Zero)
        {
            yield return new ValidationResult("Hotspot:ErrorBudget:Window must be a time longer than zero, for example 00:01:00.", [nameof(Window)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class HandlerErrorBudgetOptionsValidator : IValidateOptions<HandlerErrorBudgetOptions>;
