namespace OpenWorker.Hotspot.Modules.Quests.Enums;

public enum QuestConditionTarget : byte
{
    Npc = 0x0,
    Monster = 0x1,
    Pc = 0x2,
    Item = 0x3,
    Object = 0x4,
    Collision = 0x5,
    Trigger = 0x6,
    Maze = 0x7,
    MonsterGroup = 0x8,
    Sector = 0x9,
    SkillDeck = 0xA,
    Event = 0xB
}
