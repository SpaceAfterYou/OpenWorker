namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCDailyMission : byte
    {
        List = 0x1,
        Accept = 0x2,
        Update = 0x3,
        Helper = 0x4,
        InitAll = 0x5,
    }
}