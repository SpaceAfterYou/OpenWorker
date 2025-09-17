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
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static ChatNotifyType ReadChatNotifyType(this BinaryReader reader)
    // {
    //     return (ChatNotifyType)reader.ReadByte();
    // }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static MapValue ReadMapValue(this BinaryReader reader)
    {
        return reader.ReadUInt64();
    }

    internal static EquipItemValueEntry[] ReadEquippedItems(this BinaryReader reader)
    {
        return Enumerable.Range(0, ItemModuleDefines.EquippedItemCount)
            .Select(_ => new EquipItemValueEntry(reader.ReadInt32()))
            .ToArray();
    }

    #region Enums

    //
    // public static BoosterDecreaseTime ReadBoosterDecreaseTime(this BinaryReader reader)
    // {
    //     return (BoosterDecreaseTime)reader.ReadByte();
    // }
    //
    // public static Character ReadClass(this BinaryReader reader)
    // {
    //     return (Character)reader.ReadByte();
    // }
    //
    //
    // public static LogOutType ReadLogoutWayType(this BinaryReader reader)
    // {
    //     return (LogOutType)reader.ReadByte();
    // }
    //
    // public static StorageType ReadStorageType(this BinaryReader reader)
    // {
    //     return (StorageType)reader.ReadByte();
    // }
    //
    // public static Advancement ReadClassAdvancement(this BinaryReader reader)
    // {
    //     return (Advancement)reader.ReadByte();
    // }
    //
    // public static Permisson ReadPermission(this BinaryReader reader)
    // {
    //     return (Permisson)reader.ReadByte();
    // }
    //
    // public static GroupRole ReadGroupRoleType(this BinaryReader reader)
    // {
    //     return (GroupRole)reader.ReadByte();
    // }

    public static EnterGateFrom ReadEnterGateFrom(this BinaryReader reader)
    {
        return (EnterGateFrom)reader.ReadByte();
    }
    //
    // public static ItemClassifySlotType ReadItemClassifySlotType(this BinaryReader reader)
    // {
    //     return (ItemClassifySlotType)reader.ReadByte();
    // }
    //
    // public static ItemClassifyInventoryType ReadItemClassifyInventoryType(this BinaryReader reader)
    // {
    //     return (ItemClassifyInventoryType)reader.ReadByte();
    // }
    //
    // public static AuthType ReadAuthType(this BinaryReader reader)
    // {
    //     return (AuthType)reader.ReadByte();
    // }

    #endregion Enums
}