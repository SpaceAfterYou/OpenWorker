using System.Numerics;
using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A skill carries the monster the way its animations do, as the clients play them: each frame's move, turned by the facing the
/// skill began with, on the floor and a radius short of a wall or the edge of the ground. A wall, or the monster's target in the
/// way, ends the move of that animation, and the clients are told where the monster stands.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// How close to its target the monster comes, on top of the two bodies.
    /// </summary>
    private const float TargetMargin = 10f;

    private readonly List<(double Time, Vector2 Offset, int Clip)> _moves = [];

    /// <summary>
    /// The facing the skill began with, by which its moves are turned.
    /// </summary>
    private float _moveYaw;

    /// <summary>
    /// Numbers the animations whose moves are kept, so that a cut ends the moves of one animation only.
    /// </summary>
    private int _moveClips;

    private void AddMoves(IReadOnlyList<SkillMove> moves, double at, int clips)
    {
        _moveYaw = Yaw;

        foreach (var move in moves)
        {
            _moves.Add((at + move.Time, new Vector2(move.Offset.X, move.Offset.Y), _moveClips + move.Clip));
        }

        _moveClips += Math.Max(clips, 1);
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
        var (sin, cos) = MathF.SinCos(_moveYaw * (MathF.PI / 180f));
        var from = OnFloor(Position);
        var wanted = new Vector3(from.X + offset.X * cos - offset.Y * sin, from.Y + offset.X * sin + offset.Y * cos, from.Z);

        if (RunsIntoTarget(wanted))
        {
            TellCut(from);

            return false;
        }

        var reached = _manager.Navigation.Clamp(from, wanted, Math.Min(Radius, MaxWayRadius), out var arrived);

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
