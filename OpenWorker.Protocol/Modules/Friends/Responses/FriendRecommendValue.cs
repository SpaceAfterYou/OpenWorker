using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[WireMessage(WireMethods.Write)]
public readonly partial struct FriendRecommendValue
{
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
    public ActorValue Actor { get; init; }
    public byte Level { get; init; }
    [WireAs(typeof(byte))]
    public Hero Hero { get; init; }
    public short World { get; init; }
    public byte Channel { get; init; }
    // public bool IsLoggedIn { get; init; }
    public byte IsLoggedIn { get; init; }
}