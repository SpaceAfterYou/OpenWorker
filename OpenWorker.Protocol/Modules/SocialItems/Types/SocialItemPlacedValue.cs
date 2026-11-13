using System.Numerics;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Types;

/// <summary>
/// A social object with the place it stands at and the way it faces, which the client builds it from.
/// </summary>
[WireMessage(WireMethods.Write)]
public readonly partial record struct SocialItemPlacedValue
{
    public SocialItemValue Prop { get; init; }

    public Vector3 Position { get; init; }

    public float Rotation { get; init; }
}
