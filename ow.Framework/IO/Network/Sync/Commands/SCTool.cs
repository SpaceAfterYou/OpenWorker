namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCTool : byte
    {
        Command = 0x1,
        DropMonster = 0x2,
        DropMaze = 0x3,
        Randombox = 0x4,
        Gacha = 0x5,
        Reload = 0x6,
        KeepAlive = 0x7,
        ClearReward = 0x8,
        Disassemble = 0x9,
        Soulstone = 0x10,
    }
}