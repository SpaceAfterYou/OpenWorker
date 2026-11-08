using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

/// <summary>
/// A monster controlled by the client has summoned another one.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct MazeMonsterClientSpawnRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MONSTER_CLIENT_SPAWN;

    public Vector3 Position { get; } = reader.ReadVector3();
    public float Rotation { get; } = reader.ReadSingle();

    /// <summary>
    /// The row of the table of monsters.
    /// </summary>
    public int Monster { get; } = reader.ReadInt32();

    public int Owner { get; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);
}
