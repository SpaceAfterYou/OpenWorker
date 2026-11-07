using System.Numerics;

namespace OpenWorker.Channel;

/// <summary>
/// What a client says about where it stands is not to be trusted blindly: a broken value would stay in the save.
/// </summary>
public static class Place
{
    private const float MaxRotation = 360f;

    public static bool IsValid(Vector3 position)
    {
        return float.IsFinite(position.X) && float.IsFinite(position.Y) && float.IsFinite(position.Z);
    }

    public static bool IsValid(Vector3 position, float rotation)
    {
        return IsValid(position) && rotation is >= -MaxRotation and <= MaxRotation;
    }
}
