using System.Numerics;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// What a blow does to a monster over the time after it: the monster is pushed, thrown, lies and gets up as the clients show
/// it, and does nothing else meanwhile. The rules are in <see cref="MonsterReaction"/>; this is where the body keeps one and
/// tells the clients what the reaction does to the place of the monster.
/// </summary>
internal partial class MonsterBody
{
    /// <summary>
    /// How long a client is given to put a monster that has landed at the place it is told, in seconds.
    /// </summary>
    private const float LandingTime = 0.1f;

    /// <summary>
    /// How far from a player a slide stops, on top of the two bodies.
    /// </summary>
    private const float PlayerMargin = 10f;

    private const float SlideBodyLimit = 100f;

    private const short ChangeMotionRaise = 5;

    private MonsterReaction? _reaction;

    private MonsterReaction Reaction => _reaction ??= CreateReaction();

    private bool IsReactionBlockingMove => _reaction?.BlocksMove == true;

    private bool IsReactionBlockingAttack => _reaction?.BlocksAttack == true;

    private MonsterReaction CreateReaction()
    {
        var combat = _ecs.Get<MonsterCombatComponent>(Entity);
        var profile = new ReactionProfile(combat.WeightRank, combat.ReactionLimit, combat.StandType, combat.Rank, combat.BodyRadius);

        return new MonsterReaction(profile, _manager.Navigation, name => _actions.GetAnimationLength(Behavior.ActionFile, name), _manager.Random)
        {
            IsBlocked = place => _manager.IsPlayerNear(place, Math.Min(combat.BodyRadius, SlideBodyLimit) + PlayerMargin)
        };
    }

    /// <summary>
    /// A monster in a reaction is silent about walking: the clients start a walk at once whatever the monster does, so a word
    /// about one would lift it out of its fall.
    /// </summary>
    private bool IsSilencedByReaction(IResponseHotspotMessage message)
    {
        return _reaction is { IsBusy: true } && message is MovementMoveBtResponse or MovementStopBtResponse or MovementIdleBtResponse;
    }

    /// <summary>
    /// A blow lands on the monster. Returns what the clients are told in the hit.
    /// </summary>
    /// <param name="defense">The defense type of the monster when the blow lands, once its super armor has taken the blow.</param>
    /// <param name="survives">The monster lives on; one that dies is left where it is.</param>
    public HitOutcome ReceiveBlow(in Blow blow, DefenseType defense, bool survives, in BlowRolls rolls)
    {
        var outcome = Reaction.Hit(blow, defense, survives, Position, rolls);

        Place(outcome.Position, float.IsNaN(outcome.Facing) ? Yaw : outcome.Facing);

        if (outcome.Shown)
        {
            Interrupt();
        }

        return outcome;
    }

    /// <summary>
    /// The armor of the monster broke and it shows it: the monster stands helpless for the time.
    /// </summary>
    public void BreakArmor(float seconds)
    {
        Reaction.BreakArmor(seconds);

        Interrupt();
    }

    /// <summary>
    /// A reaction that shows ends whatever the monster was doing: its attack, with the blows and shots it had still to deal,
    /// and its walk.
    /// </summary>
    private void Interrupt()
    {
        _attackEnd = 0;
        _pending.Clear();
        _flights.Clear();
        _clips.Clear();
        ReleaseGrab(true);

        StopMoving(false);
    }

    /// <summary>
    /// The super armor of the monster is whole again after a break. The mind of the monster may have a skill it casts then.
    /// </summary>
    public virtual void OnSuperArmorRestored()
    {
        Machine.OnSuperArmorRestored();
    }

    private void StepReaction(float elapsed)
    {
        if (_reaction is not { IsBusy: true } reaction)
        {
            return;
        }

        var step = reaction.Step(elapsed, Position);

        if (step.Position != Position)
        {
            Place(step.Position, Yaw);
        }

        TellReaction(step);
    }

    /// <summary>
    /// The clients are told when a monster lands, is cut short by a wall, is stopped by a player or begins to get up; between
    /// those they move it themselves from the hit.
    /// </summary>
    private void TellReaction(in ReactionStep step)
    {
        if (step.Events == ReactionEvents.None)
        {
            return;
        }

        var actor = Actor;

        if (step.Events.HasFlag(ReactionEvents.FlightCut))
        {
            Broadcast(default(SkillSyncPositionBroadcastResponse) with { Actor = actor, Position = OnFloor(step.Position), Yaw = Yaw });
        }

        if (step.Events.HasFlag(ReactionEvents.Landed))
        {
            Broadcast(default(SkillMoveWithTimeBroadcastResponse) with { Actor = actor, Position = step.Position, Time = LandingTime });
        }

        if (step.Events.HasFlag(ReactionEvents.SlideBlocked))
        {
            Broadcast(new MovementIgnoreMotionDeltaResponse { Actor = actor, Pos = step.Position, Yaw = Yaw, Pitch = 0f, Forced = 0 });
        }

        if (step.Events.HasFlag(ReactionEvents.RaiseBegan))
        {
            Broadcast(new MonsterChangeMotionResponse { Actor = actor, Motion = step.Motion, SubMotion = ChangeMotionRaise });
        }
    }
}
