namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCHelper : byte
    {
        ListLoad = 0x1,
        Summon = 0x2,
        Add = 0x3,
        UserStatUpdate = 0x4,
        SupportInfo = 0x5,
        SupportRegister = 0x6,
        SupportReward = 0x7,
        SupportList = 0x8,
        SupportEquip = 0x9,
        Equip = 0x10,
        UpdateInfo = 0x11,
        ChangeOrder = 0x12,
        SyncPos = 0x13,
        AllRelease = 0x14,
        AutoSummon = 0x15,
    }
}