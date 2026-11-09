using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Who may use the test commands of the chat, in the <c>Commands</c> section of the district and the maze. A command names
/// the access level of the account it needs; with <see cref="OpenToEveryone"/> every account counts as a game master, as on
/// a test server of the original.
/// </summary>
public sealed class CommandOptions
{
    public const string SectionName = "Commands";

    /// <summary>
    /// Every account may use the commands of a game master: a test stand, never a server with players.
    /// </summary>
    public bool OpenToEveryone { get; set; }
}

/// <summary>
/// A flag has no wrong value; the validator is what <c>AddValidated</c> registers the section with.
/// </summary>
internal sealed class CommandOptionsValidator : IValidateOptions<CommandOptions>
{
    public ValidateOptionsResult Validate(string? name, CommandOptions options) => ValidateOptionsResult.Success;
}
