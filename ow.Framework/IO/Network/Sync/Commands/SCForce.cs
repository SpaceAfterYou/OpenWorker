namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCForce : byte
    {
        Invite = 0x1,
        Accept = 0x2,
        ChangeMaster = 0x3,
        KickOut = 0x4,
        Leave = 0x5,
        UpdateMemberInfo = 0x6,
        Delete = 0x7,
        Cancel = 0x8,
        UpdateInfo = 0x9,
        AddMember = 0x10,
        RemoveMember = 0x11,
        UpdateMemberHp = 0x12,
        UpdateMemberSg = 0x13,
        Message = 0x20,
        MatchingEnter = 0x30,
        MatchingExit = 0x31,
        MatchingCheck = 0x32,
        MatchingReset = 0x33,
        MatchingWait = 0x34,
        MazeClear = 0x36,
        End = 0x80
    }
}