using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// How much one actor has hit a monster, as the client shows it in its debug window.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MovementTraceAggro(int Actor, float Value);

/// <summary>
/// What a monster is doing and thinking. The server sends it only to a client that asked to follow the monster,
/// the client keeps it for its debug window.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementTraceBtResponse : IResponseHotspotMessage
{
    public const int FuzzyCount = 4;
    public const int ActionCount = 7;

    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.TraceBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public int Animation { get; init; }
    public byte DefenseType { get; init; }
    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<Vector3> Projectiles { get; init; }
    public byte AiState { get; init; }

    /// <summary>
    /// The four fuzzy values of the monster, fewer are padded with zeros.
    /// </summary>
    [WireArray(Count = FuzzyCount)]
    public IReadOnlyList<float> Fuzzy { get; init; }

    /// <summary>
    /// The weights of the seven kinds of action it chooses from, fewer are padded with zeros.
    /// </summary>
    [WireArray(Count = ActionCount)]
    public IReadOnlyList<int> Actions { get; init; }

    public float MoveX { get; init; }
    public float MoveY { get; init; }
    public ushort WayPoint { get; init; }
    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<MovementTraceAggro> Aggro { get; init; }
}
