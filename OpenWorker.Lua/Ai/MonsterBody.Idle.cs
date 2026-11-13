using OpenWorker.Gameplay.Ai;
using OpenWorker.Hotspot.Modules.Movement.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// What a monster does with its time when nothing is asked of it: it turns before it walks the other way, and a monster out of
/// battle that has stood for a while plays an idle animation now and then.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// How long a monster stands before it may play an idle animation, and the chance in percent that it does at each look.
    /// The original gives every monster these two numbers, and a script may change them; none of this client does.
    /// </summary>
    private const double IdleCheckSeconds = 5.0;

    private const int IdleChancePercent = 20;

    private const string IdleAnimation = "N_Idle_01";
    private const string TurnLeft = "B_Turn_L";
    private const string TurnRight = "B_Turn_R";
    private const string BigTurnLeft = "B_Big_Turn_L";
    private const string BigTurnRight = "B_Big_Turn_R";

    private double _lastChange;
    private double _holdUntil;
    private bool _moveAfterHold;

    /// <summary>
    /// The monster begins to walk, and the clients are told how: at once, after a turn, or after a short wait. Returns whether the
    /// message of the walk is to be sent now.
    /// </summary>
    private bool BeginWalk(float wanted, bool wasStanding)
    {
        var now = _manager.Now;

        // A monster that is on its way finds its path again and again; a turn or a pause that is going on is not cut short by it.
        if (!wasStanding)
        {
            return now >= _holdUntil && !_moveAfterHold;
        }

        _holdUntil = 0;
        _moveAfterHold = false;

        // The animations of turning are those of the fight; out of it the monster pauses instead.
        var hasTurn = _battlePose && _actions.GetAnimationLength(Behavior.ActionFile, TurnLeft) > 0f && _actions.GetAnimationLength(Behavior.ActionFile, TurnRight) > 0f;
        var hasBigTurn = hasTurn && _actions.GetAnimationLength(Behavior.ActionFile, BigTurnLeft) > 0f && _actions.GetAnimationLength(Behavior.ActionFile, BigTurnRight) > 0f;

        switch (WalkStart.Of(Yaw, wanted, hasTurn, hasBigTurn, out var left))
        {
            case WalkStarts.Pause:
                SendIdle(StandAnimation(), WalkStart.PauseSeconds);

                _holdUntil = now + WalkStart.PauseSeconds;

                return true;

            case WalkStarts.Walk:
                return true;

            case var turn:
                var name = turn == WalkStarts.BigTurn ? left ? BigTurnLeft : BigTurnRight : left ? TurnLeft : TurnRight;

                SendIdle(_actions.GetAnimationIndex(Behavior.ActionFile, name), 0f);

                // The walk is told when the turn is over; until then the monster stands.
                _holdUntil = now + _actions.GetAnimationLength(Behavior.ActionFile, name);
                _moveAfterHold = true;

                return false;
        }
    }

    private int StandAnimation()
    {
        return _actions.GetAnimationIndex(Behavior.ActionFile, _battlePose ? "B_Stand" : "N_Stand");
    }

    private void SendIdle(int animation, float delay)
    {
        Broadcast(new MovementIdleBtResponse
        {
            Actor = Actor,
            Position = Position,
            Yaw = Yaw,
            Animation = animation,
            DefaultStep = 0,
            Delay = delay
        });
    }

    /// <summary>
    /// A walk waits for its turn or its pause to be over. Returns whether the monster stays where it is for now.
    /// </summary>
    private bool IsHeldBeforeWalk(double now)
    {
        if (now < _holdUntil)
        {
            return true;
        }

        if (_moveAfterHold)
        {
            _moveAfterHold = false;

            SendMove(now);
        }

        return false;
    }

    /// <summary>
    /// A monster out of battle that stands and has not changed what it does for a while may play an idle animation, which the
    /// clients are told of. One that has none stops asking.
    /// </summary>
    private void CheckIdleTime(double now)
    {
        if (IsMoving || IsAttacking || _battlePose || IsStaggered || _carried || _hiding.IsHidden || _grab.IsHolding || _reaction is { IsBusy: true } || now < _idleEnd)
        {
            _lastChange = now;

            return;
        }

        if (_lastChange <= 0)
        {
            _lastChange = now;
        }

        if (now - _lastChange <= IdleCheckSeconds)
        {
            return;
        }

        _lastChange = now;

        if (_manager.Random.Next(100) >= IdleChancePercent)
        {
            return;
        }

        var length = _actions.GetAnimationLength(Behavior.ActionFile, IdleAnimation);

        if (length <= 0f)
        {
            return;
        }

        _idleAnimation = _actions.GetAnimationIndex(Behavior.ActionFile, IdleAnimation);
        _idleEnd = now + length;

        SendIdle(_idleAnimation, 0f);
    }
}
