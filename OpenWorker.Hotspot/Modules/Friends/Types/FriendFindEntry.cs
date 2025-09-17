namespace OpenWorker.Hotspot.Modules.Friends.Types;

public readonly record struct FriendFindEntry
{
    public required int Person { get; init; } 
    public required string Name { get; init; }
    public required byte Level { get; init; } 
    public required byte Channel { get; init; }
    public required short World { get; init; } 
    public required bool IsLoggedIn { get; init; }
}