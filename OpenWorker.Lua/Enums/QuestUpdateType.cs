namespace OpenWorker.Lua;

public enum QuestUpdateType : byte
{
    AcceptQuest = 0x0,
    CompleteQuest = 0x1,
    CompleteCondition = 0x2,
}