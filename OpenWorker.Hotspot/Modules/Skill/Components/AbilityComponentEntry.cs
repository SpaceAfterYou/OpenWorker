namespace OpenWorker.Hotspot.Modules.Skill.Components;

public readonly record struct AbilityComponentEntry
{
    public required int Current { get; init; }
    public required int Max { get; init; }
}