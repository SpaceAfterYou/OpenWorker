using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Extensions;

/// <summary>
/// The writers of an item for code that still holds a <see cref="BinaryWriter"/> (the tests): through the span format, so
/// the layout is declared once.
/// </summary>
public static class BinaryWriteExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ItemValue value)
    {
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, StorageItemValue value)
    {
        WireBridge.Write(writer, value);
    }
}