using OpenWorker.Hotspot.Modules.Login.Enums;

namespace OpenWorker.Hotspot.Modules.Login.Extensions;

internal static class BinaryWriterExtension
{
    internal static void Write(this BinaryWriter writer, LoginType loginType)
    {
        writer.Write((byte)loginType);
    }
}