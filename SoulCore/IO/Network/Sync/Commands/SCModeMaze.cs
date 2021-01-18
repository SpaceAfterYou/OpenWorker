namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCModeMaze : byte
    {
        MatchingEnter = 0x1,
        MatchingExit = 0x3,
        MatchingWait = 0x5,
        MatchingTimeInfo = 0x6,
        ScoreInfo = 0x11,
        ScoreUserPointInfo = 0x12,
        RewardInfo = 0x13,
        RoguelikeEnter = 0x20,
        RoguelikePocketList = 0x21,
        RoguelikeSelectPocket = 0x22,
        RoguelikeResult = 0x23,
        RoguelikeCurrentInfo = 0x24,
        RoguelikeShopMyInfo = 0x25,
        RoguelikeShopInfo = 0x26,
        RoguelikeShopBuy = 0x27,
        Notice = 0x30,
        End = 0x80
    }
}
