using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;

namespace OpenWorker.Hotspot.Modules.Friends.Types;

public readonly struct FriendValue
{
    public string Name { get; init; }
    public int Friend { get; init; }
    public byte Level { get; init; }
    public byte Class { get; init; }
    public FriendState State { get; init; }
    public CommunityState CommunityState { get; init; }
    public string Note { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    public ulong FriendPoint { get; init; }
    public bool Login { get; init; }
    public ulong LogOut { get; init; }
    public ulong Remain { get; init; }
}