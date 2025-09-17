namespace OpenWorker.Hotspot.Modules.Friends.Enums;

public enum FriendResult
{
    Success = 0x0,
    Nobody = 0x1,
    AlreadyFriend = 0x2,
    AlreadyInvite = 0x3,
    WaitOver = 0x4,
    Block = 0x5,
    FriendFull = 0x6,
    BlockFull = 0x7,
    FriendCondition = 0x8,
    RefuseInvite = 0x9
}