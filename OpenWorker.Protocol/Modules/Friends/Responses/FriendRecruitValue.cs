using OpenWorker.Domain.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[WireMessage(WireMethods.Write)]
public readonly partial struct FriendRecruitValue
{
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; }
    
    /// <summary>
    /// TODO
    /// </summary>
    public int Person { get; init; }
    public byte Level { get; init; }
    [WireAs(typeof(byte))]
    public Hero Hero { get; init; }
    public byte Status { get; init; }
    [WireString(WireStringFormat.Utf16Unicode)]
    public string Memo { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    public bool IsLoggedIn { get; init; }
    public ulong tLogOut { get; init; }
    public ulong tAddTime { get; init; }
}