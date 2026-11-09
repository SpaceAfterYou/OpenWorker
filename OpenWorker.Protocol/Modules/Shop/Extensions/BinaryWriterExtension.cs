using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Hotspot.Modules.Shop.Extensions;

internal static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ShopCurrency value)
    {
        writer.Write((int)value);
    }
}