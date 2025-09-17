using System.Numerics;

namespace OpenWorker.Hotspot.Modules.World.Types;

public readonly record struct WarpValue
{
    public required bool HasError { get; init; }
    public required Vector3 Position { get; init; }
    public required float Rotation { get; init; }
}