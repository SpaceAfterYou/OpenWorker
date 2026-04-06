using Arch.Core;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;

namespace OpenWorker.Gameplay.Modules.Login.Components;

[EntityComponent(EntityComponentService.Gate)]
public readonly record struct GatePersonComponent(Entity[] SlotList, int LastIndex = -1)
{
    public GatePersonComponent(GatePersonComponent component) : this(component.SlotList, component.LastIndex)
    {
        
    }
    
    public bool HasLast => LastIndex >= 0;
    
    public Entity Last => SlotList[LastIndex];
}
