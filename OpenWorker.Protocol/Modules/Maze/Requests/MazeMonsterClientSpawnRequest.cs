using System.Numerics;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Maze.Requests;

/// <summary>
/// A monster controlled by the client has summoned another one.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MazeMonsterClientSpawnRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Maze;
    private const MazeOpcode Command = MazeOpcode.eSUB_CMD_MONSTER_CLIENT_SPAWN;

    public Vector3 Position { get; }
    public float Rotation { get; }

    /// <summary>
    /// The row of the table of monsters.
    /// </summary>
    public int Monster { get; }

    public int Owner { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
