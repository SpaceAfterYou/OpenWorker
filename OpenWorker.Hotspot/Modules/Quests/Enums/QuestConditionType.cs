namespace OpenWorker.Hotspot.Modules.Quests.Enums;

public enum QuestConditionType : byte
{
    Talk = 0x0,
    Move = 0x1,
    Trigger = 0x2,
    Hunt = 0x3,
    Collect = 0x4,
    Deliver = 0x5,
    Protect = 0x6,
    Survial = 0x7,
    Trace = 0x8,
    Guard = 0x9,
    Clear = 0xA,
    Buy = 0xB,
    Sell = 0xC,
    Make = 0xD,
    Upgrade = 0xE,
    Skill = 0xF,
    Disassemble = 0x10,
    AkashicMake = 0x11,
    Cultivation = 0x12,
    Harvest = 0x13,
    HuntAll = 0x14,
    End = 0x15
}