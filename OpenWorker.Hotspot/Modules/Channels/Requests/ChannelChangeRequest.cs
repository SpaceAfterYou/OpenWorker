using System.Numerics;
using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Extensions;

namespace OpenWorker.Hotspot.Modules.Channels.Requests;

public readonly struct SkillPositionValue
{
    public Vector3 xPos { get; init; } 
    public float fAngle { get; init; } 
    public short nMotionClass { get; init; } 
    
    /// <summary>
    /// TODO: Unused bSwapSkill or bInputFlag
    /// </summary>
    // public bool bSwapSkill { get; init; } 
    
    public bool bInputFlag { get; init; } 
    
    public SkillPositionValue(BinaryReader reader)
    {
        xPos = reader.ReadVector3();
        fAngle = reader.ReadSingle();
        nMotionClass = reader.ReadInt16();
        // bSwapSkill = reader.ReadBoolean();
        bInputFlag = reader.ReadBoolean();
    }
}

public readonly struct ActiveSkillValue
{
    public int nSkillID { get; init; }
    public ActorValue uxUseActorID { get; init; }
    public SkillPositionValue Position { get; init; }
    public int nDivergenceID { get; init; }
    public int nParentSkillID { get; init; }
    public byte bySkillDeckIndex { get; init; }
    public int nRandomKey { get; init; }
    
    public ActiveSkillValue(BinaryReader reader)
    {
        nSkillID = reader.ReadInt32();
        uxUseActorID = new ActorValue(reader);
        Position = new SkillPositionValue(reader);
        nDivergenceID = reader.ReadInt32();
        nParentSkillID = reader.ReadInt32();
        bySkillDeckIndex = reader.ReadByte();  
        nRandomKey = reader.ReadInt32();
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SkillPassiveEndRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.PassiveSkillEndReq;

    public ActiveSkillValue Skill { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}

public readonly struct SkillDeckBonus
{
    public short[] Value { get; } = new short[4];

    public SkillDeckBonus(BinaryReader reader)
    {
        Value = reader.ReadUInt16AsArray(4);
    }
}

[HotspotMessage(Group, Command)]
public readonly struct SkillActiveSkillRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ActiveSkillReq;

    public ActiveSkillValue Skill { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct SkillDeckBonusRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillDeckBonus;

    public SkillDeckBonus Deck { get; } = new(reader);

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct SkillPassiveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.PassiveSkillRes;

    public short Channel { get; } = reader.ReadInt16();

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ChannelChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Change;

    public short Channel { get; } = reader.ReadInt16();

    public MessageOpcode Opcode => new(Group, Command);
}