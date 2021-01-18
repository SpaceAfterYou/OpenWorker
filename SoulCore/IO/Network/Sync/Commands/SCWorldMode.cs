namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCWorldMode : byte
    {
        Start = 0x1,
        Update = 0x2,
        Finish = 0x3,
        Rank = 0x4,
        Info = 0x5,
    }
}
