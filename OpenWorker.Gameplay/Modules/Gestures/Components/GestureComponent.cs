using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Modules.Gestures.Types;

namespace OpenWorker.Gameplay.Modules.Gestures.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct GestureComponent()
{
    public int Active { get; init; } = -1;
    public required int[] Collection { get; init; }
}