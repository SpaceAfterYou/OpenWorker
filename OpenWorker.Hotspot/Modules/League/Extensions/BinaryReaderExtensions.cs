using OpenWorker.Hotspot.Modules.League.Enums;

namespace OpenWorker.Hotspot.Modules.League.Extensions;

public static class BinaryReaderExtensions
{
    public static LeagueListType ReadLeagueListType(this BinaryReader reader) => (LeagueListType)reader.ReadByte();
}