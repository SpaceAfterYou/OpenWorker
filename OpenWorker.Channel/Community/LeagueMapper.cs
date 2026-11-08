using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Persistence;

namespace OpenWorker.Channel.Community;

/// <summary>
/// What the database knows about a league, in the form the client reads.
/// </summary>
public static class LeagueMapper
{
    public static long ToUnix(DateTime? value)
    {
        return value is { } time ? new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)).ToUnixTimeSeconds() : 0;
    }

    public static int CardOf(LeagueRecord league)
    {
        return (ushort)league.Emblem | (league.Border << 16);
    }

    public static LeagueInfo ToInfo(LeagueRecord league)
    {
        return new LeagueInfo
        {
            Identifier = league.Id,
            Rank = 0,
            GroupType = LeagueListType.Normal,
            Rating = league.Rating,
            MemberCount = (short)league.MemberCount,
            Exp = league.Exp,
            Name = league.Name,
            Money = league.Money,
            CreateDate = ToUnix(league.CreatedAt),
            NoticeDate = ToUnix(league.NoticeAt),
            Master = league.MasterId,
            MasterName = league.MasterName,
            ViceMasterName = league.SubMasterName,
            RewardList = league.Auth
                .Zip(league.LimitGoldOut, (auth, limit) => new LeagueRewardInfo { Auth = auth, LimitGoldOut = limit })
                .ToList(),
            Open = league.Open,
            Card = new LeagueCard { Logo = league.Emblem, Border = league.Border },
            Notice = league.Notice,
            PositionList = league.PositionNames.ToList(),
            RecruitNotice = league.RecruitNotice,
            RecruitNoticeDate = ToUnix(league.RecruitNoticeAt)
        };
    }

    public static LeagueMember ToMember(LeagueMemberRecord member, PresenceRecord? online)
    {
        return new LeagueMember
        {
            LeagueInfo = new LeagueInfoForMember
            {
                League = member.League,
                Position = (LeaguePosition)member.Position,
                LeagueExp = member.Exp,
                JoinDate = ToUnix(member.JoinedAt),
                ApplicationDate = 0
            },
            Login = online is not null,
            World = online?.Location ?? 0,
            Channel = (byte)(online?.Channel ?? 0),
            Person = member.Person,
            Name = member.Name,
            Level = online?.Level ?? PersonModuleDefines.DefaultLevel,
            BoardLimitTime = 0,
            Hero = (Hero)member.Hero,
            PlayDate = (ulong)Math.Max(0, ToUnix(member.LastLogout))
        };
    }

    public static LeagueApplicant ToApplicant(LeagueApplicantRecord applicant)
    {
        return new LeagueApplicant
        {
            League = applicant.League,
            Actor = applicant.Person,
            Name = applicant.Name,
            Level = PersonModuleDefines.DefaultLevel,
            Date = ToUnix(applicant.At),
            Class = applicant.Hero
        };
    }

    public static LeagueBoardEntry ToBoard(LeagueBoardRecord note)
    {
        return new LeagueBoardEntry
        {
            Serial = note.Serial,
            League = note.League,
            Name = note.Name,
            Message = note.Message,
            Date = ToUnix(note.At)
        };
    }
}
