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

public enum EquipSlotType : byte
{
    E_EQUIP_SLOT_TYPE_SOUL_WEAPON = 0x1,
    E_EQUIP_SLOT_TYPE_SUB_WEAPON = 0x2,
    E_EQUIP_SLOT_TYPE_PENDANT = 0x6F,
    E_EQUIP_SLOT_TYPE_EARING = 0x83,
    E_EQUIP_SLOT_TYPE_RING = 0x8D,
    E_EQUIP_SLOT_TYPE_HEAD_GEAR = 0x97,
    E_EQUIP_SLOT_TYPE_SHOULDER_GEAR = 0xA1,
    E_EQUIP_SLOT_TYPE_BODY_GEAR = 0xAB,
    E_EQUIP_SLOT_TYPE_LEG_GEAR = 0xB5,
    E_EQUIP_SLOT_TYPE_HAIR_ACC = 0xB,
    E_EQUIP_SLOT_TYPE_HAIR_CAP = 0xC,
    E_EQUIP_SLOT_TYPE_FACE_UPPER = 0xD,
    E_EQUIP_SLOT_TYPE_FACE_LOWER = 0xE,
    E_EQUIP_SLOT_TYPE_GLOVE = 0xF,
    E_EQUIP_SLOT_TYPE_UNDERWEAR = 0x10,
    E_EQUIP_SLOT_TYPE_COSTUME = 0x11,
    E_EQUIP_SLOT_TYPE_BACK = 0x12,
    E_EQUIP_SLOT_TYPE_SOCKS = 0x13,
    E_EQUIP_SLOT_TYPE_SHOES = 0x14,
    E_EQUIP_SLOT_TYPE_WEAPON_SKIN = 0x15,
    E_EQUIP_SLOT_TYPE_PANTS = 0x16,
    E_EQUIP_SLOT_TYPE_TAIL = 0x17,
    E_EQUIP_SLOT_TYPE_EFFECT = 0x18,
    E_EQUIP_SLOT_TYPE_BROACH_ATK = 0x61,
    E_EQUIP_SLOT_TYPE_BROACH_DEF = 0x62,
    E_EQUIP_SLOT_TYPE_BROACH_FUN = 0x63,
    E_EQUIP_SLOT_TYPE_MEMORY1 = 0x65,
    E_EQUIP_SLOT_TYPE_MEMORY2 = 0x66,
    E_EQUIP_SLOT_TYPE_MEMORY3 = 0x67,
    E_EQUIP_SLOT_TYPE_MEMORY4 = 0x68,
    E_EQUIP_SLOT_TYPE_MEMORY5 = 0x69,
    E_EQUIP_SLOT_TYPE_MEMORY6 = 0x6A,
    E_EQUIP_SLOT_TYPE_MEMORY7 = 0x6B,
    E_EQUIP_SLOT_TYPE_MEMORY8 = 0x6C,
    E_EQUIP_SLOT_TYPE_MEMORY9 = 0x6D,
    E_EQUIP_SLOT_TYPE_MEMORY10 = 0x6E,
    E_EQUIP_SLOT_TYPE_HEAR = 0xFB,
    E_EQUIP_SLOT_TYPE_HEAR_COLOR = 0xFC,
    E_EQUIP_SLOT_TYPE_SKIN_COLOR = 0xFD,
    E_EQUIP_SLOT_TYPE_EYE_COLOR = 0xFE,
    E_EQUIP_SLOT_TYPE_SKILL = 0xC7,
    E_EQUIP_SLOT_TYPE_SOCIAL = 0xCB,
    E_EQUIP_SLOT_OWN_AKASHIC_RECODE = 0xC9,
    E_EQUIP_SLOT_SKILL_AKASHIC_RECODE = 0xCA,
    E_EQUIP_SLOT_CONSUME_ITEM = 0xC8,
    E_EQUIP_SLOT_HELPER_ITEM_1 = 0xF0,
    E_EQUIP_SLOT_HELPER_ITEM_2 = 0xF1,
    E_EQUIP_SLOT_HELPER_COSTUME = 0xF2,
    E_EQUIP_SLOT_TYPE_MAX = 0xF3,
}

public enum AbilityEquipSlot : byte
{
    SoulWeapon = 0x0,
    SubWeapon = 0x1,
    Earring = 0x2,
    Pendant = 0x3,
    Ring1 = 0x4,
    Ring2 = 0x5,
    HeadGear = 0x6,
    ShoulderGear = 0x7,
    BodyGear = 0x8,
    LegGear = 0x9,
    Memory1 = 0xA,
    Memory2 = 0xB,
    Memory3 = 0xC,
    Memory4 = 0xD,
    Memory5 = 0xE,
    Memory6 = 0xF,
    Memory7 = 0x10,
    Memory8 = 0x11,
    Memory9 = 0x12,
    Memory10 = 0x13,
    Max = 0x14,
}

public readonly record struct EquipableSlot(EquipSlotType Type, Range Slot);