using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A player is on a defensive weapon: every client puts it on the weapon, and its own client gives it the weapon's controls.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct MonsterWeaponMountResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.DefensiveWeaponStartBt;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Rider { get; init; }

    public ActorValue Weapon { get; init; }
}
