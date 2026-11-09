using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Movement.Responses;

/// <summary>
/// A monster turns on the spot.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementUpdateDirResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Move;
    private const MoveOpcode Command = MoveOpcode.UpdateDir;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Actor { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }

    /// <summary>
    /// Turn at once instead of smoothly.
    /// </summary>
    public bool Direct { get; init; }
}
