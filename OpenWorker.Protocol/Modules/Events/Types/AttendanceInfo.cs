using OpenWorker.Hotspot.Modules.Events.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Types;

/// <summary>
/// The attendance of a person in a table: 45 bytes. Written by hand because a table of another number of days is refused,
/// not padded or cut.
/// </summary>
public readonly struct AttendanceInfo : IWireWritable
{
    public const int Days = 40;

    /// <summary>
    /// How many days of the table the person has come, which is the day of today. Zero tells the client to ignore the message.
    /// </summary>
    public required byte CurrentDay { get; init; }

    /// <summary>
    /// A character for each day of the table, <see cref="Days"/> of them.
    /// </summary>
    public required AttendanceState[] State { get; init; }

    public required int Table { get; init; }

    public void Write(ref PacketWriter writer)
    {
        if (State.Length != Days)
        {
            throw new InvalidOperationException("The attendance has a fixed number of days.");
        }

        writer.WriteByte(CurrentDay);

        foreach (var state in State)
        {
            writer.WriteByte((byte)state);
        }

        writer.WriteInt32(Table);
    }
}
