using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeInteractionEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_INTERACTION_ENABLE;

    public bool Show { get; init; }
    public bool Enable { get; init; }
    public int BoxIndex { get; init; }
    public int CallCount { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Show);
        writer.Write(Enable);
        writer.Write(BoxIndex);
        writer.Write(CallCount);
    }
}