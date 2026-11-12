using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

/// <summary>
/// A stat and its value. The monster info of the world sends it as the stat in a byte, then the value; the updates of the
/// stats of a monster and of the base stats of a player send the value first and the stat in a short
/// (<see cref="WriteValueFirst"/>).
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial struct StatKeyValuePair
{
    public required STAT_TYPE byIndex { get; init; }
    public required float statValue { get; init; }

    /// <summary>
    /// A byte count, then each stat as its value and the stat in a short: the layout of the stat updates.
    /// </summary>
    public static void WriteValueFirst(ref PacketWriter writer, ReadOnlySpan<StatKeyValuePair> stats)
    {
        writer.WriteLength(WireLength.Byte, stats.Length);

        foreach (var stat in stats)
        {
            writer.WriteSingle(stat.statValue);
            writer.WriteInt16((short)stat.byIndex);
        }
    }
}
