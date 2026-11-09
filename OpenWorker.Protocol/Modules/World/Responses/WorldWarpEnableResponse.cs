using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct WorldWarpEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpEnableRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Box { get; init; }
    public bool Open { get; init; }
    public bool IsMazeComplete { get; init; }
}
