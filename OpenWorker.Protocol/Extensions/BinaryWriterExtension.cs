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
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Extensions;

/// <summary>
/// The writers of the values of the messages that still write through <see cref="BinaryWriter"/>. A value that has a span
/// format (<see cref="IWireWritable"/>) is written through it here too, so its layout is declared once.
/// </summary>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, BroachSlotState value)
    {
        writer.Write((byte)value);
    }

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
        Debug.Assert(value.dwItemID is { Length: ST_ITEM_BROACH.ItemCount });

        writer.Write(value.biSerial);

        foreach (var item in value.dwItemID)
        {
            writer.Write(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ST_ITEM_BROACH> list)
    {
        writer.Write(checked((byte)list.Count));
        foreach (var value in list)
        {
            writer.Write(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ST_SOCKET_DATA value)
    {
        writer.Write(value.biEquipSerial);
        writer.Write(value.dwSocketID);
        writer.Write(value.bySocketPos);
        writer.Write(value.stExtendOption);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, IReadOnlyCollection<ST_SOCKET_DATA> list)
    {
        writer.Write(checked((byte)list.Count));
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
    public static void Write(this BinaryWriter writer, EnterMapResultValue value)
    {
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, ZoneValue value)
    {
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, WorldValue value)
    {
        WireBridge.Write(writer, value);
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
        writer.Write((byte)value.SecondPassword);
        writer.Write((byte)value.TradePassword);
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
        writer.WriteUtf8AsciiString(gate.AccountId);
        writer.Write(gate.NetCafe);
        writer.Write(gate.ClassScene);
        writer.Write(gate.WorldType);
        writer.Write(gate.UsePvpDistrict);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PersonValue value)
    {
        WireBridge.Write(writer, value);
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
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, SpeedValueEntry value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, AbilityValue value)
    {
        WireBridge.Write(writer, value);
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
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PersonLeagueCardValueEntry value)
    {
        writer.Write(value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, LeagueValue value)
    {
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, PrivateShopValue value)
    {
        WireBridge.Write(writer, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this BinaryWriter writer, StatusEffectValueEntry value)
    {
        WireBridge.Write(writer, value);
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
        WireBridge.Write(writer, value);
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

    extension(BinaryWriter writer)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteUnicodeString(string value)
        {
            var str = Encoding.Unicode.GetBytes(value);

            writer.Write(str);
            writer.Write(byte.MinValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf8UnicodeString(string value, int max = 0)
        {
            var length = (short)(value.Length + 1);

            writer.Write(length);
            writer.WriteUnicodeString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUtf16UnicodeString(string value, int max = 0)
        {
            var length = (short)(2 * value.Length + 1);

            writer.Write(length);
            writer.WriteUnicodeString(value);
        }
    }

#endregion Unicode String
}
