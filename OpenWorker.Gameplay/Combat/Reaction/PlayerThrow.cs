using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Gameplay.Combat;

/// <summary>
/// What a blow of a monster does to a player, as far as the hit tells it. The server does not move the player: the client of
/// the player flies it from the hit and reports where it stands, and the report replaces what the server keeps.
/// </summary>
public static class PlayerThrow
{
    /// <summary>
    /// The throw and the fly speed to put in the hit. A throw that lifts the player is told in the frame of the attacker; any
    /// other is told as the place the push ends at, a radius before a wall.
    /// </summary>
    public static (Vector3 ExtraMove, float FlySpeed) Told(in Blow blow, in Struck player, in BlowRolls rolls, INavigation ground, float radius)
    {
        var thrown = ExtraMove.Of(blow, player, rolls);
        var push = blow.Reaction;
        var vector = thrown.Vector;

        if (player.Defense is DefenseType.SuperArmor or DefenseType.Invincible or DefenseType.NoReaction && !MonsterReaction.Displayed(push.Hardness, player.Defense))
        {
            vector = Vector3.Zero;
        }

        // A throw with no height is only a push.
        var physical = MonsterReaction.IsLaunch(blow.Kind) && vector.Z <= 0f ? ReactionKind.Flinch : blow.Kind;

        if (MonsterReaction.IsLaunch(physical))
        {
            return (vector, push.Speed > 0f ? push.Speed : new Vector2(vector.X, vector.Y).Length());
        }

        if (!push.Moves)
        {
            return (vector, 0f);
        }

        var place = player.Position;

        if (player.Defense == DefenseType.Normal)
        {
            var offset = MonsterReaction.IsLaunch(blow.Kind) ? AiMath.Rotate(new Vector2(vector.X, vector.Y), thrown.Yaw) : new Vector2(vector.X, vector.Y);

            place = ground.Clamp(player.Position, player.Position + new Vector3(offset, 0f), radius, out _);
        }

        return (new Vector3(place.X, place.Y, vector.Z), 0f);
    }
}
