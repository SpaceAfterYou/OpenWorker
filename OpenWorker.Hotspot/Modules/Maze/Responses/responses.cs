using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Messages.Response.Person;

namespace OpenWorker.Hotspot.Modules.Maze.Responses;

[HotspotMessage(Group, Command)]
public readonly struct MazeLuaFunctionResponse(int box) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_LUA_FUNCTION_RES;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(box);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct MazeInteractionEnableResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_INTERACTION_ENABLE;

    public bool Show { get; init; }
    public bool Enable { get; init; }
    public int BoxIndex { get; init; }
    public int CallCount { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Show);
        writer.Write(Enable);
        writer.Write(BoxIndex);
        writer.Write(CallCount);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct MazeEnterResponse(WorldValue world) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MAZE_ENTER_RES;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(world);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct MazeWarpInSectorResponse(Vector3 position, int sector) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_WARP_IN_SECTOR;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(position);
        writer.Write(sector);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct MazeClearSectorResponse(int sector, bool status) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_SECTOR_CLEAR;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(sector);
        writer.Write(status);
    }
}