using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Types;

[WireMessage(WireMethods.Write)]
public readonly partial record struct WarpValue
{
    public required bool HasError { get; init; }
    public required Vector3 Position { get; init; }
    public required float Rotation { get; init; }
}