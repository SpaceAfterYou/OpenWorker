using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PersonUpdateOriginStatListResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.UpdateOriginStat;

    public MessageOpcode Opcode => new(Group, Command);

    public required ActorValue Actor { get; init; }

    /// <summary>
    /// The stats the status window shows, the ones left out are zero.
    /// </summary>
    public required StatKeyValuePair[] Stats { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write((byte)Stats.Length);

        foreach (var stat in Stats)
        {
            writer.Write(stat.statValue);
            writer.Write((short)stat.byIndex);
        }
    }
}
