using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command)]
public readonly struct PartyAcceptRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Accept;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyChangeMasterRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.ChangeMaster;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyLeaveRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Leave;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyUpdateMemberInfoRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateMemberInfo;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyDeleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Delete;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyCancelRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Cancel;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyUpdateInfoRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateInfo;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAddMemberRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AddMember;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyUpdateMemberHpRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateMemberHp;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyUpdateMemberSgRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateMemberSg;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyMatchingCheckRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingCheck;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyMatchingResetRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingReset;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyMatchingWaitRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingWait;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAutoPenaltyRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AutoPenalty;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitMyApplyListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitMyApplyList;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitAddRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitAdd;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApply;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyAcceptRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyAccept;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyRejectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyReject;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyUpdateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyUpdate;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitDelRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitDel;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyList;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitInfoRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitInfo;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAwaiterAddRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterAdd;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAwaiterDelRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterDel;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAwaiterListRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterList;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyInfoRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyInfo;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyAwaiterInfoRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterInfo;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyRecruitApplyNoticeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyNotice;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyMazeClearRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MazeClear;
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct PartyUnknown5Request(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Unknown5;
    
    public MessageOpcode Opcode => new(Group, Command);
}
