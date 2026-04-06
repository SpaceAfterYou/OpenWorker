using System.Diagnostics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Types;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldInInfoNpcResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.InInfoNpc;

    public MessageOpcode Opcode => new(Group, Command);

    public required IReadOnlyList<WorldNpcListEntry> List { get; init; }

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(List.Count < byte.MaxValue);
        writer.Write((byte)List.Count);

        foreach (var npc in List)
        {
            writer.WriteActor(npc.Actor);
            writer.Write(npc.Position);
            writer.Write(npc.Rotation);
            writer.Write(npc.Health);
            writer.Write(npc.Waypoint);
            writer.Write(npc.Sector);
            writer.Write(npc.Level);
            writer.Write(npc.Prototype);
        }
    }
}
