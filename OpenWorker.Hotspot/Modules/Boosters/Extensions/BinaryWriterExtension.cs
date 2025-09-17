using OpenWorker.Hotspot.Modules.Boosters.Enums;

namespace OpenWorker.Hotspot.Modules.Boosters.Extensions;

internal static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, BoosterConsumeArea area)
    {
        writer.Write((byte)area);
    }
}