namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCVaccum : byte
    {
        ClickStart = 0x1,
        ClickCancel = 0x2,
        ClickComplete = 0x3,
        In = 0x11,
        Out = 0x12,
        Infos = 0x13,
    }
}