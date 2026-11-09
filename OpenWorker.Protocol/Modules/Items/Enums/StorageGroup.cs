namespace OpenWorker.Hotspot.Modules.Items.Enums;

public enum StorageGroup : byte
{
    None = 0xFF,
    
    /// <summary>
    ///    Clothes with broach
    /// </summary>
    ShapeEquip = 0x0,
    
    /// <summary>
    ///    Gear
    /// </summary>
    AbilityEquip = 0x1,
    
    Common = 0x2,
    LookEquip = 0x3,
    Costume = 0x4,
    CommonStorage = 0x5,
    CostumeStorage = 0x6,
    RepurchaseList = 0xA,

    /// <summary>
    ///    Room (My Room)
    /// </summary>
    Cube = 0xB,

    Appearance = 0xC,
    Cash = 0xD,
    CashStorage = 0xE,
    League = 0xF,
    AccountCommonStorage = 0x10,
    AccountFashionStorage = 0x11,
    AccountCashStorage = 0x12,
    // Max = 0x13,
    Post = 0x64,
    ExchangeSell = 0x66,
    HelperEquip = 0x69
}