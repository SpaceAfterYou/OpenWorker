using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Monster.Requests;

/// <summary>
/// The player presses an attack key while it controls a monster: the monster plays one of its skills. Nothing answers it.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MonsterControlAttackRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.ControlMonsterAttackReq;

    /// <summary>
    /// The controlled monster, in the form the client keeps an actor in a packet.
    /// </summary>
    public int Monster { get; }

    /// <summary>
    /// Which skill of the monster's row: 0 for the first attack key, 1 for the second.
    /// </summary>
    public byte Attack { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
