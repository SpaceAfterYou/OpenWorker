using System.Diagnostics;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Enums;
using OpenWorker.Hotspot.Modules.League.Requests;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Extensions;

public static class BinaryWriterExtension
{
    internal static void WriteLeagueName(this BinaryWriter writer, string value)
    {
        writer.WriteUtf16UnicodeString(value, 9);
    }
    
    internal static void WriteLeagueNotice(this BinaryWriter writer, string value)
    {
        writer.WriteUtf16UnicodeString(value,1603);
    }
    
    internal static void WriteLeagueRecruitNotice(this BinaryWriter writer, string value)
    {
        writer.WriteUtf16UnicodeString(value,103);
    }
    
    internal static void WriteLeaguePosition(this BinaryWriter writer, string value)
    {
        writer.WriteUtf16UnicodeString(value,23);
    }
    
    internal static void Write(this BinaryWriter writer, LeaguePosition value)
    {
        writer.Write((byte)value);
    }
    
    internal static void Write(this BinaryWriter writer, LeagueCard value)
    {
        writer.Write(value.Value);
    } 
    
    internal static void Write(this BinaryWriter writer, LeagueRewardInfo value)
    {
        writer.Write(value.Auth);
        writer.Write(value.LimitGoldOut);
    }

    internal static void Write(this BinaryWriter writer, LeagueListType value)
    {
        writer.Write((byte)value);
    }
    
    internal static void Write(this BinaryWriter writer, LeagueInfo value)
    {
        writer.Write(value.Identifier);
        writer.Write(value.Rank);
        writer.Write(value.GroupType);
        writer.Write(value.Rating);
        writer.Write(value.MemberCount);
        writer.Write(value.Exp);
        writer.WriteLeagueName(value.Name);
        writer.Write(value.Money);
        writer.Write(value.CreateDate);
        writer.Write(value.NoticeDate);
        writer.Write(value.Master);
        writer.WritePersonName(value.MasterName);
        writer.WritePersonName(value.ViceMasterName);

        Debug.Assert(value.RewardList.Count is LeagueModuleDefines.LeagueRewardCount);
        foreach (var reward in value.RewardList)
        {
            writer.Write(reward);
        }
        
        writer.Write(value.Card);
        writer.WriteLeagueNotice(value.Notice);

        Debug.Assert(value.PositionList.Count is LeagueModuleDefines.LeaguePositionCount);
        foreach (var position in value.PositionList)
        {
            writer.WriteLeaguePosition(position);
        }
        
        writer.Write(value.Open);
        writer.WriteLeagueRecruitNotice(value.RecruitNotice);
        writer.Write(value.RecruitNoticeDate);
    }

    internal static void Write(this BinaryWriter writer, LeagueInfoForMember value)
    {
        writer.Write(value.League);
        writer.Write(value.Position);
        writer.Write(value.LeagueExp);
        writer.Write(value.JoinDate);
        writer.Write(value.ApplicationDate);
    }

    internal static void Write(this BinaryWriter writer, LeagueMember value)
    {
        writer.Write(value.LeagueInfo);
        writer.Write(value.Login);
        writer.Write(value.World);
        writer.Write(value.Channel);
        writer.Write(value.Person);
        writer.WriteLeagueName(value.Name);
        writer.Write(value.Level);
        writer.Write(value.BoardLimitTime);
        writer.Write(value.Hero);
        writer.Write(value.PlayDate);
    }
}