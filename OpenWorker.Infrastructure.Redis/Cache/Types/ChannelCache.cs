using OpenWorker.Hotspot.Modules.Channels.Enums;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed class ChannelCache
{
    [RedisIdField]
    public Guid Guid { get; init; } = Guid.CreateVersion7();

    [Indexed]
    public required Guid Owner { get; init; }

    [Indexed]
    public required short Gate { get; init; }

    [Indexed]
    public required short Location { get; init; }

    [Indexed]
    public required short Identifier { get; init; }

    [RedisField]
    public required short OnlineCount { get; set; }

    [Indexed]
    public ChannelWorkload Workload => ChannelUtils.GetWorkload(OnlineCount);
}