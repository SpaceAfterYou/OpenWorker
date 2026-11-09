using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// The master of the party asks the members to enter a maze together. The client opens the question with the maze for a
/// member, who answers <c>MazeEnterPartyAnswerRequest</c>, and a window to wait in for the master, who can call it off
/// with the same request. It comes in the group of the party, although it is about the maze.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeEnterPartyAskResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Unknown5;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// The maze, as the maze table names it.
    /// </summary>
    public int Maze { get; init; }

    /// <summary>
    /// Always false: the question is still open.
    /// </summary>
    public bool Accepted { get; init; }
}
