using System.Numerics;

namespace OpenWorker.Content.Tool.Rendering;

/// <summary>
/// Converts SoulWorker world coordinates (DirectX left-handed, Z up, XY floor) to OpenGL (right-handed, Y up).
/// </summary>
internal static class GameCoordinates
{
    /// <summary>
    /// Game (X, Y, Z) → GL (X, Z, −Y): up stays up, the floor keeps its scale, the hand of the axes matches OpenGL.
    /// </summary>
    public static Vector3 ToGl(Vector3 game) => new(game.X, game.Z, -game.Y);
}
