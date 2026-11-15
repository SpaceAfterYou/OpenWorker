using OpenWorker.Batch.Actions;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A skill's animation may turn the monster to its target as it plays, at the speed its event names, so a dash bends toward a
/// target that has moved. When the turning stops the clients are told the way the monster looks, and they take it at once:
/// what each client turned to on its own is put right before the rest of the skill carries the monster.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// The speed by which an event turns the monster to its target at once instead of over time.
    /// </summary>
    private const int LookAtOnce = 1000;

    private readonly List<(double Time, int Speed)> _looks = [];
    private float _lookSpeed;
    private double _lookedAt;

    private void AddLooks(IReadOnlyList<SkillLook> looks, double at)
    {
        _looks.Clear();
        _lookSpeed = 0f;
        _lookedAt = at;

        for (var index = 0; index < looks.Count; index++)
        {
            _looks.Add((at + looks[index].Time, looks[index].TurnSpeed));
        }
    }

    /// <summary>
    /// Turns the monster by the events of its skill up to this moment.
    /// </summary>
    private void Look(double until)
    {
        if (!IsAttacking)
        {
            _looks.Clear();
            _lookSpeed = 0f;

            return;
        }

        while (true)
        {
            var due = _looks.Count > 0 && _looks[0].Time <= until;
            var next = due ? _looks[0].Time : until;

            if (_lookSpeed > 0f && next > _lookedAt)
            {
                TurnToTarget(_lookSpeed * (float)(next - _lookedAt));
            }

            _lookedAt = Math.Max(_lookedAt, next);

            if (!due)
            {
                return;
            }

            var speed = _looks[0].Speed;

            _looks.RemoveAt(0);

            if (speed == LookAtOnce)
            {
                TurnToTarget(180f);
                TellFacing();
            }
            else if (speed > 0)
            {
                _lookSpeed = speed;
            }
            else
            {
                _lookSpeed = 0f;
                TellFacing();
            }
        }
    }

    /// <summary>
    /// Turns the monster toward its target by at most the given degrees.
    /// </summary>
    private void TurnToTarget(float degrees)
    {
        if (Machine.TargetId == AiMachine.NoTarget || GetTarget(Machine.TargetId) is not { } target || AiMath.Distance2D(Position, target.Position) < 1f)
        {
            return;
        }

        var yaw = Yaw;
        var wanted = AiMath.YawTo(Position, target.Position);
        var turn = (wanted - yaw) % 360f;

        turn = turn > 180f ? turn - 360f : turn < -180f ? turn + 360f : turn;

        var turned = yaw + Math.Clamp(turn, -degrees, degrees);

        Place(Position, turned > 180f ? turned - 360f : turned < -180f ? turned + 360f : turned);
    }

    private void TellFacing()
    {
        Broadcast(new MovementUpdateDirResponse { Actor = Actor, Yaw = Yaw, Pitch = 0f, Direct = true });
    }
}
