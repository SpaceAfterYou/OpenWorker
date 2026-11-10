using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

/// <summary>
/// A notice of the maze to everyone in it. The client knows a few by their type and shows the text of each; the one the
/// party sends when its monsters change their strength is 80.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MazeNoticeResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_NOTICE;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    /// <summary>
    /// Which notice, which the client has a text for.
    /// </summary>
    public int Type { get; init; }

    /// <summary>
    /// A number the text of some notices carries.
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    /// How long the notice stays, for those that stay.
    /// </summary>
    public float Time { get; init; }

#endregion Message: Body
}
