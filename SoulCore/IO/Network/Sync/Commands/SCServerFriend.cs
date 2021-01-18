namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCServerFriend : byte
    {
        Load = 0x1,
        LoadBlocklist = 0x2,
        Invite = 0x3,
        Accept = 0x4,
        Delete = 0x5,
        Add = 0x6,
        BlockAdd = 0x7,
        BlockDelete = 0x8,
        Recommand = 0x11,
        UpdateLeaderCard = 0x12,
        RecruitList = 0x15,
        RecruitAdd = 0x16,
        RecruitDelete = 0x17,
        RecruitInfo = 0x18,
        UpdateInfo = 0x20,
        CommunityUpdate = 0x21,
        Find = 0x22,
        CheckDailyMissionReq = 0x25,
        CheckDailyMissionRes = 0x26,
        HelperSupportInfo = 0x27,
        HelperSupportRegister = 0x28,
        HelperSupportReward = 0x29,
        HelperSupportList = 0x30,
        HelperSupportEquip = 0x31,
        HelperSupportEquipReward = 0x32,
        HelperEquip = 0x33,
        ServerLoad = 0x34,
    }
}
