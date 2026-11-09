using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Events.Enums;

namespace OpenWorker.Hotspot.Modules.Events.Types;

/// <summary>
/// The attendance of a person in a table: 45 bytes.
/// </summary>
public readonly struct AttendanceInfo : IWritableData
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

    public void Write(BinaryWriter writer)
    {
        if (State.Length != Days)
        {
            throw new InvalidOperationException("The attendance has a fixed number of days.");
        }

        writer.Write(CurrentDay);

        foreach (var state in State)
        {
            writer.Write((byte)state);
        }

        writer.Write(Table);
    }
}
