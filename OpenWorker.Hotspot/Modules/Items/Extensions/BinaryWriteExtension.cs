using System.Runtime.CompilerServices;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;

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
    internal static void Write(this BinaryWriter writer, ItemBindType value)
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
        writer.Write(value.Item);
        writer.Write(value.Serial);
        writer.Write(value.Count);
        writer.Write(value.BindType);
        writer.Write(value.OptionList);
        writer.Write(value.Upgrade);
        writer.Write(value.Endurance);
        writer.Write(value.SocketActiveCount);
        writer.Write(value.DueTo);
        writer.Write(value.UpgradeCount);
        writer.Write(value.UpgradeLimit);
        writer.Write(value.Flag);
        writer.Write(value.Exp);
        writer.WriteUtf8AsciiString(value.BroachState);
        writer.Write(value.RestoreCount);
        writer.Write(value.SealCount);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, StorageItemValue value)
    {
        writer.Write(value.Group);
        writer.Write(value.Index);
        writer.Write(value.Item);
    }
}