using Microsoft.Extensions.Configuration;

namespace OpenWorker.Extensions;

public static class ConfigurationExtensions
{
    public static string GetGame(this IConfiguration configuration)
    {
        return configuration.GetValue<string>("Game:Version", "0.0.0.0");
    }
    
    public static Guid GetInstance(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string>(ConfigurationUtils.InstanceIdentifierKey);
        ArgumentException.ThrowIfNullOrEmpty(value);

        return Guid.Parse(value);
    }

    public static short GetGroup(this IConfiguration configuration)
    {
        return configuration.GetValue<short>(ConfigurationUtils.InstanceGroupKey);
    }
    
    public static short GetLocation(this IConfiguration configuration)
    {
        return configuration.GetValue<short>(ConfigurationUtils.InstanceLocationKey);
    }

    public static short GetGate(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Gate:Id");
    }

    public static string GetAuthHost(this IConfiguration configuration)
    {
        return configuration.GetValue("Auth:Host", "127.0.0.1");
    }

    public static short GetAuthPort(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Auth:Port", 10_000);
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
    /// Where clients reach this server, when it differs from the default of its location.
    /// </summary>
    public static (string Host, short? Port) GetPublicAddress(this IConfiguration configuration)
    {
        return (configuration.GetValue("Instance:PublicHost", "127.0.0.1"), configuration.GetValue<short?>("Instance:PublicPort"));
    }

    /// <summary>
    /// Where clients reach the maze server.
    /// </summary>
    public static (string Host, short Port) GetMazeAddress(this IConfiguration configuration)
    {
        return (configuration.GetValue("Maze:Host", "127.0.0.1"), configuration.GetValue<short>("Maze:Port", 10_101));
    }
}
