using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.DistrictServer.Server.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct KeepAliveComponent()
{
    public TimeSpan LastTickCount { get; init; } = new(DateTime.UtcNow.Ticks);
    public byte PenaltyCount { get; init; }
}