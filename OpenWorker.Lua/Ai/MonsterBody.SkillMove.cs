using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// An animation carries the monster the way the clients play it: each frame's move, turned by the way the monster faces, on the
/// floor (unless it is in the air) and a radius short of a wall or the edge of the ground. A wall, or the monster's target in the
/// way, ends the move of that animation, and the clients are told where the monster stands. A skill, an idle action, the
/// animation of a spawn and those of a reaction all carry it; a new animation ends the moves of the one before.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// How close to its target the monster comes, on top of the two bodies.
    /// </summary>
    private const float TargetMargin = 10f;

    private readonly List<(double Time, Vector2 Offset, int Clip)> _moves = [];

    /// <summary>
    /// Numbers the animations whose moves are kept, so that a cut ends the moves of one animation only.
    /// </summary>
    private int _moveClips;

    private void AddMoves(IReadOnlyList<SkillMove> moves, double at, int clips)
    {
        foreach (var move in moves)
        {
            _moves.Add((at + move.Time, new Vector2(move.Offset.X, move.Offset.Y), _moveClips + move.Clip));
        }

        _moveClips += Math.Max(clips, 1);
    }

    /// <summary>
    /// The monster begins an animation of its own after the given time: one that begins now ends the moves of the animation
    /// before it, one that begins later follows it.
    /// </summary>
    private void PlayMoves(string animation, float after = 0f)
    {
        if (after <= 0f)
        {
            _moves.Clear();
        }

        AddMoves(_actions.GetMoves(Behavior.ActionFile, animation), _manager.Now + Math.Max(after, 0f), 1);
    }

    /// <summary>
    /// Carries the monster by the moves that are due up to this moment.
    /// </summary>
    private void MoveWithSkill(double until)
    {
        while (_moves.Count > 0 && _moves[0].Time <= until)
        {
            var clip = _moves[0].Clip;
            var offset = Vector2.Zero;

            while (_moves.Count > 0 && _moves[0].Time <= until && _moves[0].Clip == clip)
            {
                offset += _moves[0].Offset;
                _moves.RemoveAt(0);
            }

            if (offset != Vector2.Zero && !Carry(offset))
            {
                _moves.RemoveAll(e => e.Clip == clip);
            }
        }
    }

    /// <summary>
    /// Returns whether the monster went the whole way. One that runs into its target stays where it is; one that runs into a wall
    /// stops a radius before it.
    /// </summary>
    private bool Carry(Vector2 offset)
    {
        var (sin, cos) = MathF.SinCos(Yaw * (MathF.PI / 180f));
        var airborne = IsAirborne;
        var from = airborne ? Position : OnFloor(Position);
        var wanted = new Vector3(from.X + offset.X * cos - offset.Y * sin, from.Y + offset.X * sin + offset.Y * cos, from.Z);

        if (RunsIntoTarget(wanted))
        {
            TellCut(from);

            return false;
        }

        var reached = _manager.Navigation.Clamp(from, wanted, Math.Min(Radius, MaxWayRadius), out var arrived);

        // In the air the monster keeps its height: the fall is the reaction's.
        if (airborne)
        {
            reached = reached with { Z = from.Z };
        }

        Place(reached, Yaw);

        if (!arrived)
        {
            TellCut(reached);
        }

        return arrived;
    }

    /// <summary>
    /// A monster stops before the player it fights. A helper of the players goes through.
    /// </summary>
    private bool RunsIntoTarget(Vector3 place)
    {
        return Owner is null
            && _manager.FindPlayer(Machine.TargetId) is { } target
            && AiMath.Distance2D(place, target.Position) <= Math.Min(Radius, MaxWayRadius) + target.Radius + TargetMargin;
    }

    private void TellCut(Vector3 place)
    {
        Broadcast(new MovementIgnoreMotionDeltaResponse { Actor = Actor, Pos = place, Yaw = Yaw, Pitch = 0f, Forced = 0 });
    }
}
