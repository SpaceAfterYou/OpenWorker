using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Login.Types;

public readonly struct GateInfo
{
    public required short Id { get; init; }
    public required string Name { get; init; }
    public required GateWorkload Workload { get; init; }
    public required string Address { get; init; }
    public required short Port { get; init; }
    public required int OnlineCount { get; init; }
}