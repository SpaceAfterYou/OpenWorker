using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

public readonly struct FriendRecruitValue
{
    public string Name { get; init; }
    
    /// <summary>
    /// TODO
    /// </summary>
    public int Person { get; init; }
    public byte Level { get; init; }
    public Hero Hero { get; init; }
    public byte Status { get; init; }
    public string Memo { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    public bool IsLoggedIn { get; init; }
    public ulong tLogOut { get; init; }
    public ulong tAddTime { get; init; }
}