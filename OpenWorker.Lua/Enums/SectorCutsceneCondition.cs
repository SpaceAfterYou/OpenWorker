namespace OpenWorker.Lua;

public enum SectorCutsceneCondition : byte
{
    None = 0x0,
    EnterSector = 0x1,
    DieMonster = 0x2,
    HaveCondition = 0x3,
    HaveEpisode = 0x4,
    CompleteCondition = 0x5,
    CompleteEpisode = 0x6,
}