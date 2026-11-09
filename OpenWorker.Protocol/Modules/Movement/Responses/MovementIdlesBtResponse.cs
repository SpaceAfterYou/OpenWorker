using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// One animation of a monster, as the client reads it inside <see cref="MovementIdlesBtResponse"/>.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct MovementIdleEntry(ActorValue Actor, Vector3 Position, float Yaw, int Animation, float Delay);

/// <summary>
/// Several monsters stand still and play an animation each.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementIdlesBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.IdlesBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<MovementIdleEntry> Entries { get; init; }
}
