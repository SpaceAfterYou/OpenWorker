using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Channels.Requests;
using OpenWorker.Hotspot.Modules.Persons.DataTypes;

namespace OpenWorker.Hotspot.Modules.Channels.Responses;

/// <summary>
/// 
/// </summary>
/// <param name="errorCode">0 - no error. Message code in tb_(.*?)_script.res</param>
/// <param name="divergence">tb_divergence.res</param>
/// <param name="swap"></param>
[HotspotMessage(Group, Command)]
public readonly struct SkillActiveSkillResponse(int errorCode, int divergence, bool swap) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.ActiveSkillRes;

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(errorCode);
        writer.Write(divergence);
        writer.Write(swap);
    }

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct SkillDeckBonusResponse(SkillDeckBonus deck) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.SkillDeckBonus;

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(deck.Value);
    }

    public MessageOpcode Opcode => new(Group, Command);
}

/// <summary>
/// 
/// </summary>
/// <param name="error">0 - no error. Message code in tb_(.*?)_script.res</param>
[HotspotMessage(Group, Command)]
public readonly struct SkillPassiveResponse(short error) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Skill;
    private const SkillOpcode Command = SkillOpcode.PassiveSkillRes;

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(error);
    }

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct ChannelChangeResponse(EnterMapResultValue value) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Change;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(value);
    }

    public static ChannelChangeResponse Error => new(EnterMapResultValue.Error);
}