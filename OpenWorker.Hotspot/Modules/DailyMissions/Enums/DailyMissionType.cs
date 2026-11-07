namespace OpenWorker.Hotspot.Modules.DailyMissions.Enums;

public enum DailyMissionType : byte
{
    None = 0x0,

    /// <summary>
    /// Drawn again every day.
    /// </summary>
    Special = 0x1,

    /// <summary>
    /// Open for a part of every day.
    /// </summary>
    Guerrilla = 0x2,

    /// <summary>
    /// Open between two dates.
    /// </summary>
    Event = 0x3
}
