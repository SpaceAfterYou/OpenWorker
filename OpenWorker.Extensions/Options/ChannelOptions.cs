using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// The numbers of the channels the server runs, from <c>From</c> to <c>To</c> inclusive, in the <c>Channel</c> section.
/// </summary>
public sealed class ChannelOptions : IValidatableObject
{
    public const string SectionName = "Channel";

    [NumberRange(0, short.MaxValue, ErrorMessage = "Channel:From must be a channel number from 0 to 32767.")]
    public short From { get; set; }

    [NumberRange(0, short.MaxValue, ErrorMessage = "Channel:To must be a channel number from 0 to 32767.")]
    public short To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (To < From)
        {
            yield return new ValidationResult("Channel:To must not be less than Channel:From.", [nameof(To)]);
        }
    }
}

[OptionsValidator]
internal sealed partial class ChannelOptionsValidator : IValidateOptions<ChannelOptions>;
