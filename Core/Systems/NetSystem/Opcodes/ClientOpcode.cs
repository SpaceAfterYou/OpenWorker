namespace Core.Systems.NetSystem.Opcodes
{
    public enum ClientOpcode : ushort
    {
        OptionUpdate = 0x0201, /// 0x0102
        KeepAlive = 0x0501, /// 0x0105
        Heartbeat = 0x0601, /// 0x0106
        SystemServerOptionUpdate = 0x0701, /// 0x0107
        SystemTutorialStage = 0x1401, /// 0x0114
        TradePassword = 0x1801, /// 0x0118

        LoginResult = 0x0202, /// 0x0202
        GateList = 0x0402, /// 0x0204
        GateConnect = 0x1102, /// 0x0211
        GateEnter = 0x1402, /// 0x0214
        OptionLoad = 0x3102, /// 0x0231

        CharacterDelete = 0x0203, /// 0x0302
        CharactersList = 0x1203, /// 0x0312
        CharacterSelect = 0x1403, /// 0x0314
        WorldEnter = 0x2203, /// 0x0322
        CharacterTitleLoad = 0x2303, /// 0x0323
        CharacterTitleUpdate = 0x2503, /// 0x0325
        CharacterDbLoadSync = 0x3003, /// 0x0330
        CharacterInfo = 0x3203, /// 0x0332
        CharacterStatsUpdate = 0x3403, /// 0x0334
        CharacterLevelUp = 0x3603, /// 0x0336
        CharacterSpecialOptionUpdateList = 0x4703, /// 0x0347
        CharacterOriginStatsUpdate = 0x5103, /// 0x0351
        Undefined4 = 0x5403, /// 0x0354
        LogOut = 0x6003, /// 0x0360
        LoadDistrictState = 0x6103, /// 0x0361
        CharacterEnergyUpdate = 0x6403, /// 0x0364
        NpcCreditUpdate = 0x6603, /// 0x0366
        AchieveUpdate = 0x7103, /// 0x0371
        CharacterProfileInfo = 0x7703, /// 0x0377

        WorldJoin = 0x0204, /// 0x0402
        CurrentDate = 0x0304, /// 0x0403
        WorldVersion = 0x0404, /// 0x0404
        Warp = 0x0804, /// 0x0408
        WarpUpdate = 0x0904, /// 0x0409
        WarpMaze = 0x0E04, /// 0x040E
        CharacterInInfo = 0x1104, /// 0x0411
        CharacterOutInfo = 0x1204, /// 0x0412
        CharacterOtherInfos = 0x2104, /// 0x0421
        NpcOtherInfos = 0x2204, /// 0x0422

        MovementMove = 0x0205, /// 0x0502
        MovementJump = 0x0605, /// 0x0506
        MovementStop = 0x0405, /// 0x0504
        CharacterToggleWeapon = 0x0805, /// 0x0508
        MovementLoopMotionEnd = 0x3305, /// 0x0533
        MovementMoveAttached = 0x3405, /// 0x0534
        MovementMoveAttachedEnd = 0x3505, /// 0x0535

        Undefined0x0705 = 0x0507, /// 0x0705
        ChatMessage = 0x0107, /// 0x0701

        StorageInventoryItemsInfo = 0x0108, /// 0x0801
        StorageItemCombine = 0x0208, /// 0x0802
        StorageItemMove = 0x0308, /// 0x0803
        StorageItemDivide = 0x0408, /// 0x0804
        StorageItemBreak = 0x0508, /// 0x0805
        StorageItemCreate = 0x0608, /// 0x0806
        StorageItemMoveBroadcast = 0x0D08, /// 0x080D
        StorageOpenSlotInfo = 0x0E08, /// 0x080E
        StorageUpdateSlotInfo = 0x0F08, /// 0x080F
        StorageBankItemsInfo = 0x1008, /// 0x0810
        StorageItemUse = 0x1108, /// 0x0811
        StorageItemUpdateCount = 0x1208, /// 0x0812
        SetLimitEffect = 0x1408, /// 0x0814
        ItemUpdate = 0x1508, /// 0x0815
        CharacterSetMoney = 0x2008, /// 0x0820
        BankSetMoney = 0x2108, /// 0x0821
        Unknown0x0825 = 0x2508, /// 0x0825
        ItemLoadQuickslot = 0x2608, /// 0x0826
        ItemUpdateQuickslotCard = 0x2708, /// 0x0827
        ItemUpdateQuickslotItem = 0x2808, /// 0x0828
        CharacterSetSoulPoints = 0x3108, /// 0x0831
        CharacterSetEther = 0x3208, /// 0x0832
        StorageCashUpdate = 0x3308, /// 0x0833
        ItemUpdateFriendPoint = 0x3408, /// 0x0834
        ItemMazeRevardItem = 0x4708, /// 0x0847
        Unknown0x0848 = 0x4808, /// 0x0848
        Unknown0x0849 = 0x4908, /// 0x0849
        CubeInvenAddItem = 0x4A08, /// 0x084A
        ItemAppearanceLoad = 0x5008, /// 0x0850
        ItemAppearanceEquip = 0x5208, /// 0x0852
        ItemNameChange = 0x5308, /// 0x0853
        ItemSocketLoad = 0x5508, /// 0x0855
        ItemBroachLoad = 0x5608, /// 0x0856

        ShopCashBuyCountLoad = 0x3009, // 0x0930

        MazeClearInfo = 0x6411, /// 0x1164
        MazeEscortStatus = 0x6711, /// 0x1167

                                   ///
        PartyInvite = 0x0112, /// 0x1201

        PartyChangeMaster = 0x0312, /// 0x1203
        PartyLeave = 0x0512, /// 0x1205
        PartyDelete = 0x0712, /// 0x1207
        PartyInfo = 0x0912, /// 0x1209

        QuestsCompleteList = 0x0115, /// 0x1501 receive_eSUB_CMD_QUEST_COMPLETE_LIST
        QuestsList = 0x0215, /// 0x1502 receive_eSUB_CMD_QUEST_LIST
        QuestsAccept = 0x0315, /// 0x1503 receive_eSUB_CMD_QUEST_ACCEPT

        PartyRecruitMyApplyList = 0x3712, /// 0x1237

        CharacterSuperArmorGage = 0x5317, /// 0x1753

        StorageItemUpgrade = 0x0218, /// 0x1802

        PostSendList = 0x0120, /// 0x2001
        PostRecvList = 0x0220, /// 0x2002
        PostSend = 0x0320, /// 0x2003
        PostRead = 0x0420, /// 0x2004
        PostReceipt = 0x0520, /// 0x2005
        PostSendDelete = 0x0620, /// 0x2006
        PostRecvDelete = 0x0720, /// 0x2007
        PostSendBack = 0x0820, /// 0x2008
        PostRecv = 0x0920, /// 0x2009
        PostInfo = 0x1020, /// 0x2010
        PostSave = 0x1220, /// 0x2012
        PostSaveList = 0x1320, /// 0x2013
        PostAccountList = 0x1420, /// 0x2014
        PostAccountRecv = 0x1520, /// 0x2015
        PostAccountRead = 0x1620, /// 0x2016
        PostAccountReceipt = 0x1720, /// 0x2017
        PostAccountDel = 0x1820, /// 0x2018
        PostDeleteAll = 0x1920, /// 0x2019
        PostReceiptAll = 0x2020, /// 0x2020

        GestureDo = 0x0123, /// 0x2301
        GestureLoad = 0x0223, /// 0x2302
        GestureUpdateSlots = 0x0323, /// 0x2303

        InfiniteTowerLoadInfo = 0x0128, /// 0x2801
        InfiniteTowerReward = 0x0428, /// 0x2804

        BoosterListLoad = 0x0129, /// 0x2901
        BoosterAdd = 0x0229, /// 0x2902
        BoosterRemove = 0x0329, /// 0x2903
        BoosterClear = 0x0429, /// 0x2904

        AttendanceLoad = 0x012A, /// 0x2A01
        AttendanceReward = 0x022A, /// 0x2A02
        AttendanceContinueReward = 0x032A, /// 0x2A03
        AttendancePlayTimeReward = 0x042A, /// 0x2A04
        AttendancePlayTimeInit = 0x052A, /// 0x2A05
        EventDayEventBoosterList = 0x202A, /// 0x2A20
        EventUseCouponCode = 0x222A, /// 0x2A22

        ExchangeSearch = 0x012B, /// 0x2B01
        ExchangePriceHistory = 0x022B, /// 0x2B02
        ExchangeInterestList = 0x032B, /// 0x2B03
        ExchangeInterestItem = 0x042B, /// 0x2B04
        ExchangeSellRegister = 0x052B, /// 0x2B05
        ExchangeItemBuy = 0x062B, /// 0x2B06
        ExchangeItemRecalc = 0x072B, /// 0x2B07
        ExchangeMyList = 0x082B, /// 0x2B08

        BattleArenaUndefined1 = 0x0633, /// 0x3306

        ChannelInfo = 0x01F1, /// 0xF101
        ChannelSwitch = 0x02F1, /// 0xF102

        /// <summary>
        /// waaaaaa
        /// </summary>
        CharacterLeagueMemberUpdate = 0x2222, /// 0x2222

        ClientGestureCancel = 0x3305, /// 0x0533
        Undefined1 = 0x6608, /// 0x0866
        Undefined2 = 0x6808, /// 0x0868
        Undefined3 = 0x7306, /// 0x0673
        ChatReceivedExp = 0x3703, /// 0x0337
        MazeEnterLimitCountLoad = 0x6203, /// 0x0362
        MazeEnterLimitCountReset = 0x4404, /// 0x0444
        MovementUndefined1 = 0x1105, /// 0x0511
        QuestAccept = 0x0315, /// 0x1503
        QuestEpisodeComplete = 0x0515, /// 0x1505
        QuestUpdate = 0x0715, /// 0x1507
        QuickSlotItem = 0x2808, /// 0x0828
        RouletteMyInfo = 0x282A, /// 0x2A28
        RouletteUse = 0x292A, /// 0x2A29
        SkillUpgrade = 0x7106, /// 0x0671
        SkillUse = 0x0906, /// 0x0609
    }
}