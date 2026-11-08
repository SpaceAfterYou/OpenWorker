using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Where clients reach a maze server when no maze server has registered itself, in the <c>Maze</c> section.
/// A maze server without <c>Instance:PublicPort</c> registers this port.
/// </summary>
public sealed class MazeEndpointOptions
{
    public const string SectionName = "Maze";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Maze:Host must be set to the address clients reach a maze server at.")]
    public string Host { get; set; } = string.Empty;

    [NumberRange(1, PortRange.AddressMax, ErrorMessage = "Maze:Port must be a TCP port from 1 to 32767.")]
    public int Port { get; set; }
}

[OptionsValidator]
internal sealed partial class MazeEndpointOptionsValidator : IValidateOptions<MazeEndpointOptions>;
