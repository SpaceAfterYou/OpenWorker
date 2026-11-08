using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// One animation of a monster, as the client reads it inside <see cref="MovementIdlesBtResponse"/>.
/// </summary>
public readonly record struct MovementIdleEntry(ActorValue Actor, Vector3 Position, float Yaw, int Animation, float Delay);

/// <summary>
/// Several monsters stand still and play an animation each.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MovementIdlesBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.IdlesBt;

    public MessageOpcode Opcode => new(Group, Command);

    public IReadOnlyCollection<MovementIdleEntry> Entries { get; init; }

    public void Write(BinaryWriter writer)
    {
        var entries = Entries ?? [];

        writer.Write((byte)entries.Count);

        foreach (var entry in entries)
        {
            writer.WriteActor(entry.Actor);
            writer.Write(entry.Position);
            writer.Write(entry.Yaw);
            writer.Write(entry.Animation);
            writer.Write(entry.Delay);
        }
    }
}
