using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.DataTypes;

/// <summary>
/// A league as the client reads it. What the client sends of a league comes in another order (the flag before the card)
/// and with its texts counted in bytes, so the reading is written by hand.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct LeagueInfo : IWireReadable<LeagueInfo>
{
    public LeagueInfo()
    {
        Name = string.Empty;
        MasterName = string.Empty;
        ViceMasterName = string.Empty;
        RewardList = Enumerable.Repeat(new LeagueRewardInfo(), LeagueModuleDefines.LeagueRewardCount).ToList();
        Notice = string.Empty;
        PositionList = Enumerable.Repeat(string.Empty, LeagueModuleDefines.LeaguePositionCount).ToList();
        RecruitNotice = string.Empty;
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

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }

    public long Money { get; init; }
    public long CreateDate { get; init; }
    public long NoticeDate { get; init; }
    public int Master { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string MasterName { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string ViceMasterName { get; init; }

    [WireArray(Count = LeagueModuleDefines.LeagueRewardCount)]
    public IReadOnlyList<LeagueRewardInfo> RewardList { get; init; }

    public LeagueCard Card { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Notice { get; init; }

    [WireArray(Count = LeagueModuleDefines.LeaguePositionCount, ElementString = WireStringFormat.Utf16Unicode)]
    public IReadOnlyList<string> PositionList { get; init; }

    public bool Open { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string RecruitNotice { get; init; }

    public long RecruitNoticeDate { get; init; }

    public static LeagueInfo Read(ref PacketReader reader)
    {
        var identifier = reader.ReadInt32();
        var rank = reader.ReadInt32();
        var groupType = (LeagueListType)reader.ReadByte();
        var rating = reader.ReadByte();
        var memberCount = reader.ReadInt16();
        var exp = reader.ReadInt64();
        var name = reader.ReadUtf8UnicodeString();
        var money = reader.ReadInt64();
        var createDate = reader.ReadInt64();
        var noticeDate = reader.ReadInt64();
        var master = reader.ReadInt32();
        var masterName = reader.ReadUtf8UnicodeString();
        var viceMasterName = reader.ReadUtf8UnicodeString();
        var rewards = new LeagueRewardInfo[LeagueModuleDefines.LeagueRewardCount];

        for (var index = 0; index < rewards.Length; index++)
        {
            rewards[index] = LeagueRewardInfo.Read(ref reader);
        }

        var open = reader.ReadBoolean();
        var card = LeagueCard.Read(ref reader);
        var notice = reader.ReadUtf8UnicodeString();
        var positions = new string[LeagueModuleDefines.LeaguePositionCount];

        for (var index = 0; index < positions.Length; index++)
        {
            positions[index] = reader.ReadUtf8UnicodeString();
        }

        return new LeagueInfo
        {
            Identifier = identifier,
            Rank = rank,
            GroupType = groupType,
            Rating = rating,
            MemberCount = memberCount,
            Exp = exp,
            Name = name,
            Money = money,
            CreateDate = createDate,
            NoticeDate = noticeDate,
            Master = master,
            MasterName = masterName,
            ViceMasterName = viceMasterName,
            RewardList = rewards,
            Open = open,
            Card = card,
            Notice = notice,
            PositionList = positions,
            RecruitNotice = reader.ReadUtf8UnicodeString(),
            RecruitNoticeDate = reader.ReadInt64()
        };
    }
}
