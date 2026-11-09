using System.Runtime.CompilerServices;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Hotspot.Modules.Items.Extensions;

public static class BinaryReaderExtension
{
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