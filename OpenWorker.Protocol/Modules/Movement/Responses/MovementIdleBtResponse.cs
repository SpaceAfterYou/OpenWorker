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
/// A monster stands still and plays an animation of its own.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementIdleBtResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.IdleBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Yaw { get; init; }

    /// <summary>
    /// The animation by its key in the action file of the monster.
    /// </summary>
    public int Animation { get; init; }

    public byte DefaultStep { get; init; }

    /// <summary>
    /// How long the client waits before it moves again. A negative value tells it to play the animation to the end.
    /// </summary>
    public float Delay { get; init; }
}
