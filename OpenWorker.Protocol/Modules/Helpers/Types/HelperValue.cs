using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Hotspot.Modules.Helpers.Types;

/// <summary>
/// A stat of a helper and its value. The client keeps the stats of a helper in a table of 61 places by the number of the
/// stat, so the number has to be below 61.
/// </summary>
public readonly record struct HelperStatValue(byte Type, float Value) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Type);
        writer.Write(Value);
    }
}

/// <summary>
/// What a friend adds to the helpers of the person.
/// </summary>
public readonly record struct HelperSupportInfoValue(int Friend, byte SupportType, float Value, DateTimeOffset Date, byte RewardType, bool Register) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Friend);
        writer.Write(SupportType);
        writer.Write(Value);
        writer.Write(Date.ToUnixTimeSeconds());
        writer.Write(RewardType);
        writer.Write(Register);
    }
}

/// <summary>
/// A helper as the client reads it: the helper, the three items it wears, the friend whose orb it wears, whether it is
/// called, two lists of stats and its place in the list.
/// </summary>
public readonly struct HelperValue : IWritableData
{
    /// <summary>
    /// The number of items a helper can wear: a weapon, a gear and a third slot no item uses.
    /// </summary>
    public const int ItemSlots = 3;

    /// <summary>
    /// The state of the broach of an item that has none.
    /// </summary>
    private const string NoBroach = "000000000000000";

    /// <summary>
    /// A slot with nothing in it: no item and no serial number, nothing set. The client reads it as an empty slot.
    /// </summary>
    public static ItemValue NoItem { get; } = new()
    {
        Item = -1,
        Serial = new SerialValue(-1),
        OptionList = Enumerable.Range(0, ItemModuleDefines.OptionCount).Select(_ => new ItemOption(0, 0)).ToList(),
        DueTo = DateTimeOffset.UnixEpoch,
        BroachState = NoBroach
    };

    public int Helper { get; init; }

    /// <summary>
    /// The items the helper wears, by slot. A slot past the end of the list, or holding <see cref="NoItem"/>, is empty.
    /// </summary>
    public IReadOnlyList<ItemValue>? Items { get; init; }

    /// <summary>
    /// The orb of a friend the helper wears, if any: the friend, the kind of orb, what it adds and until when.
    /// </summary>
    public HelperSupportInfoValue Support { get; init; }

    public bool Summoned { get; init; }

    /// <summary>
    /// The stats the helper has by itself at the level of its owner.
    /// </summary>
    public IReadOnlyList<HelperStatValue> Origin { get; init; }

    /// <summary>
    /// The stats that its items and the orb it wears add.
    /// </summary>
    public IReadOnlyList<HelperStatValue> Additional { get; init; }

    public byte Order { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Helper);

        for (var slot = 0; slot < ItemSlots; slot++)
        {
            writer.Write(Items is { } items && slot < items.Count ? items[slot] : NoItem);
        }

        writer.Write(Support.Friend);
        writer.Write(Support.SupportType);
        writer.Write(Support.Value);
        // A helper without an orb has no date: zero, not the first day of the calendar.
        writer.Write(Support.Date == default ? 0L : Support.Date.ToUnixTimeSeconds());
        writer.Write(Summoned);

        WriteStats(writer, Origin ?? []);
        WriteStats(writer, Additional ?? []);

        writer.Write(Order);
    }

    private static void WriteStats(BinaryWriter writer, IReadOnlyList<HelperStatValue> stats)
    {
        writer.Write(checked((short)stats.Count));

        foreach (var stat in stats)
        {
            stat.Write(writer);
        }
    }

}

/// <summary>
/// The values the stats of one helper add up to.
/// </summary>
public readonly record struct HelperStatUpdateValue(int Helper, IReadOnlyList<HelperStatValue> Stats) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Helper);
        writer.Write(checked((short)Stats.Count));

        foreach (var stat in Stats)
        {
            stat.Write(writer);
        }
    }
}

/// <summary>
/// A friend that offers its orb, as the list of the client shows it. The value is what the orb gives the reader, which is
/// less than the original value when the friend is far above the reader's level; the class is the friend's.
/// </summary>
public readonly record struct HelperSupportFriendValue(
    int Friend,
    byte SupportType,
    float Value,
    DateTimeOffset Date,
    float OriginalValue,
    string Name,
    byte Level,
    byte Class) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Friend);
        writer.Write(SupportType);
        writer.Write(Value);
        writer.Write(Date.ToUnixTimeSeconds());
        writer.Write(OriginalValue);
        writer.WriteUtf16UnicodeString(Name);
        writer.Write(Level);
        writer.Write(Class);
    }
}
