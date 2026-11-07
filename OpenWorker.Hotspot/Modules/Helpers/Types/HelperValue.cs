using System.Text;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Helpers.Types;

/// <summary>
/// A stat of a helper and its value.
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
/// A helper as the client reads it: the helper, the three items it wears, the friend that supports it, whether it is
/// called, two lists of stats and its place in the list.
/// </summary>
public readonly struct HelperValue : IWritableData
{
    /// <summary>
    /// The number of items a helper can wear.
    /// </summary>
    public const int ItemSlots = 3;

    /// <summary>
    /// The state of the broach of an item that has none.
    /// </summary>
    private const string NoBroach = "000000000000000";

    public int Helper { get; init; }

    /// <summary>
    /// The friend that supports the helper, if any.
    /// </summary>
    public HelperSupportInfoValue Support { get; init; }

    public bool Summoned { get; init; }

    /// <summary>
    /// The stats the helper has by itself.
    /// </summary>
    public IReadOnlyList<HelperStatValue> Origin { get; init; }

    /// <summary>
    /// The stats that its items and its supporter add.
    /// </summary>
    public IReadOnlyList<HelperStatValue> Additional { get; init; }

    public byte Order { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Helper);

        // Helpers do not wear anything until they can be given items.
        for (var slot = 0; slot < ItemSlots; slot++)
        {
            WriteNoItem(writer);
        }

        writer.Write(Support.Friend);
        writer.Write(Support.SupportType);
        writer.Write(Support.Value);
        writer.Write(Support.Date.ToUnixTimeSeconds());
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

    /// <summary>
    /// An item slot with nothing in it: no item and no serial number, nothing set.
    /// </summary>
    public static void WriteNoItem(BinaryWriter writer)
    {
        writer.Write(-1);
        writer.Write(-1L);
        writer.Write((short)0);
        writer.Write((byte)0);

        for (var option = 0; option < 5; option++)
        {
            writer.Write((short)0);
            writer.Write(0);
        }

        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(0L);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write(0);
        writer.Write((short)NoBroach.Length);
        writer.Write(Encoding.ASCII.GetBytes(NoBroach));
        writer.Write((byte)0);
        writer.Write((byte)0);
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
/// A friend that offers support, as the list of the client shows it.
/// </summary>
public readonly record struct HelperSupportFriendValue(
    int Friend,
    byte SupportType,
    float Value,
    DateTimeOffset Date,
    float Rate,
    string Name,
    byte Level,
    byte Hero) : IWritableData
{
    public void Write(BinaryWriter writer)
    {
        writer.Write(Friend);
        writer.Write(SupportType);
        writer.Write(Value);
        writer.Write(Date.ToUnixTimeSeconds());
        writer.Write(Rate);
        writer.WriteUtf16UnicodeString(Name);
        writer.Write(Level);
        writer.Write(Hero);
    }
}
