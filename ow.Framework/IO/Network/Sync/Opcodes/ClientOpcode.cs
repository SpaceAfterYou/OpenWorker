﻿namespace ow.Framework.IO.Network.Sync.Opcodes
{
    public enum ClientOpcode : ushort
    {
        OptionUpdate = 0x0102,
        KeepAlive = 0x0105,
        Heartbeat = 0x0106,
        SystemServerOptionUpdate = 0x0107,
        SystemTutorialStage = 0x0114,
        TradePassword = 0x0118,

        LoginResult = 0x0202,
        GateList = 0x0204,
        GateConnect = 0x0211,
        GateEnter = 0x0214,
        OptionLoad = 0x0231,

        CharacterDelete = 0x0302,
        CharacterMarkAsFavorite = 0x030F,
        CharacterList = 0x0312,
        CharacterSelect = 0x0314,
        WorldEnter = 0x0322,
        CharacterTitleLoad = 0x0323,
        CharacterTitleUpdate = 0x0325,
        CharacterChangeBackground = 0x032C,
        CharacterDbLoadSync = 0x0330,
        CharacterInfo = 0x0332,
        CharacterStatsUpdate = 0x0334,
        CharacterLevelSet = 0x0336,
        ChatReceivedExp = 0x0337,
        CharacterSpecialOptionUpdateList = 0x0347,
        CharacterOriginStatsUpdate = 0x0351,

        // Undefined4 = 0x0354,
        LogOut = 0x0360,

        LoadDistrictState = 0x0361,
        MazeEnterLimitCountLoad = 0x0362,
        CharacterEnergyUpdate = 0x0364,
        NpcCreditUpdate = 0x0366,
        AchieveUpdate = 0x0371,
        CharacterProfileInfo = 0x0377,

        WorldJoin = 0x0402,
        CurrentDate = 0x0403,
        WorldVersion = 0x0404,
        Warp = 0x0408,
        WarpUpdate = 0x0409,
        WarpMaze = 0x040E,
        CharacterInInfo = 0x0411,
        CharacterOutInfo = 0x0412,
        CharacterOtherInfos = 0x0421,
        NpcOtherInfos = 0x0422,
        MazeEnterLimitCountReset = 0x0444,

        MovementMove = 0x0502,
        MovementJump = 0x0506,
        MovementStop = 0x0504,
        CharacterToggleWeapon = 0x0508,

        //MovementUndefined1 = 0x0511,
        MovementLoopMotionEnd = 0x0533,

        MovementMoveAttached = 0x0534,
        MovementMoveAttachedEnd = 0x0535,

        SkillUse = 0x0609,
        SkillUpgrade = 0x0671,
        //Undefined3 = 0x0673,

        // Undefined0x0705 = 0x0705,
        ChatMessage = 0x0701,

        StorageInventoryItemsInfo = 0x0801,
        StorageItemCombine = 0x0802,
        StorageItemMove = 0x0803,
        StorageItemDivide = 0x0804,
        StorageItemBreak = 0x0805,
        StorageItemCreate = 0x0806,
        StorageItemMoveBroadcast = 0x080D,
        StorageOpenSlotInfo = 0x080E,
        StorageUpdateSlotInfo = 0x080F,
        StorageBankItemsInfo = 0x0810,
        StorageItemUse = 0x0811,
        StorageItemUpdateCount = 0x0812,
        SetLimitEffect = 0x0814,
        ItemUpdate = 0x0815,
        CharacterSetMoney = 0x0820,
        BankSetMoney = 0x0821,
        Unknown0x0825 = 0x0825,
        ItemLoadQuickslot = 0x0826,
        ItemUpdateQuickslotCard = 0x0827,
        ItemUpdateQuickslotItem = 0x0828,
        CharacterSetSoulPoints = 0x0831,
        CharacterSetEther = 0x0832,
        StorageCashUpdate = 0x0833,
        ItemUpdateFriendPoint = 0x0834,
        ItemMazeRevardItem = 0x0847,
        Unknown0x0848 = 0x0848,
        Unknown0x0849 = 0x0849,
        CubeInvenAddItem = 0x084A,
        ItemAppearanceLoad = 0x0850,
        ItemAppearanceEquip = 0x0852,
        ItemNameChange = 0x0853,
        ItemSocketLoad = 0x0855,
        ItemBroachLoad = 0x0856,
        // Undefined1 = 0x0866,
        // Undefined2 = 0x0868,

        ShopCashBuyCountLoad = 0x0930,

        MazeClearInfo = 0x1164,
        MazeEscortStatus = 0x1167,

        PartyInvite = 0x1201,

        PartyChangeMaster = 0x1203,
        PartyLeave = 0x1205,
        PartyDelete = 0x1207,
        PartyInfo = 0x1209,

        QuestCompleteList = 0x1501,
        QuestList = 0x1502,
        QuestAccept = 0x1503,
        QuestEpisodeComplete = 0x1505,
        QuestUpdate = 0x1507,

        PartyRecruitMyApplyList = 0x1237,

        CharacterSuperArmorGage = 0x1753,

        StorageItemUpgrade = 0x1802,

        PostSendList = 0x2001,
        PostRecvList = 0x2002,
        PostSend = 0x2003,
        PostRead = 0x2004,
        PostReceipt = 0x2005,
        PostSendDelete = 0x2006,
        PostRecvDelete = 0x2007,
        PostSendBack = 0x2008,
        PostRecv = 0x2009,
        PostInfo = 0x2010,
        PostSave = 0x2012,
        PostSaveList = 0x2013,
        PostAccountList = 0x2014,
        PostAccountRecv = 0x2015,
        PostAccountRead = 0x2016,
        PostAccountReceipt = 0x2017,
        PostAccountDel = 0x2018,
        PostDeleteAll = 0x2019,
        PostReceiptAll = 0x2020,

        GestureDo = 0x2301,
        GestureLoad = 0x2302,
        GestureUpdateSlots = 0x2303,

        InfiniteTowerLoadInfo = 0x2801,
        InfiniteTowerReward = 0x2804,

        BoosterListLoad = 0x2901,
        BoosterAdd = 0x2902,
        BoosterRemove = 0x2903,
        BoosterClear = 0x2904,

        AttendanceLoad = 0x2A01,
        AttendanceReward = 0x2A02,
        AttendanceContinueReward = 0x2A03,
        AttendancePlayTimeReward = 0x2A04,
        AttendancePlayTimeInit = 0x2A05,
        EventDayEventBoosterList = 0x2A20,
        EventUseCouponCode = 0x2A22,
        RouletteMyInfo = 0x2A28,
        RouletteUse = 0x2A29,
        BattlePassLoad = 0x2A30,

        ExchangeSearch = 0x2B01,
        ExchangePriceHistory = 0x2B02,
        ExchangeInterestList = 0x2B03,
        ExchangeInterestItem = 0x2B04,
        ExchangeSellRegister = 0x2B05,
        ExchangeItemBuy = 0x2B06,
        ExchangeItemRecalc = 0x2B07,
        ExchangeMyList = 0x2B08,

        // BattleArenaUndefined1 = 0x3306,

        ChannelInfo = 0xF101,
        ChannelSwitch = 0xF102,

        CharacterLeagueMemberUpdate = 0x2222,
    }
}