namespace Core.Systems.NetSystem.Opcodes
{
    public enum ReceiveOpcode
    {
        KeepAlive = 0x0105,
        Heartbeat = 0x0106,

        Login = 0x0201,
        GateList = 0x0203,
        GateConnect = 0x0205,
        GateEnter = 0x0213,
        LoginOptionUpdate = 0x0232,
        EnterWaitCancel = 0x0235,

        CharacterCreate = 0x0301,
        CharacterDelete = 0x0302,
        CharacterChangeSlot = 0x0306,
        CharacterMarkFavorite = 0x030F,
        CharacterList = 0x0311,
        CharacterSelect = 0x0313,
        CharacterInfo = 0x0331,
        DistrictEnter = 0x0321,
        CharacterSpecialOptionUpdateList = 0x0347,
        CharacterTitleLoad = 0x0323,
        CharacterTitleUpdate = 0x0325,
        LogOut = 0x0360,

        OthersInfo = 0x0406,
        Disconnect = 0x0419,

        MovementMove = 0x0501,
        MovementStopBt = 0x0503,
        MovementJump = 0x0505,
        CharacterToggleWeapon = 0x0507,
        MovementLoopMotionEndBt = 0x0532,

        ChatReceiveMessage = 0x0701,

        StorageInventoryItemsInfo = 0x0801,
        StorageItemCombine = 0x0802,
        StorageItemMove = 0x0803,
        StorageItemDivide = 0x0804,
        StorageItemBreak = 0x0805,
        StorageBankItemsInfo = 0x0810,
        StorageItemUse = 0x0811,

        ShopCashBuyCountLoad = 0x0930,

        PartyInvite = 0x1201,
        PartyAccept = 0x1202,
        PartyLeave = 0x1205,

        PostSendList = 0x2001,
        PostRecvList = 0x2002,
        PostSend = 0x2003,
        PostRead = 0x2004,
        PostReceipt = 0x2005,
        PostRecvDelete = 0x2007,
        PostSendDelete = 0x2006,
        PostSendBack = 0x2008,
        PostSave = 0x2012,
        PostSaveList = 0x2013,
        PostAccountList = 0x2014,
        PostAccountRead = 0x2016,
        PostAccountReceipt = 0x2017,
        PostReceiptAll = 0x2020,
        PostListRefresh = 0x2021,

        GestureDo = 0x2301,
        GestureUpdateSlots = 0x2303,

        ChannelInfo = 0xF101,
        ChannelSwitch = 0xF102,
    }
}
