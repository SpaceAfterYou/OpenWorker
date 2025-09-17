namespace OpenWorker.Hotspot.Modules.League.Components;

public readonly record struct LeagueComponentCard
{
    public required short Emblem { get; init; }
    public required short Border { get; init; }
}