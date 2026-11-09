using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MazeInteractionRunResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_INTERACTION_RUN;

    public MessageOpcode Opcode => new(Group, Command);

    public byte Result { get; init; }
    public MazeInteractionValue Interaction { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Result);
        writer.Write(Interaction.Show);
        writer.Write(Interaction.Enable);
        writer.Write(Interaction.Box);
        writer.Write(Interaction.CallCount);
    }
}
