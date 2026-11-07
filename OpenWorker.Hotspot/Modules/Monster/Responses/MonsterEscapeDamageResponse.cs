using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

/// <summary>
/// A monster takes a defense of its own for a while to get out of a fight.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MonsterEscapeDamageResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.EscapeDamageBt;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public byte DefenseType { get; init; }
    public float Time { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(DefenseType);
        writer.Write(Time);
    }
}
