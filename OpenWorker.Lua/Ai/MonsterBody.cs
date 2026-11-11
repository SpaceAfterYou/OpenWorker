using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Gameplay.Ai;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Movement.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua.Ai;

/// <summary>
/// A monster as its mind sees it. The mind decides, the body walks, turns, attacks, and tells the clients about it.
/// </summary>
internal partial class MonsterBody : IAiHost
{
    /// <summary>
    /// How often a client hears about a monster that keeps walking, in seconds.
    /// </summary>
    private const double MoveResend = 0.6;

    private const float TurnToReport = 1f;

    /// <summary>
    /// How wide a way a monster needs at most: the body of a monster that fills a room does not make it walk round the walls.
    /// </summary>
    private const float MaxWayRadius = 100f;
    private const float Arrived = 3f;
    private const float AttackDefaultLength = 1f;
    private const short NoMotion = -1;

    private readonly World _ecs;
    private readonly LuaCreatureManager _manager;
    private readonly SkillActionProvider _actions;
    private readonly List<PendingHit> _pending = [];

    private readonly List<Vector3> _route = [];
    private readonly List<Vector3> _found = [];

    private Vector3 _home;

    private Vector3? _destination;
    private bool _run;
    private bool _battlePose;
    private bool _gazing;
    private double _attackEnd;
    private double _staggerEnd;
    private double _forcedDefenseEnd;
    private string _spawnAction = string.Empty;
    private int _idleAnimation;
    private double _idleEnd;
    private InvisibleEvent? _hidden;
    private double _hiddenEnd;
    private readonly List<(double Time, InvisibleEvent Event)> _hidings = [];
    private double _lastMoveSent;
    private float _lastYawSent = float.NaN;
    private double _born;

    public MonsterBody(World ecs, LuaCreatureManager manager, SkillActionProvider actions, Entity entity, MonsterBehavior behavior, AiProfile profile, Vector3 home)
    {
        _ecs = ecs;
        _manager = manager;
        _actions = actions;

        Entity = entity;
        Behavior = behavior;
        _home = home;
        _born = manager.Now;

        Machine = CreateMind(profile);
    }

    protected World Ecs => _ecs;

    protected LuaCreatureManager Manager => _manager;

    public Entity Entity { get; private set; }
    public MonsterBehavior Behavior { get; private set; }
    public AiMachine Machine { get; private set; }

    private AiMachine CreateMind(AiProfile profile)
    {
        var machine = new AiMachine(profile, this, _manager.Random);

        machine.StateChanged += OnStateChanged;

        return machine;
    }

    private void OnStateChanged(AiState old, AiState state)
    {
        _manager.Logger.MonsterChangedState(Behavior.Id, Entity.Id, old, state);
    }

    /// <summary>
    /// The body of a monster that is gone takes a new monster of the same script: its lists are kept, everything it
    /// knew is forgotten, and the mind starts afresh, because a mind carries too much to clear by hand.
    /// </summary>
    public MonsterBody Revive(Entity entity, MonsterBehavior behavior, Vector3 home)
    {
        Entity = entity;
        Behavior = behavior;
        _home = home;
        IsRevealed = false;

        _pending.Clear();
        _route.Clear();
        _found.Clear();
        _flights.Clear();
        _destination = null;
        _run = false;
        _battlePose = false;
        _gazing = false;
        _attackEnd = 0;
        _staggerEnd = 0;
        _reaction = null;
        _forcedDefenseEnd = 0;
        _spawnAction = string.Empty;
        _idleEnd = 0;
        _hidden = null;
        _hidings.Clear();
        Group = default;
        _lastMoveSent = 0;
        _lastYawSent = float.NaN;
        _born = _manager.Now;
        _shotSession = 0;

        Machine = CreateMind(Machine.Profile);

        return this;
    }

    /// <summary>
    /// The clients know the monster and can be told what it does.
    /// </summary>
    public bool IsRevealed { get; private set; }

    private ActorComponent Actor => _ecs.Get<ActorComponent>(Entity);

    private readonly record struct PendingHit(double Time, SkillHit Hit, int Skill, bool Shot = false);

    private sealed class Flight(ProjectileFlight projectile, AttackTrigger trigger, int skill)
    {
        public ProjectileFlight Projectile { get; } = projectile;
        public AttackTrigger Trigger { get; } = trigger;
        public int Skill { get; } = skill;
    }

    private readonly List<Flight> _flights = [];
    private uint _shotSession;

    // ---- The body in the world ----------------------------------------------------------------

    public Vector3 Position => _ecs.Get<WorldComponent>(Entity).Position;

    public virtual Vector3 HomePosition => _home;

    public float Yaw => _ecs.Get<WorldComponent>(Entity).Rotation;

    public float Radius => _ecs.Get<MonsterCombatComponent>(Entity).BodyRadius;

    public int Health => _ecs.Get<CreatureComponent>(Entity).Health;

    public int MaxHealth => (int)_ecs.Get<MonsterCombatComponent>(Entity).Stats.MaxHealth;

    public bool IsDead => !_ecs.IsAlive(Entity) || Health <= 0;

    public bool IsAttacking => _manager.Now < _attackEnd;

    public bool CanMove => !IsAttacking && !IsStaggered && !IsReactionBlockingMove && !Behavior.IsRooted && (_battlePose || !Behavior.WalksOnlyInBattle);

    private bool IsStaggered => _manager.Now < _staggerEnd;

    public bool IsRooted => Behavior.IsRooted;

    public bool CanAttack => !IsAttacking && !IsStaggered && !IsReactionBlockingAttack;

    public bool IsMoving => _destination is not null;

    public bool IsBattlePose => _battlePose;

    public float Age => (float)(_manager.Now - _born);

    private float Speed => (_run ? Behavior.RunSpeed : Behavior.WalkSpeed) is > 0f and var speed ? speed : DefaultSpeed(_run);

    private static float DefaultSpeed(bool run) => run ? 300f : 100f;

    /// <summary>
    /// The monster has appeared on the screens: from now on it is told to the clients when something happens to it.
    /// </summary>
    public void Reveal()
    {
        if (IsRevealed)
        {
            return;
        }

        IsRevealed = true;

        Machine.Start();
    }

    /// <summary>
    /// The animation a spawn box has the monster play as it appears. The monster does nothing else until it is over, unless it
    /// is one that starts over by itself. Null when the box asks for none or the file has no such animation.
    /// </summary>
    public MovementIdleEntry? StartSpawnAction()
    {
        var length = string.IsNullOrEmpty(_spawnAction) ? 0f : _actions.GetAnimationLength(Behavior.ActionFile, _spawnAction);

        if (length <= 0f)
        {
            return null;
        }

        _idleAnimation = _actions.GetAnimationIndex(Behavior.ActionFile, _spawnAction);

        if (!_actions.IsLooping(Behavior.ActionFile, _spawnAction))
        {
            _idleEnd = _manager.Now + length;
            _attackEnd = Math.Max(_attackEnd, _idleEnd);
        }

        return IdleEntry();
    }

    /// <summary>
    /// What the monster plays now if it is in the middle of an animation of its own, for a client that comes into the maze late.
    /// </summary>
    public MovementIdleEntry? CurrentAction()
    {
        return _manager.Now < _idleEnd ? IdleEntry() : null;
    }

    private MovementIdleEntry IdleEntry()
    {
        return new MovementIdleEntry(Actor, Position, Yaw, _idleAnimation, 0f);
    }

    public void SetSpawnAction(string action)
    {
        _spawnAction = action;
    }

    /// <summary>
    /// A hit has thrown the monster off balance: whatever it was doing is over and it can do nothing for a while.
    /// </summary>
    public void Stagger(float seconds)
    {
        if (!IsRevealed)
        {
            return;
        }

        _staggerEnd = _manager.Now + seconds;
        _attackEnd = 0;
        _pending.Clear();
        _flights.Clear();

        if (IsMoving)
        {
            StopMoving(true);
        }
    }

    public void Update(double now, float elapsed)
    {
        if (!IsRevealed || IsDead)
        {
            return;
        }

        StepReaction(elapsed);
        if (_forcedDefenseEnd > 0 && now >= _forcedDefenseEnd)
        {
            _forcedDefenseEnd = 0;
            _manager.RestoreDefense(Entity);
        }

        Advance(now, elapsed);
        ResolveHits(now);
        ResolveHidings(now);
        Fly(elapsed);

        // A monster that has hidden thinks no more, unless its animation leaves the mind on.
        if (_hidden is null || (_hidden.Flags & InvisibleEvent.KeepsMind) != 0)
        {
            Machine.Update(elapsed);
        }
    }

    // ---- Movement -----------------------------------------------------------------------------

    private void Advance(double now, float elapsed)
    {
        if (_destination is null)
        {
            return;
        }

        var left = Speed * elapsed;
        var turned = false;

        while (_route.Count > 0)
        {
            var position = Position;
            var waypoint = _route[0];
            var offset = new Vector2(waypoint.X - position.X, waypoint.Y - position.Y);
            var distance = offset.Length();

            if (distance <= Math.Max(left, Arrived))
            {
                Place(OnFloor(new Vector3(waypoint.X, waypoint.Y, position.Z)), Yaw);

                left = Math.Max(0f, left - distance);

                _route.RemoveAt(0);

                turned = turned || _route.Count > 0;

                continue;
            }

            var direction = offset / distance;

            Place(OnFloor(new Vector3(position.X + direction.X * left, position.Y + direction.Y * left, position.Z)), AiMath.YawOf(direction.X, direction.Y));

            if (turned || now - _lastMoveSent >= MoveResend)
            {
                SendMove(now);
            }

            return;
        }

        _destination = null;

        SendStop();
    }

    private Vector3 OnFloor(Vector3 position)
    {
        return _manager.Navigation.TryGetFloor(position, out var height) ? position with { Z = height } : position;
    }

    private void Place(Vector3 position, float yaw)
    {
        _ecs.Set(Entity, _ecs.Get<WorldComponent>(Entity) with { Position = position, Rotation = yaw });
    }

    public void StopMoving(bool send)
    {
        var moved = _destination is not null;

        _destination = null;
        _route.Clear();

        if (send || moved)
        {
            SendStop();
        }
    }

    public bool MoveTo(Vector3 destination, bool run)
    {
        if (!CanMove || !float.IsFinite(destination.X) || !float.IsFinite(destination.Y))
        {
            return false;
        }

        var position = Position;

        if (!_manager.Navigation.TryFindPath(position, destination, Math.Min(Radius, MaxWayRadius), _found))
        {
            return false;
        }

        _route.Clear();

        // The first point of the way is the place the monster stands at.
        _route.AddRange(_found.Skip(1));

        _run = run;
        _destination = _route.Count > 0 ? _route[^1] : position;

        if (_route.Count == 0)
        {
            return true;
        }

        Place(position, AiMath.YawTo(position, _route[0]));

        SendMove(_manager.Now);

        return true;
    }

    public void FaceTo(Vector3 point)
    {
        var position = Position;

        if (AiMath.Distance2D(position, point) < 1f)
        {
            return;
        }

        var yaw = AiMath.YawTo(position, point);

        Place(position, yaw);

        if (IsMoving || !float.IsNaN(_lastYawSent) && AiMath.AngleBetween(_lastYawSent, yaw) < TurnToReport)
        {
            return;
        }

        _lastYawSent = yaw;

        Broadcast(new MovementUpdateDirResponse { Actor = Actor, Yaw = yaw, Pitch = 0f, Direct = false });
    }

    public void Teleport(Vector3 position)
    {
        _destination = null;
        _route.Clear();

        Place(position, Yaw);

        Broadcast(new SkillWarpPositionBroadcastResponse
        {
            Actor = Actor,
            PosInfo = new SkillPositionValue { Position = position, Angle = Yaw, MotionClass = NoMotion }
        });
    }

    public void SetBattlePose(bool battle, bool playMotion)
    {
        if (_battlePose == battle)
        {
            return;
        }

        _battlePose = battle;

        if (IsMoving)
        {
            StopMoving(true);
        }

        Broadcast(new MovementBattleBtResponse
        {
            Actor = Actor,
            Pos = Position,
            Yaw = Yaw,
            BattlePose = (byte)(battle ? 1 : 0),
            PlayMotion = (byte)(playMotion ? 1 : 0)
        });
    }

    public void SetTarget(int id)
    {
        Broadcast(new MonsterTargetChangeResponse { Actor = Actor, Target = id });
    }

    public void SetGaze(bool gazing)
    {
        if (_gazing == gazing)
        {
            return;
        }

        _gazing = gazing;

        Broadcast(new MovementGazeBtResponse { Actor = Actor, Position = Position, Yaw = Yaw, Gazing = gazing });
    }

    public void SetInvincible(bool invincible)
    {
        // The clients are told by the defense type a monster carries in the next hit it takes.
        _manager.SetInvincible(Entity, invincible);
    }

    public void ClearBuffs()
    {
        _manager.Buffs?.ClearAll(Entity);
    }

    public void RestoreHealth()
    {
        _ecs.Set(Entity, _ecs.Get<CreatureComponent>(Entity) with { Health = MaxHealth });

        Broadcast(new MonsterUpdateStatListResponse
        {
            Actor = Actor,
            Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_CUR, statValue = MaxHealth }]
        });
    }

    public void Die()
    {
        _manager.Kill(Entity);
    }

    /// <summary>
    /// A monster of the maze has no owner; a helper has.
    /// </summary>
    public virtual AiTarget? Owner => null;

    /// <summary>
    /// How far above or below a place the ground may be for something to stand there.
    /// </summary>
    private const float GroundReach = 200f;

    public bool HasGround(Vector3 position)
    {
        return _manager.Navigation.TryGetFloor(position, out var height) && MathF.Abs(height - position.Z) <= GroundReach;
    }

    public virtual void WarpTo(Vector3 position) => Teleport(position);

    public void Heal(int amount)
    {
        var health = Math.Min(MaxHealth, Health + Math.Max(0, amount));

        _ecs.Set(Entity, _ecs.Get<CreatureComponent>(Entity) with { Health = health });

        Broadcast(new MonsterUpdateStatListResponse
        {
            Actor = Actor,
            Stats = [new StatKeyValuePair { byIndex = STAT_TYPE.STAT_HP_CUR, statValue = health }]
        });
    }

    public AiSkill? FindSkill(int id) => _manager.Services?.Behaviors.GetSkill(id);

    // ---- Hate and escape ----------------------------------------------------------------------

    /// <summary>
    /// The monsters that come to help when this one fights, from the spawn box it came from.
    /// </summary>
    public AggroGroup Group { get; private set; }

    public void SetGroup(AggroGroup group)
    {
        Group = group;
        Machine.CallsGroup = group.Id != 0 && group.MaxCount != 0;
    }

    public void AlertGroup(int target)
    {
        _manager.AlertGroup(this, target);
    }

    /// <summary>
    /// Whether the monster lies on the ground or flies after a blow.
    /// </summary>
    public virtual bool IsDownOrFlying => _reaction is { IsAirborne: true } or { IsDown: true };

    public void ForceDefense(AiDefense defense, float seconds)
    {
        _forcedDefenseEnd = _manager.Now + seconds;
        _manager.ForceDefense(Entity, defense == AiDefense.Invincible ? DefenseType.Invincible : DefenseType.SuperArmor);
    }

    public void EndFlinch()
    {
        _staggerEnd = 0;
        _reaction?.EndFlinch();
    }

    public void NotifyEscape(float seconds)
    {
        Broadcast(new MonsterEscapeDamageResponse
        {
            Actor = Actor,
            DefenseType = (byte)_ecs.Get<MonsterCombatComponent>(Entity).Defense,
            Time = seconds
        });
    }

    // ---- Seeing -------------------------------------------------------------------------------

    public AiSkill? GetSkill(int slot)
    {
        if (slot is < 0 or >= AiProfile.SkillSlots || Behavior.Skills[slot] == 0)
        {
            return null;
        }

        return _manager.Services?.Behaviors.GetSkill(Behavior.Skills[slot]);
    }

    /// <summary>
    /// The enemies of a monster are the players and the helpers that fight beside them.
    /// </summary>
    public virtual AiTarget? GetTarget(int id)
    {
        return _manager.FindPlayer(id) ?? _manager.Helpers.FindTarget(id);
    }

    public virtual AiTarget? FindEnemy(float distance)
    {
        var player = _manager.NearestPlayer(Position, distance);

        return _manager.Helpers.Count == 0 ? player : _manager.Helpers.Nearest(this, distance, player);
    }

    /// <summary>
    /// The enemies a monster counts are the monsters of a side at war with its own: the helpers of the players.
    /// </summary>
    public virtual int CountEnemies(float distance)
    {
        return _manager.Helpers.CountFoes(this, distance);
    }

    public int CountUsers(float distance)
    {
        return _manager.CountPlayers(Position, distance);
    }

    public int CountSectorMonsters()
    {
        return _manager.CountMonsters(_ecs.Get<NonPlayableCreatureComponent>(Entity).Sector);
    }

    public int CountSpawnedMonsters() => 0;

    // ---- Attacking ----------------------------------------------------------------------------

    public float PlayAction(string name)
    {
        var length = _actions.GetAnimationLength(Behavior.ActionFile, name);

        if (length <= 0f)
        {
            return 0f;
        }

        _idleAnimation = _actions.GetAnimationIndex(Behavior.ActionFile, name);
        _idleEnd = _manager.Now + length;

        Broadcast(new MovementIdleBtResponse
        {
            Actor = Actor,
            Position = Position,
            Yaw = Yaw,
            Animation = _idleAnimation,
            DefaultStep = 0,
            Delay = -1f
        });

        return length;
    }

    public virtual bool UseSkill(AiSkill skill, AiTarget target)
    {
        var now = _manager.Now;
        var duration = _actions.GetDuration(skill.Id, Behavior.ActionFile);

        if (duration <= 0f)
        {
            duration = _manager.Services?.Behaviors.GetSkillDuration(skill.Id) ?? 0f;
        }

        if (duration <= 0f)
        {
            duration = AttackDefaultLength;
        }

        StopMoving(true);

        FaceTo(target.Position);

        _attackEnd = now + duration;
        _pending.Clear();

        foreach (var hit in _actions.GetHits(skill.Id, Behavior.ActionFile))
        {
            _pending.Add(new PendingHit(now + hit.Time, hit, skill.Id));
        }

        foreach (var shot in _actions.GetShots(skill.Id, Behavior.ActionFile))
        {
            _pending.Add(new PendingHit(now + shot.Time, shot, skill.Id, true));
        }

        _pending.Sort((first, second) => first.Time.CompareTo(second.Time));

        _hidings.Clear();

        foreach (var hiding in _actions.GetHidings(skill.Id, Behavior.ActionFile))
        {
            // The kinds that wait for other monsters to die are not followed.
            if (hiding.Event.ConditionType is InvisibleEvent.ForTime or InvisibleEvent.UntilHurt)
            {
                _hidings.Add((now + hiding.Time, hiding.Event));
            }
        }

        Broadcast(new SkillActiveBroadcastResponse
        {
            Skill = default(ActiveSkillValue) with
            {
                Actor = Actor,
                Skill = skill.Id,
                Position = new SkillPositionValue { Position = Position, Angle = Yaw, MotionClass = NoMotion },
                RandomTrapIndex = _manager.Random.Next(1, int.MaxValue)
            }
        });

        return true;
    }

    private void ResolveHits(double now)
    {
        while (_pending.Count > 0 && _pending[0].Time <= now)
        {
            var pending = _pending[0];

            _pending.RemoveAt(0);

            if (pending.Shot)
            {
                Fire(pending.Skill, pending.Hit.Trigger);

                continue;
            }

            Strike(pending.Skill, pending.Hit.Trigger, new AttackArea(pending.Hit.Trigger, Position, Yaw), null, false);
        }
    }

    /// <summary>
    /// A blow of the monster lands on an area, and on what a projectile ran into: the players and the helpers in it are hit.
    /// </summary>
    protected virtual void Strike(int skill, AttackTrigger trigger, AttackArea area, int? collider, bool colliderOnly)
    {
        IReadOnlyList<Entity> victims = colliderOnly ? [] : _manager.PlayersTouching(area);

        if (collider is { } key && _manager.FindPlayerEntity(key) is { } hit && !victims.Contains(hit))
        {
            victims = [.. victims, hit];
        }

        _manager.HitPlayers(this, skill, trigger, victims);

        if (_manager.Helpers.Count > 0)
        {
            _manager.Helpers.HitBy(this, skill, trigger, area, collider, colliderOnly);
        }
    }

    /// <summary>
    /// What the projectiles of the monster can run into.
    /// </summary>
    protected virtual FlightTarget[] FlightTargets()
    {
        var players = _manager.GetPlayerTargets();

        return _manager.Helpers.Count == 0 ? players : [.. players, .. _manager.Helpers.FlightTargets(this)];
    }

    /// <summary>
    /// Sets a projectile off in the direction the monster looks. A monster has no camera: whatever the event says about one, the
    /// shot goes the way the monster faces, turned and lowered by the angles of the event.
    /// </summary>
    private void Fire(int skill, AttackTrigger trigger)
    {
        if (trigger.Projectile is null)
        {
            return;
        }

        var right = new Vector3(AiMath.Direction(Yaw - 90f), 0f);
        var flight = ProjectileFlight.LaunchByFacing(trigger, Position, right, Yaw);

        _flights.Add(new Flight(flight, trigger, skill));

        if (!_actions.IsFlownByClient(skill))
        {
            Broadcast(new SkillProjectileBroadcastResponse
            {
                Owner = Actor,
                Projectile = default(ProjectileShotValue) with
                {
                    Skill = skill,
                    Attack = (short)trigger.Id,
                    Position = flight.Position,
                    Direction = flight.Direction,
                    Right = right,
                    Yaw = Yaw,
                    Session = ++_shotSession
                }
            });
        }
    }

    private void Fly(float elapsed)
    {
        if (_flights.Count == 0)
        {
            return;
        }

        var targets = FlightTargets();

        // In place: a projectile that is over leaves the list after the round.
        for (var index = 0; index < _flights.Count; index++)
        {
            var flight = _flights[index];

            foreach (var explosion in flight.Projectile.Advance(elapsed, targets))
            {
                var area = new AttackArea(flight.Trigger, explosion.Position, Yaw);

                Strike(flight.Skill, flight.Trigger, area, explosion.Collider, flight.Trigger.Projectile is { TargetOnly: true });
            }
        }

        _flights.RemoveAll(static e => e.Projectile.IsOver);
    }

    // ---- Hiding -------------------------------------------------------------------------------

    /// <summary>
    /// The monster is gone from sight and cannot be hit or aimed at.
    /// </summary>
    public bool IsHidden => _hidden is not null;

    private void ResolveHidings(double now)
    {
        if (_hidden is { } hidden && (hidden.ConditionType == InvisibleEvent.ForTime ? now >= _hiddenEnd : IsHurtTo(hidden.Value)))
        {
            _hidden = null;

            Broadcast(new MonsterInvisibleResponse { Actor = Actor, Invisible = false });
        }

        while (_hidings.Count > 0 && _hidings[0].Time <= now)
        {
            var hide = _hidings[0].Event;

            _hidings.RemoveAt(0);

            _hidden = hide;
            _hiddenEnd = now + hide.Value * 0.001;

            Broadcast(new MonsterInvisibleResponse { Actor = Actor, Invisible = true, Flags = hide.Flags, Type = hide.ConditionType, Value = hide.Value });
        }
    }

    /// <summary>
    /// Whether the life of the monster is down to a share, in thousandths.
    /// </summary>
    private bool IsHurtTo(int thousandths)
    {
        return MaxHealth > 0 && (long)Health * 1000 / MaxHealth <= thousandths;
    }

    // ---- Telling the clients ------------------------------------------------------------------

    private void SendMove(double now)
    {
        if (_destination is not { } end)
        {
            return;
        }

        // The client walks to the next point of the way; it hears of the next one when it gets there.
        var destination = _route.Count > 0 ? _route[0] : end;

        _lastMoveSent = now;
        _lastYawSent = Yaw;

        Broadcast(new MovementMoveBtResponse
        {
            Actor = Actor,
            Map = _ecs.Get<WorldComponent>(Entity).Map,
            Pos = Position,
            Yaw = Yaw,
            TargetPosX = destination.X,
            TargetPosY = destination.Y,
            RunBit = (byte)(_run ? 1 : 0),
            Pitch = 0f,
            MoveSpeed = Speed,
            ChangeMotion = 0
        });
    }

    private void SendStop()
    {
        _lastYawSent = Yaw;

        Broadcast(new MovementStopBtResponse
        {
            Actor = Actor,
            MapId = _ecs.Get<WorldComponent>(Entity).Map,
            PosX = Position.X,
            PosY = Position.Y,
            PosZ = Position.Z,
            Yaw = Yaw,
            Pitch = 0f
        });
    }

    protected void Broadcast(OpenWorker.Hotspot.Messages.Abstractions.IResponseHotspotMessage message)
    {
        if (IsRevealed && !IsSilencedByReaction(message))
        {
            _manager.Broadcast(message);
        }
    }
}
