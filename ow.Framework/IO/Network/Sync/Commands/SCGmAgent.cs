namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCGmAgent : byte
    {
        UserKick = 0x1,
        NoticeMsg = 0x2,
        ShutDown = 0x3,
        TimeEvent = 0x4,
        ValueEvent = 0x5,
        ServerOption = 0x6,
        CashShopBanner = 0x7,
    }
}