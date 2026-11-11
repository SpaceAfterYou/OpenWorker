using System.Numerics;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// What a helper does that a monster of the maze does not. It has an owner: the guard variables of its script read the
/// owner, its MOVE goes to the owner, and when it is too far from the owner it is put next to the owner at once. A skill
/// that is not meant for an enemy is used on the owner. Beaten down, it does not die: it stays at one point of life, gets up
/// with its recovery skill, cannot be hurt and heals a twentieth of its life every second until it is whole. It escapes a
/// run of blows with the skill its script names. A monster of the maze has no owner and none of this happens to it.
/// </summary>
public sealed partial class AiMachine
{
    /// <summary>
    /// How far a helper that keeps failing to find its way to its owner may be before it is put next to the owner.
    /// </summary>
    private const float ForcedFarDistance = 500f;

    /// <summary>
    /// How many times a helper may fail to find its way before the shorter distance counts.
    /// </summary>
    private const int FarChecks = 10;

    /// <summary>
    /// A beaten helper heals once a second, a twentieth of its life each time.
    /// </summary>
    private const float RecoveryTick = 1f;

    private const int RecoveryParts = 20;

    private int _farChecksLeft = FarChecks;
    private float _recoveryTime;

    /// <summary>
    /// The machine drives a helper of a person, not a monster of the maze.
    /// </summary>
    public bool IsFollower => _host.Owner is not null;

    /// <summary>
    /// The helper is beaten down and getting up again.
    /// </summary>
    public bool IsRecovering => _state == AiState.Recovery;

    /// <summary>
    /// The life of a helper would have reached zero: it is kept at one and gets up again instead of dying.
    /// </summary>
    public void Recover()
    {
        if (_state != AiState.Recovery)
        {
            ChangeState(AiState.Recovery);
        }
    }

    /// <summary>
    /// The owner calls its helpers back: the helper stands where the owner stands at once, whatever it was doing.
    /// </summary>
    public void Recall()
    {
        if (_host.Owner is { } owner)
        {
            WarpToOwner(owner.Position);
        }
    }

    private AiTarget Self => new(NoTarget, _host.Position, _host.Radius, _host.Yaw, _host.Health, _host.MaxHealth, false);

    /// <summary>
    /// The guard variables: for a helper they read its owner. A monster of the maze has nobody to guard here.
    /// </summary>
    private double ReadGuard(AiVariable variable)
    {
        var owner = _host.Owner;

        return variable switch
        {
            AiVariable.GuardDistance => owner is { } near ? Vector3.Distance(_host.Position, near.Position) : -1,
            AiVariable.GuardDistanceCapsule => owner is { } body ? Vector3.Distance(_host.Position, body.Position) - body.Radius : -1,
            _ => owner is { MaxHealth: > 0 } hurt ? (int)(hurt.Health * 100f / hurt.MaxHealth) : 100
        };
    }

    /// <summary>
    /// Whom a skill is used on: a helper uses a skill that is not meant for an enemy on its owner.
    /// </summary>
    private AiTarget? TargetFor(AiSkill skill)
    {
        return skill.TargetsOwner && _host.Owner is { } owner ? owner : Target;
    }

    /// <summary>
    /// MOVE of a helper goes to a place near its owner, at the distance and angle of the state. False for a monster.
    /// </summary>
    private bool StartFollowing()
    {
        if (_host.Owner is not { } owner)
        {
            return false;
        }

        if (IsFarFromOwner(false, false))
        {
            return true;
        }

        var destination = PlaceAround(owner, _settings.TargetDistanceMin, _settings.TargetDistanceMax, _settings.AngleMin, _settings.AngleMax);

        if (!_host.MoveTo(destination, true) && !IsFarFromOwner(false, true))
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);
        }

        return true;
    }

    /// <summary>
    /// A helper on its way to its owner: too far, it is put next to the owner; arrived, it goes on. False for a monster.
    /// </summary>
    private bool UpdateFollowing()
    {
        if (!IsFollower)
        {
            return false;
        }

        if (!IsFarFromOwner(false, false) && !_host.IsMoving)
        {
            ChangeState(_host.IsBattlePose ? AiState.SelectAction : AiState.Wait);
        }

        return true;
    }

    /// <summary>
    /// A monster that cannot find its way to its target chooses again; a helper first checks that it is not lost far from
    /// its owner, then waits.
    /// </summary>
    private void GiveUpReaching()
    {
        if (!IsFollower)
        {
            ChangeState(AiState.SelectAction);

            return;
        }

        if (!IsFarFromOwner(false, true))
        {
            ChangeState(AiState.Wait);
        }
    }

    /// <summary>
    /// A helper farther from its owner than its script allows (out of a fight or in one) is put next to the owner, if
    /// there is ground there. One that keeps failing to find its way is put there from a shorter distance. True when it was
    /// moved.
    /// </summary>
    private bool IsFarFromOwner(bool battle, bool force)
    {
        var settings = _profile.Follower;

        if (_host.Owner is not { } owner || settings.FarDistance <= 0f)
        {
            return false;
        }

        var limit = battle ? settings.FarBattleDistance : settings.FarDistance;

        if (force && _farChecksLeft-- < 0)
        {
            limit = ForcedFarDistance;
        }

        if (Vector3.Distance(_host.Position, owner.Position) <= limit || !_host.HasGround(owner.Position))
        {
            return false;
        }

        _farChecksLeft = FarChecks;

        WarpToOwner(owner.Position);

        return true;
    }

    private void WarpToOwner(Vector3 position)
    {
        ClearTarget();

        _host.WarpTo(position);

        ChangeState(AiState.SelectAction);
    }

    /// <summary>
    /// The state that follows the owner: a monster without an owner has nobody to follow and waits.
    /// </summary>
    private void StartTraceMove()
    {
        if (!IsFollower)
        {
            ChangeState(AiState.Wait);

            return;
        }

        StartFollowing();
    }

    private void StartRecovery()
    {
        _recoveryTime = 0f;

        if (_profile.Follower.RecoverySkill == 0)
        {
            return;
        }

        ClearTarget();
        _host.ClearBuffs();

        _host.StopMoving(true);
        _host.SetInvincible(true);

        if (_host.FindSkill(_profile.Follower.RecoverySkill) is { } skill)
        {
            _host.UseSkill(skill, Self);
        }
    }

    private void UpdateRecovery(float elapsed)
    {
        _recoveryTime += elapsed;

        if (_recoveryTime <= RecoveryTick)
        {
            return;
        }

        _recoveryTime = 0f;

        _host.ClearBuffs();
        _host.Heal(Math.Max(1, _host.MaxHealth / RecoveryParts));

        if (_host.Health >= _host.MaxHealth)
        {
            ChangeState(AiState.SelectAction);
        }
    }

    private void EndRecovery()
    {
        _host.ClearBuffs();
        _host.RestoreHealth();
        _host.SetInvincible(false);
    }
}
