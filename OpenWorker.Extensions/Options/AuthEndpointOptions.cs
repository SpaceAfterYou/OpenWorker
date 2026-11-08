using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where clients reach the auth server, in the <c>Auth</c> section. A server that sends a player
/// back to the server list answers with it.
/// </summary>
public sealed class AuthEndpointOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Auth:Host must be set to the address clients reach the auth server at.")]
    public string Host { get; set; } = string.Empty;

    [NumberRange(1, PortRange.AddressMax, ErrorMessage = "Auth:Port must be a TCP port from 1 to 32767.")]
    public int Port { get; set; }
}

[OptionsValidator]
internal sealed partial class AuthEndpointOptionsValidator : IValidateOptions<AuthEndpointOptions>;
