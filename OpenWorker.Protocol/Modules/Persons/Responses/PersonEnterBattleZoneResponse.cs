using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonEnterBattleZoneResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.EnterBattleZone;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public required ZoneValue Zone { get; init; }
    public short Unknown96 { get; init; }
    public int Unknown100 { get; init; }
    public short Unknown112 { get; init; }
    public long Unknown104 { get; init; }
    public int Unknown132 { get; init; }
    public required int[] Values { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Zone);
        writer.Write(Unknown96);
        writer.Write(Unknown100);
        writer.Write(Unknown112);
        writer.Write(Unknown104);
        writer.Write(Unknown132);
        writer.Write(checked((byte)Values.Length));

        foreach (var value in Values)
        {
            writer.Write(value);
        }
    }
}
