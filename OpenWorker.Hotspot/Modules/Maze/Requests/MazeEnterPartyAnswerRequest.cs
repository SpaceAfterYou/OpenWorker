using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

/// <summary>
/// The answer of a player to the call of the party to enter a maze together.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeEnterPartyAnswerRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_ENTER_PARTY_RES;

    public int Maze { get; } = reader.ReadInt32();
    public bool Agree { get; } = reader.ReadBoolean();

    public MessageOpcode Opcode => new(Group, Command);
}
