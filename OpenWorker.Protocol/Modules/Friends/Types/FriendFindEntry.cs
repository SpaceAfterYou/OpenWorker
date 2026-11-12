using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Friends.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct FriendFindEntry
{
    public required int Person { get; init; } 
    [WireString(WireStringFormat.Utf16Unicode)]
    public required string Name { get; init; }
    public required byte Level { get; init; } 
    public required byte Channel { get; init; }
    public required short World { get; init; } 
    public required bool IsLoggedIn { get; init; }
}