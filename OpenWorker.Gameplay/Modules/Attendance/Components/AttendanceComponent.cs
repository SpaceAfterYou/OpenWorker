using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Attendance.Components;

/// <summary>
/// The attendance of the person, null until it is loaded.
/// </summary>
[EntityComponent(EntityComponentService.District)]
public struct AttendanceComponent
{
    public int Person;

    public AttendanceBook? Book;

    /// <summary>
    /// Whether the client has been told about the attendance in this connection.
    /// </summary>
    public bool Sent;

    /// <summary>
    /// When the time played was counted last.
    /// </summary>
    public DateTimeOffset LastTick;
}
