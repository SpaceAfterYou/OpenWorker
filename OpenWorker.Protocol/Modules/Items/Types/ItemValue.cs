using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// An item as the client and the server send it: what it is, its serial, its count and everything done to it.
/// </summary>
public readonly struct ItemValue : IWireReadable<ItemValue>, IWireWritable
{
    /// <summary>
    /// The state of the broaches of an item with none.
    /// </summary>
    public const string NoBroaches = "000000000000000";

    public int Item { get; init; }
    public SerialValue Serial { get; init; }
    public short Count { get; init; }
    public ItemBindState BindState { get; init; }
    public byte Endurance { get; init; }

    public List<ItemOption> OptionList { get; init; }

    public byte Upgrade { get; init; }
    public byte Flag { get; init; }
    public byte SocketActiveCount { get; init; }
    public DateTimeOffset DueTo { get; init; }
    public byte UpgradeCount { get; init; }
    public byte UpgradeLimit { get; init; }
    public int Exp { get; init; }
    public string BroachState { get; init; }
    public byte RestoreCount { get; init; }
    public byte SealCount { get; init; }

    /// <summary>Reads through <see cref="Read"/>, for code that still holds a <see cref="BinaryReader"/>.</summary>
    public ItemValue(BinaryReader reader)
    {
        this = WireBridge.Read<ItemValue>(reader);
    }

    /// <summary>
    /// Reads an item from a packet of the client, in the layout <see cref="Write(ref PacketWriter)"/> writes it. The client is
    /// hostile: a time past the range of a date is a malformed packet.
    /// </summary>
    public static ItemValue Read(ref PacketReader reader)
    {
        var item = reader.ReadInt32();
        var serial = new SerialValue(reader.ReadInt64());
        var count = reader.ReadInt16();
        var bindState = (ItemBindState)reader.ReadByte();
        var options = new List<ItemOption>(ItemModuleDefines.OptionCount);

        for (var i = 0; i < ItemModuleDefines.OptionCount; i++)
        {
            var type = reader.ReadInt16();

            options.Add(new ItemOption(type, reader.ReadInt32()));
        }

        return new ItemValue
        {
            Item = item,
            Serial = serial,
            Count = count,
            BindState = bindState,
            OptionList = options,
            Upgrade = reader.ReadByte(),
            Endurance = reader.ReadByte(),
            SocketActiveCount = reader.ReadByte(),
            DueTo = reader.ReadDateTimeOffset(),
            UpgradeCount = reader.ReadByte(),
            UpgradeLimit = reader.ReadByte(),
            Flag = reader.ReadByte(),
            Exp = reader.ReadInt32(),
            BroachState = reader.ReadUtf8AsciiStringWithoutTerminator(),
            RestoreCount = reader.ReadByte(),
            SealCount = reader.ReadByte()
        };
    }

    /// <summary>
    /// Writes the item. Its options always go as <see cref="ItemModuleDefines.OptionCount"/> (the missing ones empty), and
    /// a missing state of the broaches as <see cref="NoBroaches"/>: the client takes between one and fifteen characters
    /// there and has no room for a terminator.
    /// </summary>
    public void Write(ref PacketWriter writer)
    {
        writer.WriteInt32(Item);
        writer.WriteInt64(Serial.Number);
        writer.WriteInt16(Count);
        writer.WriteByte((byte)BindState);

        var options = OptionList;

        for (var i = 0; i < ItemModuleDefines.OptionCount; i++)
        {
            var option = options is not null && i < options.Count ? options[i] : default;

            writer.WriteInt16(option.Type);
            writer.WriteInt32(option.Option);
        }

        writer.WriteByte(Upgrade);
        writer.WriteByte(Endurance);
        writer.WriteByte(SocketActiveCount);
        writer.WriteDateTimeOffset(DueTo);
        writer.WriteByte(UpgradeCount);
        writer.WriteByte(UpgradeLimit);
        writer.WriteByte(Flag);
        writer.WriteInt32(Exp);
        writer.WriteUtf8AsciiStringWithoutTerminator(string.IsNullOrEmpty(BroachState) ? NoBroaches : BroachState);
        writer.WriteByte(RestoreCount);
        writer.WriteByte(SealCount);
    }
}
