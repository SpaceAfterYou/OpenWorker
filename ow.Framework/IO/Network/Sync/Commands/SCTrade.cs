namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCTrade : byte
    {
        Req = 0x1,
        Accept = 0x2,
        UpdateItem = 0x3,
        UpdateMoney = 0x4,
        Check = 0x5,
        Confirm = 0x6,
        Result = 0x7,
        Cancel = 0x8,
        PrivateShopStart = 0x9,
        PrivateShopItem = 0xA,
        PrivateShopState = 0xb,
        PrivateShopBuy = 0xC,
        PrivateShopSell = 0xD,
        PrivateShopSelect = 0xE,
        PrivateShopName = 0xF,
    }
}
