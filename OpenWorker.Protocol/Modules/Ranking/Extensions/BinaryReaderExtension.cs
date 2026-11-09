using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Extensions;

internal static class BinaryReaderExtension
{
    internal static RankingType ReadRankingType(this BinaryReader reader)
    {
        return (RankingType)reader.ReadByte();
    }
}