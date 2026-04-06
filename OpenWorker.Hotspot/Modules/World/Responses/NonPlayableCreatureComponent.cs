namespace OpenWorker.Hotspot.Modules.World.Responses;

public readonly record struct NonPlayableCreatureComponent
{
    public int Waypoint { get; init; }
    public int Sector { get; init; }
    public int SpawnBox { get; init; }
}