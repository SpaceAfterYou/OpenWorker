using OpenWorker.Hotspot.Modules.Friends.Enums;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Types;

[WireMessage(WireMethods.Write)]
public readonly partial struct FriendValue
{
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
    public int Friend { get; init; }
    public byte Level { get; init; }
    public byte Class { get; init; }
    public FriendState State { get; init; }
    public CommunityState CommunityState { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Note { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    public ulong FriendPoint { get; init; }
    public bool Login { get; init; }
    public ulong LogOut { get; init; }
    public ulong Remain { get; init; }
}