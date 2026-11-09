using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct WorldWarpOutRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpOutReq;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
