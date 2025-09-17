namespace OpenWorker.Hotspot.Modules.DailyMissions.Enums;

public enum DailyMissionState : byte
{
    None = 0x0,
    Accept = 0x1,
    Fail = 0x2,
    Success = 0x3,
    Max = 0x4
}