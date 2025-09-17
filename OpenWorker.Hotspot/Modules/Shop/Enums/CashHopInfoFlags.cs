namespace OpenWorker.Hotspot.Modules.Shop.Enums;

[Flags]
public enum CashHopInfoFlags
{
    IconHot = 0x1,
    IconNew = 0x2,
    IconSale = 0x4,
    IconGift = 0x8,
    BuyLimit = 0x10,
    BuyCount = 0x20
}