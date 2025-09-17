namespace OpenWorker.Hotspot.Modules.Skill.Components;

public readonly record struct SpeedComponentEntry
{
    public required float Move { get; init; }
    public required float Attack { get; init; }
}