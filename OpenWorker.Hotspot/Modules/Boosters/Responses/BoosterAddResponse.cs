using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Extensions;

namespace OpenWorker.Hotspot.Modules.Boosters.Responses;

[HotspotMessage(Group, Command)]
public readonly record struct SkillUpdateBuffResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.BuffUpdateBt;

    public ActorValue Actor { get; init; }
    public short Buff { get; init; }
    public float Time { get; init; }
    public byte Count { get; init; }
    public int Owner { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Buff);
        writer.Write(Time);
        writer.Write(Count);
        writer.Write(Owner);
    }
}

[HotspotMessage(Group, Command)]
public readonly record struct BoosterAddResponse(short Identifier, TimeSpan Remaining, BoosterConsumeArea Area) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Booster;
    private const BoosterOpcode Command = BoosterOpcode.Add;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(Area);
        writer.Write(Identifier);
        writer.Write(Remaining);
    }
}