using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldWarpEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpEnableRes;

    public MessageOpcode Opcode => new(Group, Command);

    public int Box { get; init; }
    public bool Open { get; init; }
    public bool IsMazeComplete { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Box);
        writer.Write(Open);
        writer.Write(IsMazeComplete);
    }
}
