namespace SoulCore.IO.Network.Sync.Commands
{
    public enum SCEvent : byte
    {
        AttendanceLoad = 0x1,
        AttendanceReward = 0x2,
        AttendanceContinueReward = 0x3,
        AttendancePlayTimeReward = 0x4,
        AttendancePlayTimeInit = 0x5,
        LuaValues = 0x10,
        DayEventBoosterList = 0x20,
        UseCouponCode = 0x21,
        WorldEventInfo = 0x22,
        WorldEventRegister = 0x23,
        WorldEventReward = 0x24,
        WorldEventDailyReward = 0x25,
        NetCafeItemBuy = 0x2A,
        NetCafeItemDelete = 0x2B,
        NetCafeMissionInfo = 0x2C,
        RouletteInfo = 0x27,
        RouletteMyInfo = 0x28,
        RouletteUse = 0x29,
        BattlePassLoad  = 0x30,
        BattlePassUpdate  = 0x31,
        BattlePassReward = 0x32,
        BattlePassUpgrade  = 0x33,
    }
}
