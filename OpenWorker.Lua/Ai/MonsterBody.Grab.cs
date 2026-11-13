using System.Numerics;
using Arch.Core;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A monster that grabs: the blow of a skill takes the first player it finds, the player is told where it will be put down, the
/// animation of the skill lets it go at the moment its file says, and the blow that follows lands on the player at the place.
/// The rules of the hold are in <see cref="GrabHold"/>; this is where the body keeps one and tells the clients.
/// </summary>
internal partial class MonsterBody
{
    private readonly GrabHold _grab = new();
    private Entity _held;
    private readonly List<(double Time, SkillDetach Detach)> _detaches = [];
    private int _grabSkill;
    private bool _staged;
    private double _stageEnd;

    /// <summary>
    /// The monster holds a player.
    /// </summary>
    public bool IsHolding => _grab.IsHolding;

    /// <summary>
    /// The monster is gone from the maze: a player it holds is let go.
    /// </summary>
    public void Retired() => ReleaseGrab(true);

    /// <summary>
    /// What happens in one animation of the skill, from the moment it begins: the blows and shots, the hiding, and the moments
    /// at which a held player is let go.
    /// </summary>
    private void AddStage(int skill, SkillStage stage, double at)
    {
        if (stage.Clip is { } clip)
        {
            _clips.Add((at, at + stage.Length, clip));
        }

        foreach (var hit in stage.Hits)
        {
            _pending.Add(new PendingHit(at + hit.Time, hit, skill));
        }

        foreach (var shot in stage.Shots)
        {
            _pending.Add(new PendingHit(at + shot.Time, shot, skill, true));
        }

        _pending.Sort((first, second) => first.Time.CompareTo(second.Time));

        foreach (var hiding in stage.Hidings.Where(e => IsFollowed(e.Event)))
        {
            _hidings.Add((at + hiding.Time, hiding.Event));
        }

        foreach (var detach in stage.Detaches)
        {
            _detaches.Add((at + detach.Time, detach));
        }

        AddMoves(stage.Moves, at, 1);
    }

    /// <summary>
    /// The start of a grabbing skill is over: the monster holds the player it took, and plays the animation of holding, or it
    /// took nobody and plays the animation of the miss.
    /// </summary>
    private void ContinueGrabSkill()
    {
        _staged = false;

        var stage = _actions.GetStage(_grabSkill, Behavior.ActionFile, _grab.IsHolding ? GrabStage.Holding : GrabStage.Missed);

        AddStage(_grabSkill, stage, _stageEnd);

        _attackEnd = _stageEnd + stage.Length;
    }

    /// <summary>
    /// A blow that grabs lands on the players in its area. Nothing grabs a player that nothing can hurt, and a monster holds one
    /// player at a time; the first player of the area that is free is taken and told where it will be put down. Returns the
    /// players the blow hurts.
    /// </summary>
    private IReadOnlyList<Entity> TakeGrabbed(AttackTrigger trigger, IReadOnlyList<Entity> victims, int skill)
    {
        if (!trigger.IsGrab)
        {
            return victims;
        }

        var hurt = victims.Where(e => !_manager.IsShielded(e)).ToArray();

        if (_grab.IsHolding || hurt.Length == 0)
        {
            return hurt;
        }

        var nearest = hurt.Where(_manager.CanBeGrabbed).OrderBy(e => Vector3.DistanceSquared(Position, _manager.PositionOf(e))).Cast<Entity?>().FirstOrDefault();

        if (nearest is not { } taken)
        {
            return hurt;
        }

        var drop = GrabHold.DropPointOf(Position, Yaw, trigger.GrabDrop, _manager.Navigation, PlayerVitals.BodyRadius);
        var actor = _ecs.Get<ActorComponent>(taken);

        _grab.Seize(actor.Identifier, drop);
        _held = taken;
        _grabSkill = skill;
        _manager.Grab(taken);

        Broadcast(new MovementGrapResponse { Actor = actor, Position = drop });

        return hurt;
    }

    private void ResolveGrab(double now)
    {
        if (_staged && now >= _stageEnd)
        {
            ContinueGrabSkill();
        }

        while (_detaches.Count > 0 && _detaches[0].Time <= now)
        {
            var (time, detach) = _detaches[0];

            _detaches.RemoveAt(0);

            // The blow that follows the let-go is dealt by it, not by the animation; without a hold the animation deals it.
            if (_grab.BeginDrop(time, detach.DropTime, detach.FollowUp) && detach.FollowUp is { } followUp)
            {
                _pending.RemoveAll(e => !e.Shot && e.Hit.Trigger.Id == followUp.Id);
            }
        }

        if (!_grab.IsHolding)
        {
            return;
        }

        if (_manager.FindPlayerEntity(_grab.Victim) is not { } victim)
        {
            ReleaseGrab(false);

            return;
        }

        if (_grab.HasArrived(now))
        {
            PutDown(victim);
        }
        else if (now >= _attackEnd)
        {
            // The skill is over and the player was never let go.
            ReleaseGrab(true);
        }
    }

    /// <summary>
    /// The player has flown to the place: it stands there, the blow that follows lands on it wherever the area of the blow is,
    /// and the clients put it down.
    /// </summary>
    private void PutDown(Entity victim)
    {
        var point = _grab.DropPoint;
        var followUp = _grab.FollowUp;

        _manager.Place(victim, point);

        if (followUp is { } trigger)
        {
            _manager.HitPlayers(this, _grabSkill, trigger, [victim]);
        }

        Broadcast(new MovementDropResponse
        {
            Actor = _ecs.Get<ActorComponent>(victim),
            Position = point,
            Yaw = _ecs.Get<WorldComponent>(victim).Rotation,
            Sync = true
        });

        _manager.LetGo(victim);
        _grab.Release();
    }

    /// <summary>
    /// The monster lets go of the player where it stands: its skill is over, it was hurt, or it died. The clients free the player
    /// without moving it.
    /// </summary>
    /// <param name="tell">The player is still there to be told.</param>
    private void ReleaseGrab(bool tell)
    {
        _staged = false;

        if (!_grab.IsHolding)
        {
            return;
        }

        _manager.LetGo(_held);

        if (tell && _manager.FindPlayerEntity(_grab.Victim) is { } victim)
        {
            Broadcast(new MovementDropResponse
            {
                Actor = _ecs.Get<ActorComponent>(victim),
                Position = _manager.PositionOf(victim),
                Yaw = _ecs.Get<WorldComponent>(victim).Rotation,
                Sync = false
            });
        }

        _grab.Release();
        _detaches.Clear();
    }
}
