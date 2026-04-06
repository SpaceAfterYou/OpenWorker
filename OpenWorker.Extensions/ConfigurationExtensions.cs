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
}