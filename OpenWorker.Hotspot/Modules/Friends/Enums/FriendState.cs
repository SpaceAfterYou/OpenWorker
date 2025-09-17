namespace OpenWorker.Hotspot.Modules.Friends.Enums;

public enum FriendState : byte
{
    None = 0x0,
    Friend = 0x1,
    Wait = 0x2,
    Invite = 0x3,
    Suggested = 0x64,
    Block = 0x65
}