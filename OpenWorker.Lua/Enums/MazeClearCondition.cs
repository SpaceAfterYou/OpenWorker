namespace OpenWorker.Lua.Enums;

public enum MazeClearCondition : byte
{
    None = 0x0,
    HuntMonster = 0x1,
    CompleteQuest = 0x2,
    SectorClear = 0x3,
    DimensionShutterTimeOver = 0x4,
    DimensionShutterPoint = 0x5,
    TimeStep = 0x6,
}