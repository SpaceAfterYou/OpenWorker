using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The answer of the character selection that sends the character into a battle zone. The client reads it like the
/// ordinary answer: on a zero <see cref="Result"/> it connects to the address of the zone, otherwise it shows the message
/// of the code. Of the values between the zone and the result it keeps none, so their meaning is not known.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PersonEnterBattleZoneResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterBattleZone;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ZoneValue Zone { get; init; }
    public short Unknown96 { get; init; }
    public int Unknown100 { get; init; }
    public short Unknown112 { get; init; }
    public long Unknown104 { get; init; }

    /// <summary>
    /// Zero lets the client in; anything else is the code of the message it shows instead.
    /// </summary>
    public int Result { get; init; }

    [WireArray(WireLength.Byte)]
    public required int[] Values { get; init; }
}
