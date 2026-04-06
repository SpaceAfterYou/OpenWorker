using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonUpdateOriginStatListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateOriginStat;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue Actor { get; init; }

    public void Write(BinaryWriter writer)
    {
        var stats = Enumerable.Repeat(100.0f, 60).ToArray();

        writer.WriteActor(Actor);
        writer.Write((byte)stats.Length);

        foreach (var stat in stats.Select((value, index) => new { Value = value, Index = index }))
        {
            writer.Write(stat.Value);
            writer.Write((short)stat.Index);
        }
    }
}
