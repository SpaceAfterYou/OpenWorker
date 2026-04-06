namespace OpenWorker.Gameplay.Modules.Persons.Components;

public readonly record struct AppearanceComponentEntry
{
    public required short Shape { get; init; }
    public required short Look { get; init; }
}