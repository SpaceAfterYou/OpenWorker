using OpenWorker.Hotspot.Modules.Ranking.Types;

namespace OpenWorker.Hotspot.Modules.Ranking.Extensions;

internal static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, RankingType value)
    {
        writer.Write((byte)value);
    }
}