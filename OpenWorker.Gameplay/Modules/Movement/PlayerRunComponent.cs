using System.Numerics;
using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Gameplay.Modules.Movement;

/// <summary>
/// The run a player's client told with its last move: from the place it named, toward the end of the move, at its speed,
/// from a moment of the game clock. Between two moves the player goes on along it, as far as its end.
/// </summary>
[EntityComponent(EntityComponentService.All)]
public readonly record struct PlayerRunComponent(Vector3 From, Vector2 End, float Speed, TimeSpan At)
{
    /// <summary>
    /// No client runs faster than this, whatever it says.
    /// </summary>
    public const float MaxSpeed = 2000f;

    /// <summary>
    /// Where the player has got to at a moment, and whether it is still on its way; null when the player has been put
    /// anywhere since the run began (<paramref name="place"/> is not where it began).
    /// </summary>
    public (Vector3 Position, bool Moving)? Reached(Vector3 place, TimeSpan now)
    {
        if (place != From)
        {
            return null;
        }

        var start = new Vector2(From.X, From.Y);
        var way = End - start;
        var length = way.Length();
        var gone = Speed * (float)Math.Max((now - At).TotalSeconds, 0d);

        if (!(length >= 1f) || !(gone > 0f))
        {
            return (place, length >= 1f);
        }

        if (gone >= length)
        {
            return (new Vector3(End, From.Z), false);
        }

        return (new Vector3(start + way / length * gone, From.Z), true);
    }
}
