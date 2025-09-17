using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command)]
public readonly struct LeagueApplicantRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Applicant;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueBoardRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Board;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueWithdrawRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Withdraw;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueKickRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Kick;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueInviteRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Invite;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueInviteAcceptRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InviteAccept;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueInviteRejectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InviteReject;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueApplicantAcceptRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.ApplicantAccept;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueApplicantRejectRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.ApplicantReject;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueOverlapNameRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.OverlapName;

    public string Name { get; } = reader.ReadUtf8UnicodeString();
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueNoticeChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.NoticeChange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueAuthChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.AuthChange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeaguePositionNameChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.PositionNameChange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueMemberPositionChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.MemberPositionChange;

    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueOpenOrNotRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.OpenOrNot;

    public int League { get; } = reader.ReadInt32();
    public bool Status { get; } = reader.ReadBoolean();
    
    public MessageOpcode Opcode => new(Group, Command);
}

[HotspotMessage(Group, Command)]
public readonly struct LeagueRecruitNoticeRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.RecruitNotice;

    public MessageOpcode Opcode => new(Group, Command);
}
