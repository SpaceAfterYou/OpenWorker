namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCSystem : byte
    {
        KeyOptionUpdate = 0x1,
        OptionUpdate = 0x2,
        Xigncode = 0x3,
        XigncodeError = 0x4,
        KeepAlive = 0x5,
        Ping = 0x6,
        ServerOptionUpdate = 0x7,
        ClientLog = 0x8,
        Event = 0x9,
        Indulgence = 0x10,
        SgTokenUpdate = 0x11,
        GameGuardAuth = 0x12,
        GameGuardError = 0x13,
        TickLog = 0x21,
    }
}