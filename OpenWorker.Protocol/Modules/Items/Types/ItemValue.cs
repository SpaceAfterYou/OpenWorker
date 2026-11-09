using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Items.Types;

/// <summary>
/// An item as the client and the server send it: what it is, its serial, its count and everything done to it.
/// </summary>
public readonly struct ItemValue : IWireReadable<ItemValue>
{
    public int Item { get; init; }
    public SerialValue Serial { get; init; }
    public short Count { get; init; }
    public ItemBindType BindType { get; init; }
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

    public ItemValue(BinaryReader reader)
    {
        Item = reader.ReadInt32();
        Serial = new SerialValue(reader);
        Count = reader.ReadInt16();
        BindType = reader.ReadItemBindType();
        OptionList = Enumerable
            .Range(0, ItemModuleDefines.OptionCount)
            .Select(_ => new ItemOption(reader))
            .ToList();
        Upgrade = reader.ReadByte();
        Endurance = reader.ReadByte();
        SocketActiveCount = reader.ReadByte();
        DueTo = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64());
        UpgradeCount = reader.ReadByte();
        UpgradeLimit = reader.ReadByte();
        Flag = reader.ReadByte();
        Exp = reader.ReadInt32();
        BroachState = reader.ReadUtf8AsciiStringWithoutTerminator();
        RestoreCount = reader.ReadByte();
        SealCount = reader.ReadByte();
    }

    /// <summary>
    /// Reads an item from a packet of the client, in the layout <c>BinaryWriteExtension.Write</c> writes it. The client is
    /// hostile: a time past the range of a date is a malformed packet.
    /// </summary>
    public static ItemValue Read(ref PacketReader reader)
    {
        var item = reader.ReadInt32();
        var serial = new SerialValue(reader.ReadInt64());
        var count = reader.ReadInt16();
        var bindType = (ItemBindType)reader.ReadByte();
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
            BindType = bindType,
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
}
