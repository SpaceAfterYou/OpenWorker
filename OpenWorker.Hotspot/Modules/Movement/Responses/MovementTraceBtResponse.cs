using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// How much one actor has hit a monster, as the client shows it in its debug window.
/// </summary>
public readonly record struct MovementTraceAggro(int Actor, float Value);

/// <summary>
/// What a monster is doing and thinking. The server sends it only to a client that asked to follow the monster,
/// the client keeps it for its debug window.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementTraceBtResponse : IResponseHotspotMessage
{
    public const int FuzzyCount = 4;
    public const int ActionCount = 7;

    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.TraceBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public int Animation { get; init; }
    public byte DefenseType { get; init; }
    public IReadOnlyCollection<Vector3> Projectiles { get; init; }
    public byte AiState { get; init; }

    /// <summary>
    /// The four fuzzy values of the monster, fewer are padded with zeros.
    /// </summary>
    public IReadOnlyList<float> Fuzzy { get; init; }

    /// <summary>
    /// The weights of the seven kinds of action it chooses from, fewer are padded with zeros.
    /// </summary>
    public IReadOnlyList<int> Actions { get; init; }

    public float MoveX { get; init; }
    public float MoveY { get; init; }
    public ushort WayPoint { get; init; }
    public IReadOnlyCollection<MovementTraceAggro> Aggro { get; init; }

    public void Write(BinaryWriter writer)
    {
        var projectiles = Projectiles ?? [];
        var aggro = Aggro ?? [];

        writer.WriteActor(Actor);
        writer.Write(Position);
        writer.Write(Yaw);
        writer.Write(Pitch);
        writer.Write(Animation);
        writer.Write(DefenseType);
        writer.Write((byte)projectiles.Count);

        foreach (var projectile in projectiles)
        {
            writer.Write(projectile);
        }

        writer.Write(AiState);

        for (var i = 0; i < FuzzyCount; i++)
        {
            writer.Write(Fuzzy is not null && i < Fuzzy.Count ? Fuzzy[i] : 0f);
        }

        for (var i = 0; i < ActionCount; i++)
        {
            writer.Write(Actions is not null && i < Actions.Count ? Actions[i] : 0);
        }

        writer.Write(MoveX);
        writer.Write(MoveY);
        writer.Write(WayPoint);
        writer.Write((byte)aggro.Count);

        foreach (var entry in aggro)
        {
            writer.Write(entry.Actor);
            writer.Write(entry.Value);
        }
    }
}
