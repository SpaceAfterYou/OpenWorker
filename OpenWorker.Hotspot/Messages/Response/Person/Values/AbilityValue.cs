using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

public readonly struct AbilityValue
{
    public AbilityValueEntry Health { get; init; }
    public AbilityValueEntry SoulGain { get; init; }
    public AbilityValueEntry SoulVapor { get; init; }
    public AbilityValueEntry Stamina { get; init; }
    public AbilityValueEntry SuperArmor { get; init; }

    public SpeedValueEntry Speed { get; init; }

    public AbilityValue(
        AbilityValueEntry health,
        AbilityValueEntry soulGain,
        AbilityValueEntry soulVapor,
        AbilityValueEntry stamina,
        AbilityValueEntry superArmor,
        SpeedValueEntry speed)
    {
        Health = health;
        SoulGain = soulGain;
        SoulVapor = soulVapor;
        Stamina = stamina;
        SuperArmor = superArmor;
        Speed = speed;
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
