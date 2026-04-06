using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Gameplay.Modules.Items.Archetypes;

internal readonly struct StorageArchetype
{
    public required StorageGroup Group { get; init; }
    public required short Size { get; init; }
    public required ComponentType[] Components { get; init; }
}