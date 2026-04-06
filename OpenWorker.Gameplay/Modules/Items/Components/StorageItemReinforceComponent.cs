namespace OpenWorker.Gameplay.Modules.Items.Components;

public readonly record struct StorageItemReinforceComponent
{
    public short MaxLevel { get; init; }
    public short TryCount { get; init; }
    public short MaxTryCount { get; init; }
}