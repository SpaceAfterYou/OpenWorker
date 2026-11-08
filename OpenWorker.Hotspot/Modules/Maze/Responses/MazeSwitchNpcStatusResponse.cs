using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// A townsman that works as a switch changes the way it stands.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeSwitchNpcStatusResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_SWITCH_NPC_STATUS;

    public MessageOpcode Opcode => new(Group, Command);

    public int Npc { get; init; }
    public byte Status { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Npc);
        writer.Write(Status);
    }
}
