using System.Numerics;
using System.Runtime.CompilerServices;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.Extensions;

public static class BinaryReaderExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Hero ReadHero(this BinaryReader reader)
    {
        return (Hero)reader.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StorageGroup ReadStorageGroup(this BinaryReader reader)
    {
        return (StorageGroup)reader.ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static MessageDirection ReadMessageDirection(this BinaryReader reader)
    {
        return (MessageDirection)reader.ReadByte();
    }

    internal static EquipItemValueEntry[] ReadEquippedItems(this BinaryReader reader)
    {
        return Enumerable.Range(0, ItemModuleDefines.EquippedItemCount)
            .Select(_ => new EquipItemValueEntry(reader.ReadInt32()))
            .ToArray();
    }

    #region Enums

    public static EnterGateFrom ReadEnterGateFrom(this BinaryReader reader)
    {
        return (EnterGateFrom)reader.ReadByte();
    }

    #endregion Enums
}
