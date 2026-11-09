using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeExitRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_EXIT_MAZE_REQ;

    public int Person { get; }
    public MapValue Instance { get; }
    public int Res { get; }
    public int Jump { get; }
    public int Portal { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public byte WhatIsIt { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
