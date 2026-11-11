using System.Numerics;

namespace OpenWorker.Content.Tool.Models;

/// <summary>
/// One event box drawn as a ground rectangle from its top-left, bottom-right and yaw.
/// </summary>
public sealed class BatchBoxDraw
{
    public required int Id { get; init; }

    public required string Kind { get; init; }

    public required Vector3 TopLeft { get; init; }

    public required Vector3 BottomRight { get; init; }

    /// <summary>Yaw in degrees around world up (game Z).</summary>
    public required float RotationDegrees { get; init; }

    public required Vector3 Color { get; init; }
}
