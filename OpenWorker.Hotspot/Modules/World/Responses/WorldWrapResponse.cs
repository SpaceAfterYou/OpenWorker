using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Enums;
using OpenWorker.Hotspot.Modules.World.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command)]
public readonly struct WorldWarpResponse(ActorComponent actor, Vector3 position, float rotation, WorldWrapResult result) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpRes;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActor(actor);
        writer.Write(result);
        writer.Write(position);
        writer.Write(rotation);
    }
}