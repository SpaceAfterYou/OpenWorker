using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// The version of the game client the servers speak to, in the <c>Game</c> section.
/// </summary>
public sealed class GameOptions
{
    public const string SectionName = "Game";

    /// <summary>
    /// Also the name of a folder of the packet dumps, so it holds digits and dots only.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Game:Version must be set, for example 1.4.2.3.")]
    [RegularExpression(@"^\d+(\.\d+){1,3}$", ErrorMessage = "Game:Version must be numbers separated by dots, for example 1.4.2.3.")]
    public string Version { get; set; } = string.Empty;
}

[OptionsValidator]
internal sealed partial class GameOptionsValidator : IValidateOptions<GameOptions>;
