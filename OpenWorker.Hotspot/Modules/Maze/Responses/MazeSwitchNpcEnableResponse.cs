using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// A townsman that works as a switch can be clicked, or can not.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeSwitchNpcEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_SWITCH_NPC_ENABLE;

    public MessageOpcode Opcode => new(Group, Command);

    public int Npc { get; init; }
    public bool Enable { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
        writer.Write(Enable);
    }
}
