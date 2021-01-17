namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCLogin : byte
    {
        Req = 0x1,
        Result = 0x2,
        ServerListReq = 0x3,
        ServerList = 0x4,
        ServerConnectReq = 0x5,
        Logout = 0x6,
        EnterServer = 0x11,
        EnterServerReq = 0x13,
        EnterServerRes = 0x14,
        ForNhn = 0x15,
        ForSega = 0x16,
        ForSg = 0x17,
        ForGf = 0x18,
        ForTwn = 0x19,
        ForWm = 0x20,
        ForChn = 0x21,
        OptionLoad = 0x31,
        OptionUpdate = 0x32,
        MobileAuth = 0x33,
        EnterWaitCheck = 0x34,
        EnterWaitCancel = 0x35,
    }
}