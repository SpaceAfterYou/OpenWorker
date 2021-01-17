namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCServerForce : byte
    {
        Create = 0x1,
        JoinMember = 0x2,
        LeaveMember = 0x3,
        ChangeMaster = 0x4,
        UpdateMember = 0x5,
        Delete = 0x6,
        EnterMaze = 0x8,
        UpdateInfo = 0x9,
        EnterServer = 0xA,
        Invite = 0xb,
        Accept = 0xC,
        Cancel = 0xD,
        Message = 0x10,
        MatchingEnter = 0x13,
        MatchingExit = 0x14,
        MatchingCheck = 0x15,
        MatchingReset = 0x16,
        MatchingWait = 0x17,
        MatchingMaze = 0x18,
        MazeClear = 0x19,
        Info = 0x1A,
        NameChange = 0x20,
        ChangeMazeOpenCheck = 0x21,
    }
}
