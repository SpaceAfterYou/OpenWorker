using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Monster.Requests;

/// <summary>
/// The player gets off its defensive weapon. The client waits for no answer: everybody is told when it is off.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct MonsterWeaponDismountRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.DefensiveWeaponEndReq;

    /// <summary>
    /// The weapon, in the form the client keeps an actor in a packet.
    /// </summary>
    public int Weapon { get; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
