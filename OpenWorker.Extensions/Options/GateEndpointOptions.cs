using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where clients reach the gate server, in the <c>Gate</c> section. Only the gate server needs it.
/// </summary>
public sealed class GateEndpointOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Gate:Host must be set to the address clients reach the gate server at.")]
    public string Host { get; set; } = string.Empty;

    [NumberRange(1, PortRange.AddressMax, ErrorMessage = "Gate:Port must be a TCP port from 1 to 32767.")]
    public int Port { get; set; }
}

[OptionsValidator]
internal sealed partial class GateEndpointOptionsValidator : IValidateOptions<GateEndpointOptions>;
