namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCServerWorldMode : byte
    {
        Start = 0x1,
        Update = 0x2,
        Clear = 0x3,
        Finish = 0x4,
        Sync = 0x5,
        Command = 0x6,
        Complete = 0x7,
        Fail = 0x8,
        Enter = 0x9,
    }
}