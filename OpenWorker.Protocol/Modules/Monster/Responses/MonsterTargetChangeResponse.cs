using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A monster looks at somebody else now. No target is the actor with every bit set.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MonsterTargetChangeResponse : IResponseHotspotMessage
{
    public const int NoTarget = -1;

    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.TargetChangeBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Actor { get; init; }

    /// <summary>
    /// The actor in the form the client keeps it in a packet.
    /// </summary>
    public int Target { get; init; }
}
