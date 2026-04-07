using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Dtos;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Messages.Response.Person.Values.Entries;
using OpenWorker.Hotspot.Modules.Friends.Responses;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Persons.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.SoulWorker.Network.DataTypes.Enums;

namespace OpenWorker.Hotspot.Extensions;

public static class BinaryWriterExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteActor(this BinaryWriter writer, int value)
    {
        writer.Write(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteActorList(this BinaryWriter writer, ReadOnlySpan<ActorValue> actors)
    {
        writer.Write((byte)actors.Length);

        foreach (var actor in actors)
        {
            writer.WriteActor(actor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteActorList(this BinaryWriter writer, ActorValue[] actors) =>
        writer.WriteActorList(actors.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, EnterMapType value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, DateTime value)
    {
        writer.Write(new DateTimeOffset(value).ToUnixTimeSeconds());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, WorldType value)
    {
        writer.Write((byte)value);
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, ChatNotifyType value)
    // {
    //     @this.Write((int)value);
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, BroachSlotState value)
    {
        writer.Write((byte)value);
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter writer, IReadOnlyCollection<ItemValue> list)
    // {
    //     writer.Write((short)list.Count);
    //     foreach (var value in list)
    //     {
    //         writer.Write(value);
    //     }
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, StorageValue value)
    {
        writer.Write(value.Storage);
        writer.Write(value.Slot);
        writer.Write(value.Item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ST_ITEM_BROACH value)
    {
        writer.Write(value.biSerial);
        writer.Write(value.dwItemID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ST_ITEM_BROACH> list)
    {
        writer.Write((short)list.Count);
        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ST_SOCKET_DATA value)
    {
        writer.Write(value.dwSocketID);
        writer.Write(value.bySocketPos);
        writer.Write(value.stExtendOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ST_SOCKET_DATA> list)
    {
        writer.Write((short)list.Count);
        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, BroachValue value)
    {
        writer.Write(value.SlotState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<BroachValue> list)
    {
        Debug.Assert(list.Count is 16);

        writer.Write((short)list.Count);
        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ItemOption value)
    {
        writer.Write(value.Type);
        writer.Write(value.Option);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ItemOption> list)
    {
        Debug.Assert(list.Count is 5);

        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, SerialValue value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ST_STAT value)
    {
        writer.Write(value.fValue);
        writer.Write(value.wStatID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ST_STAT> list)
    {
        writer.Write((byte)list.Count);
        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ST_OTHER_CHARINFO value)
    {
        writer.Write(value.shLevel);
        writer.Write(value.byState);
        writer.WriteCommunityComment(value.szComment);
        writer.Write(value.m_vecBaseStat);
        writer.Write(value.m_vecStat);
        writer.Write(value.m_vecEquipItem);
        writer.Write(value.byEchelonLevel);
        writer.Write(value.nEchelonExp);
        writer.WriteCommunityMemo(value.szMemo);
        writer.Write(value.nEquipMemorySlot);
        writer.Write(value.byClass);
        writer.WritePersonName(value.szName);
        writer.Write(value.stInsideTitle);
        writer.Write(value.stOutsideTitle);
        writer.Write(value.vecSocketList);
        writer.Write(value.vecBroachList);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, EnterMapResultValue value)
    {
        writer.Write(value.Zone);
        writer.Write(value.ChangeServer);
        writer.Write(value.ChangeType);
        writer.Write(value.Result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ZoneValue value)
    {
        writer.WriteActor(value.Actor);
        writer.Write(value.Account);
        writer.Write(value.Server);
        writer.Write(value.Jump);
        writer.WriteMapValue(value.Map);
        writer.WriteMapValue(value.Parent);
        writer.WriteUtf8AsciiStringWithoutTerminator(value.Address, 16);
        writer.Write(value.Port);
        writer.Write(value.World);
        writer.Write(value.Type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, WorldValue value)
    {
        writer.Write(value.Location);
        writer.WriteMapValue(value.Map);
        writer.Write(value.Position);
        writer.Write(value.Rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteMapValue(this BinaryWriter writer, MapValue value)
    {
        writer.Write(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, AppearanceValue value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ProtectionStateValue value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, CharacterInfoGatePayload gate)
    {
        writer.Write(gate.PlaceholderInt);
        writer.Write(gate.Gold);
        writer.Write(gate.CommonStep);
        writer.Write(gate.ConsumeStep);
        writer.Write(gate.CostumeStep);
        writer.Write(gate.CardStep);
        writer.Write(gate.UserDb);
        writer.Write(gate.SyncUser);
        writer.Write(gate.BattlePoint);
        writer.Write(gate.Ether);
        writer.Write(gate.FriendPoint);
        writer.WriteUtf16UnicodeString(gate.AccountId, 21);
        writer.Write(gate.NetCafe);
        writer.Write(gate.ClassScene);
        writer.Write(gate.WorldType);
        writer.Write(gate.UsePvpDistrict);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PersonValue value)
    {
        writer.WriteActor(value.Actor);
        writer.Write(value.InfoValue);
        writer.Write(value.Level);
        writer.Write(value.Faction);
        writer.Write(value.Account);
        writer.Write(value.StuffLevel);
        writer.Write(value.PvPKillCount);
        writer.Write(value.PrimaryWeapon);
        writer.Write(value.SecondaryWeapon);
        writer.Write(value.EquipItems);
        writer.Write(value.Title);
        writer.Write(value.League);
        writer.Write(value.Ability);
        writer.Write(value.PrivateShop);
        writer.Write(value.FatiguePoints);
        writer.Write(value.Rank);
        writer.Write(value.BattlePose);
        writer.Write(value.Status);
        writer.Write(value.StatusEffects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<StatusEffectValueEntry> values)
    {
        writer.Write((byte)values.Count);

        foreach (var value in values)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PersonInfoValue value)
    {
        writer.WriteUtf16UnicodeString(value.Name);
        writer.Write(value.Hero);

        writer.Write(value.AppearanceShape);
        writer.Write(value.AppearanceLook);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, SpeedValueEntry value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, AbilityValue value)
    {
        writer.Write(value.Health);
        writer.Write(value.SoulGain);
        writer.Write(value.SoulVapor);
        writer.Write(value.Stamina);
        writer.Write(value.SuperArmor);

        writer.Write(value.Speed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, EquipItemValueEntry[] values)
    {
        Debug.Assert(values.Length is ItemModuleDefines.EquippedItemCount);

        foreach (var value in values)
        {
            writer.Write(value.Id);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, EquipItemValueEntry value)
    {
        writer.Write(value.Upgrade);
        writer.Write(value.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PersonLeagueCardValueEntry value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, LeagueValue value)
    {
        writer.Write(value.Id);
        writer.WriteUtf16UnicodeString(value.Name);
        writer.Write(value.Card);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PrivateShopValue value)
    {
        writer.Write(value.Type);
        writer.WriteUtf16UnicodeString(value.Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, StatusEffectValueEntry value)
    {
        writer.Write(value.Id);
        writer.Write(value.Time);
        writer.Write(value.Count);
        writer.Write(value.Owner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, FatiguePointsValue value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, AbilityValueEntry value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, TitleValue value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, RankValue value)
    {
        writer.Write(value.Level);
        writer.Write(value.Experience);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, MessageDirection value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, SessionValue value)
    {
        writer.Write(value.Key);
    }

    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void WriteCharacterInfoResult(this BinaryWriter @this, CharacterInfoResult value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void WriteSyncUserFlags(this BinaryWriter @this, SyncUserFlags value) => @this.Write((int)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void WriteUserFlags(this BinaryWriter @this, UserFlags value) => @this.Write((int)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, MapChangeType value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, EnterMapResult value) => @this.Write((int)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, DistrictConnectResult value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, UserOptionStatus value) => @this.Write((short)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, GateEnterResult value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, Advancement value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, ChannelLoadStatus value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, StatType value) => @this.Write((short)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, DistrictLogOutStatus value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, LogOutType value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, NpcVisablity value) => @this.Write((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, AccessLevel value)
    {
        writer.Write((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, LoginErrorMessageCode value)
    {
        writer.Write((int)value);
    }
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, SpecialOption value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, ChatType value) => @this.Write((int)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, Character value) => @this.Write((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, GateWorkload value)
    {
        writer.Write((int)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IEnumerable<byte> values)
    {
        writer.Write(values as byte[] ?? values.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IEnumerable<short> values)
    {
        writer.WriteBlock(sizeof(int), values as short[] ?? values.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IEnumerable<int> values)
    {
        writer.WriteBlock(sizeof(int), values as int[] ?? values.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IEnumerable<bool> values)
    {
        writer.WriteBlock(sizeof(bool), values as bool[] ?? values.ToArray());
    }

    private static void WriteBlock(this BinaryWriter writer, int size, Array src)
    {
        var result = new byte[src.Length * size];
        Buffer.BlockCopy(src, 0, result, 0, result.Length);

        writer.Write(result);
    }

    #region Unicode String

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf8UnicodeStringWithoutTerminator(this BinaryWriter writer, string value)
    {
        var length = (short)value.Length;

        writer.Write(length);
        writer.Write(Encoding.Unicode.GetBytes(value));
    }

    #endregion Unicode String

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, ProfileStatus value) => @this.Write((byte)value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, PacketMagick value) => @this.Write(value.Value);
    //
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static void Write(this BinaryWriter @this, PacketProtocol value) => @this.Write((byte)value);
    //

    #region Ascii String

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsciiString(this BinaryWriter writer, string value)
    {
        writer.Write(Encoding.ASCII.GetBytes(value));
        writer.Write(byte.MinValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf8AsciiString(this BinaryWriter writer, string value)
    {
        var length = (short)(value.Length + 1);
        writer.Write(length);

        writer.WriteAsciiString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf16AsciiString(this BinaryWriter writer, string value)
    {
        var length = (short)(value.Length * 2 + 1);
        writer.Write(length);

        writer.WriteAsciiString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf8AsciiStringWithoutTerminator(this BinaryWriter writer, string value, int max = 0)
    {
        var length = (short)value.Length;

        writer.Write(length);
        writer.Write(Encoding.ASCII.GetBytes(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAsciiStringWithoutTerminator(this BinaryWriter writer, string value)
    {
        writer.Write(Encoding.ASCII.GetBytes(value));
    }

    #endregion Ascii String

    #region Unicode String

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnicodeString(this BinaryWriter writer, string value)
    {
        var str = Encoding.Unicode.GetBytes(value);

        writer.Write(str);
        writer.Write(byte.MinValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf8UnicodeString(this BinaryWriter writer, string value, int max = 0)
    {
        var length = (short)(value.Length + 1);

        writer.Write(length);
        writer.WriteUnicodeString(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUtf16UnicodeString(this BinaryWriter writer, string value, int max = 0)
    {
        var length = (short)(2 * value.Length + 1);

        writer.Write(length);
        writer.WriteUnicodeString(value);
    }

    #endregion Unicode String
}
