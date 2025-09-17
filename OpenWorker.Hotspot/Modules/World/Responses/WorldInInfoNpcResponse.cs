using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command)]
public readonly struct WorldInInfoNpcResponse(Entity[] list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.InInfoNpc;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        Debug.Assert(list.Length < byte.MaxValue);
        writer.Write((byte)list.Length);

        foreach (var entity in list)
        {
            var actor = entity.Get<ActorComponent>();
            var npc = entity.Get<CreatureComponent___Old>();

            writer.WriteActor(actor);
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