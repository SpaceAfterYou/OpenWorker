using Microsoft.Extensions.Configuration;

namespace OpenWorker.Extensions;

/// <summary>
/// Reads a setting straight from the configuration. New code asks for the options of the setting instead
/// (<see cref="InstanceOptions"/>, <see cref="GateOptions"/>, <see cref="GateEndpointOptions"/>, <see cref="MazeEndpointOptions"/>):
/// they are checked when the host starts and have no defaults in code. What is left here is read by the classes
/// that have not moved to the options yet.
/// </summary>
public static class ConfigurationExtensions
{
    public static Guid GetInstance(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string>(ConfigurationUtils.InstanceIdentifierKey);
        ArgumentException.ThrowIfNullOrEmpty(value);

        return Guid.Parse(value);
    }

    public static short GetLocation(this IConfiguration configuration)
    {
        return configuration.GetValue<short>(ConfigurationUtils.InstanceLocationKey);
    }

    public static short GetGate(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Gate:Id");
    }

    public static string GetGateHost(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string>("Gate:Host");
        ArgumentException.ThrowIfNullOrEmpty(value);

        return value;
    }

    public static short GetGatePort(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Gate:Port");
    }

    /// <summary>
    /// Where clients reach the maze server when none has registered itself: the <c>Maze</c> section, which the
    /// <c>appsettings.json</c> of the server carries.
    /// </summary>
    public static (string Host, short Port) GetMazeAddress(this IConfiguration configuration)
    {
        var options = configuration.GetSection(MazeEndpointOptions.SectionName).Get<MazeEndpointOptions>()
            ?? throw new InvalidOperationException("Maze:Host and Maze:Port must be set to the address clients reach a maze server at.");

        return (options.Host, (short)options.Port);
    }
}
