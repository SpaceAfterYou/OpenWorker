using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.Person.Values;

[WireMessage(WireMethods.Write)]
public readonly partial struct AbilityValue : IWireReadable<AbilityValue>
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

    public static AbilityValue Read(ref PacketReader reader)
    {
        return new AbilityValue
        {
            Health = AbilityValueEntry.Read(ref reader),
            SoulGain = AbilityValueEntry.Read(ref reader),
            SoulVapor = AbilityValueEntry.Read(ref reader),
            Stamina = AbilityValueEntry.Read(ref reader),
            SuperArmor = AbilityValueEntry.Read(ref reader),
            Speed = SpeedValueEntry.Read(ref reader)
        };
    }
}
