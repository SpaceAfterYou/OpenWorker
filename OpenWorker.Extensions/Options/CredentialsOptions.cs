using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// The secrets the relay server logs in with, in the <c>Credentials</c> section. No settings file carries them:
/// they come from the environment (<c>Credentials__DiscordToken</c>) or the user secrets.
/// </summary>
public sealed class CredentialsOptions
{
    public const string SectionName = "Credentials";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Credentials:DiscordToken must be set to the token of the Discord bot.")]
    public string DiscordToken { get; set; } = string.Empty;
}

[OptionsValidator]
internal sealed partial class CredentialsOptionsValidator : IValidateOptions<CredentialsOptions>;
