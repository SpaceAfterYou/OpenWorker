using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Monster.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct MonsterSuperArmorGageResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Monster;
    private const MonsterOpcode Command = MonsterOpcode.SuperArmorGage;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public float Current { get; init; }
    public float Max { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Current);
        writer.Write(Max);
    }
}
