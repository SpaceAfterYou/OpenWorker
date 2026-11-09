using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

public readonly struct FriendRecommendValue
{
    public string Name { get; init; }
    public ActorValue Actor { get; init; }
    public byte Level { get; init; }
    public Hero Hero { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    // public bool IsLoggedIn { get; init; }
    public byte IsLoggedIn { get; init; }
}