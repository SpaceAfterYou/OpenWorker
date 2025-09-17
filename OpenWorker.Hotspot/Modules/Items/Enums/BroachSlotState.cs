namespace OpenWorker.Hotspot.Modules.Items.Enums;

public enum BroachSlotState : byte
{
    Limit = (byte)'0',
    None = (byte)'1',
    Block = (byte)'2'
}

internal enum E_BROACH_CLASSIFY_INDEX
{
    E_BROACH_CLASSIFY_INDEX_1 = 0x271C9,
    E_BROACH_CLASSIFY_INDEX_2 = 0x271CA,
    E_BROACH_CLASSIFY_INDEX_3 = 0x271CB,
    E_BROACH_CLASSIFY_INDEX_4 = 0x271CC,
    E_BROACH_CLASSIFY_INDEX_5 = 0x271CD,
    E_BROACH_CLASSIFY_INDEX_6 = 0x271CE
}

internal enum E_EQUIP_REG_CATEGORY : byte
{
    E_EQUIP_REG_CATEGORY_WEAPON = 0x1,
    E_EQUIP_REG_CATEGORY_GEAR = 0x2,
    E_EQUIP_REG_CATEGORY_SOULSTONE = 0x3
}