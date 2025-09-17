using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

[HotspotMessage(Group, Command)]
public readonly struct MazeExitRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_EXIT_MAZE_REQ;

    public int Person { get; } = reader.ReadInt32();
    public MapValue Instance { get; } = reader.ReadMapValue();
    public int Res { get; } = reader.ReadInt32();
    public int Jump { get; } = reader.ReadInt32();
    public int Portal { get; } = reader.ReadInt32();
    
    /// <summary>
    /// TODO
    /// </summary>
    public byte WhatIsIt { get; } = reader.ReadByte();

    public MessageOpcode Opcode => new(Group, Command);
}