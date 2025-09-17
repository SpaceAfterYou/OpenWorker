using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.League.Requests;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

public readonly struct LeagueInfo
{
    public LeagueInfo()
    {
        Identifier = 0;
        Rank = 0;
        GroupType = LeagueListType.Normal;
        Rating = 0;
        MemberCount = 0;
        Exp = 0;
        Name = string.Empty;
        Money = 0;
        CreateDate = 0;
        NoticeDate = 0;
        Master = 0;
        MasterName = string.Empty;
        ViceMasterName = string.Empty;
        
        RewardList = Enumerable
            .Range(0, 9)
            .Select(_ => new LeagueRewardInfo())
            .ToList();
        
        Open = false;
        Card = new LeagueCard();
        Notice = string.Empty;
        
        PositionList = Enumerable
            .Range(0, LeagueModuleDefines.LeaguePositionCount)
            .Select(_ => string.Empty)
            .ToList();
        
        RecruitNotice = string.Empty;
        RecruitNoticeDate = 0;
    }
    
    public LeagueInfo(BinaryReader reader)
    {
        Identifier = reader.ReadInt32();
        Rank = reader.ReadInt32();
        GroupType = reader.ReadLeagueListType();
        Rating = reader.ReadByte();
        MemberCount = reader.ReadInt16();
        Exp = reader.ReadInt64();
        Name = reader.ReadLeagueName();
        Money = reader.ReadInt64();
        CreateDate = reader.ReadInt64();
        NoticeDate = reader.ReadInt64();
        Master = reader.ReadInt32();
        MasterName = reader.ReadPersonName();
        ViceMasterName = reader.ReadPersonName();
        
        RewardList = Enumerable
            .Range(0, LeagueModuleDefines.LeagueRewardCount)
            .Select(_ => new LeagueRewardInfo(reader))
            .ToList();
        
        Open = reader.ReadBoolean();
        Card = new LeagueCard(reader);
        Notice = reader.ReadLeagueNotice();
        
        PositionList = Enumerable
            .Range(0, LeagueModuleDefines.LeaguePositionCount)
            .Select(_ => reader.ReadLeaguePositionName())
            .ToList();
        
        RecruitNotice = reader.ReadLeagueRecruitNotice();
        RecruitNoticeDate = reader.ReadInt64();
    }

    public int Identifier { get; init; }
    public int Rank { get; init; }
    public LeagueListType GroupType { get; init; }
    
    /// <summary>
    /// Added 5 slots per level.
    /// Level 0 - 0 slots.
    /// </summary>
    public byte Rating { get; init; }
    public short MemberCount { get; init; }
    public long Exp { get; init; }
    public string Name { get; init; }
    public long Money { get; init; }
    public long CreateDate { get; init; }
    public long NoticeDate { get; init; }
    public int Master { get; init; }
    public string MasterName { get; init; }
    public string ViceMasterName { get; init; }
    public IReadOnlyList<LeagueRewardInfo> RewardList { get; init; }
    public bool Open { get; init; }
    public LeagueCard Card { get; init; }
    public string Notice { get; init; }
    public IReadOnlyList<string> PositionList { get; init; }
    public string RecruitNotice { get; init; }
    public long RecruitNoticeDate { get; init; }
}