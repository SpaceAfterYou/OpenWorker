using OpenWorker.Domain.Enums;
using Redis.OM.Modeling;

namespace OpenWorker.Hotspot.Cache.Types;

[Document(StorageType = StorageType.Hash)]
public sealed record GateCache
{
    [RedisIdField]
    public Guid Guid { get; init; } = Guid.CreateVersion7();

    [Indexed]
    public required short Identifier { get; init; }

    [Indexed]
    public required int OnlineCount { get; init; }

    [RedisField]
    public required GateWorkload Workload { get; init; }

    [RedisField]
    public required string Host { get; init; }

    [RedisField]
    public required short Port { get; init; }

    [Indexed]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}