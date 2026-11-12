using System.Runtime.CompilerServices;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Extensions;

public static class BinaryWriteExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, MoneyEarnFlow value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, UsableStorageType value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, StorageGroup value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, ItemBindState value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this BinaryWriter writer, IReadOnlyCollection<StorageValue> storageList)
    {
        writer.Write(storageList.Count);

        foreach (var storage in storageList)
        {
            writer.Write(storage);
        }
    }
    

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