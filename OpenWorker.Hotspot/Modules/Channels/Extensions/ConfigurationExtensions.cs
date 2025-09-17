using Microsoft.Extensions.Configuration;

namespace OpenWorker.Hotspot.Modules.Channels.Extensions;

public static class ConfigurationExtensions
{
    public static int GetChannelRangeStart(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Channel:From");
    }

    public static int GetChannelRangeEnd(this IConfiguration configuration)
    {
        return configuration.GetValue<short>("Channel:To");
    }
}