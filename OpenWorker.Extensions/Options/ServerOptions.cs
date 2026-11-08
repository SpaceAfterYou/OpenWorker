using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where the server listens, in the <c>Server</c> section. This is the port inside the container:
/// <c>compose.yaml</c> maps it to a port of the host.
/// </summary>
public sealed class ServerOptions
{
    public const string SectionName = "Server";

    [NumberRange(1, PortRange.ListenMax, ErrorMessage = "Server:Port must be a TCP port from 1 to 65535.")]
    public int Port { get; set; }
}

[OptionsValidator]
internal sealed partial class ServerOptionsValidator : IValidateOptions<ServerOptions>;
