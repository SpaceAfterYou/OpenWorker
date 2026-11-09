using OpenWorker.Hotspot.Modules.Channels.Enums;

namespace OpenWorker.Hotspot.Modules.Channels.Types;

public readonly record struct ChannelValue(short Id, ChannelWorkload Workload);