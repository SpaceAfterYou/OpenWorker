using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.League.Enums;

namespace OpenWorker.Hotspot.Modules.League.Extensions;

public static class BinaryReaderExtension
{
    internal static string ReadLeagueName(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(9);
    }
    
    internal static string ReadLeagueNotice(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(1603);
    }
    
    internal static string ReadLeagueRecruitNotice(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(103);
    }
    
    internal static string ReadLeaguePositionName(this BinaryReader reader)
    {
        return reader.ReadUtf8UnicodeString(23);
    }
    
    internal static LeaguePosition ReadLeaguePosition(this BinaryReader reader)
    {
        return (LeaguePosition)reader.ReadByte();
    }
}