using OpenWorker.Hotspot.Modules.Channels;
using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Hotspot.Cache;

public static class ChannelUtils
{
    public static ChannelWorkload GetWorkload(IComparable count)
    {
        return count switch
        {
            > ChannelModuleDefinitions.Workload.Full => ChannelWorkload.Full,
            > ChannelModuleDefinitions.Workload.High => ChannelWorkload.High,
            > ChannelModuleDefinitions.Workload.Normal => ChannelWorkload.Normal,

            _ => ChannelWorkload.Low
        };
    }
}