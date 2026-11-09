using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// How a new character starts, in the <c>PersonCreation</c> section of the gate.
/// </summary>
public sealed class PersonCreationOptions
{
    public const string SectionName = "PersonCreation";

    /// <summary>
    /// The level a new character has: 1 like in the game, a higher one to try what needs it.
    /// </summary>
    [NumberRange(1, 100, ErrorMessage = "PersonCreation:StartLevel must be a level from 1 to 100.")]
    public byte StartLevel { get; set; } = 1;
}

[OptionsValidator]
internal sealed partial class PersonCreationOptionsValidator : IValidateOptions<PersonCreationOptions>;
