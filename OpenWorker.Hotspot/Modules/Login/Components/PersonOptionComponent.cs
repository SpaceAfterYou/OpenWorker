using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Login.Components;

[EntityComponent(EntityComponentService.District)]
public readonly struct PersonOptionComponent
{
    public byte[] Collection { get; init; }
}