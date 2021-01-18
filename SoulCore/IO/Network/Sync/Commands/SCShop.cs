namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCShop : byte
    {
        Buy = 0x1,
        Sell = 0x2,
        RepurchaserList = 0x3,
        Repurchaser = 0x4,
        RepurchaserAdd = 0x5,
        RepurchaserDelete = 0x6,
        ItemLoad = 0x10,
        ItemUpdate = 0x11,
        ItemInit = 0x12,
        CashLoad = 0x20,
        CashBuy = 0x21,
        CashSetLoad = 0x22,
        CashSet = 0x23,
        CashSetDel = 0x24,
        CashGift = 0x25,
        Gacha = 0x26,
        GachaRes = 0x27,
        BannerLoad = 0x28,
        CashTabLoad = 0x29,
        CashUpdate = 0x2A,
        CashTabUpdate = 0x2B,
        CashBuyCountLoad = 0x30,
        CashBuyCountUpdate = 0x31,
        GetUrl = 0x32,
        CashClose = 0x33,
        AccountItemUpdate = 0x34,
    }
}
