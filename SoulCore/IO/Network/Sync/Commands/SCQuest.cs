namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCQuest
    {
        CompleteList = 0x1,
        List = 0x2,
        Accept = 0x3,
        OrderComplete = 0x4,
        EpisodeComplete = 0x5,
        GiveUp = 0x6,
        Update = 0x7,
        EventUpdate = 0x8,
        Helper = 0x9,
        Reset = 0x10,
        Fail = 0x11,
        EpisodeReset = 0x12,
        RepeatReset = 0x13,
    }
}
