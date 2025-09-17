using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Extensions;

public static class BinaryReaderExtension
{
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, ItemValue value)
    // {
    //     @this.Write(value.nItemID);
    //     @this.Write(value.xSerial);
    //     @this.Write(value.sCount);
    //     @this.Write(value.bBindType);
    //     @this.Write(value.stExtendOption);
    //     @this.Write(value.byUpgrade);
    //     @this.Write(value.byEndurance);
    //     @this.Write(value.bySocketActiveCount);
    //     @this.Write(value.nCashDate);
    //     @this.Write(value.byUpgradeCount);
    //     @this.Write(value.byUpgradeLimit);
    //     @this.Write(value.eFlag);
    //     @this.Write(value.nExp);
    //     @this.Write(value.szBroachState);
    //     @this.Write(value.byRestoreCount);
    //     @this.Write(value.bySealCount);
    //     // @this.Write(value.bySealDelCount);
    //     // @this.Write(value.nAttack);
    //     // @this.Write(value.nDefense);
    //     // @this.Write(value.byUseCount);
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemBindType ReadItemBindType(this BinaryReader reader)
    {
        return (ItemBindType)reader.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UsableStorageType ReadUsableStorageType(this BinaryReader reader)
    {
        return (UsableStorageType)reader.ReadByte();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StorageGroup[] ReadStorageGroupList(this BinaryReader reader, int count)
    {
        return reader
            .ReadBytes(count)
            .Select(b => (StorageGroup)b)
            .ToArray();
    }
}