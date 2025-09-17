using OpenWorker.Hotspot.Messages.Response.Person.Components;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Skill.Components;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct AbilityValue
{
    public AbilityValueEntry Health { get; init; }
    public AbilityValueEntry SoulGain { get; init; }
    public AbilityValueEntry SoulVapor { get; init; }
    public AbilityValueEntry Stamina { get; init; }
    public AbilityValueEntry SuperArmor { get; init; }

    public SpeedValueEntry Speed { get; init; }

    public AbilityValue(AbilityComponent component)
    {
        Health = new AbilityValueEntry(component.Health);
        SoulGain = new AbilityValueEntry(component.SoulGain);
        SoulVapor = new AbilityValueEntry(component.SoulVapor);
        Stamina = new AbilityValueEntry(component.Stamina);
        SuperArmor = new AbilityValueEntry(component.SuperArmor);
        Speed = new SpeedValueEntry(component.Speed);
    }

    public AbilityValue(BinaryReader reader)
    {
        Health = new AbilityValueEntry(reader);
        SoulGain = new AbilityValueEntry(reader);
        SoulVapor = new AbilityValueEntry(reader);
        Stamina = new AbilityValueEntry(reader);
        SuperArmor = new AbilityValueEntry(reader);
        Speed = new SpeedValueEntry(reader);
    }
}