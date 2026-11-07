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
internal sealed class MonsterBody : IAiHost
{
    /// <summary>
    /// How often a client hears about a monster that keeps walking, in seconds.
    /// </summary>
    private const double MoveResend = 0.6;

    private const float TurnToReport = 1f;
    private const float Arrived = 3f;
    private const float AttackDefaultLength = 1f;
    private const short NoMotion = -1;

    private readonly World _ecs;
    private readonly LuaCreatureManager _manager;
    private readonly SkillActionProvider _actions;
    private readonly List<PendingHit> _pending = [];

    private Vector3? _destination;
    private bool _run;
    private bool _battlePose;
    private bool _gazing;
    private double _attackEnd;
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
        HomePosition = home;
        _born = manager.Now;

        Machine = new AiMachine(profile, this, manager.Random);
        Machine.StateChanged += (old, state) => manager.Logger?.LogDebug("Monster {Monster} ({Actor}): {Old} -> {New}", behavior.Id, entity.Id, old, state);
    }

    public Entity Entity { get; }
    public MonsterBehavior Behavior { get; }
    public AiMachine Machine { get; }

    /// <summary>
    /// The clients know the monster and can be told what it does.
    /// </summary>
    public bool IsRevealed { get; private set; }

    private ActorComponent Actor => _ecs.Get<ActorComponent>(Entity);

    private readonly record struct PendingHit(double Time, SkillHit Hit, int Skill);

    // ---- The body in the world ----------------------------------------------------------------

    public Vector3 Position => _ecs.Get<WorldComponent>(Entity).Position;

    public Vector3 HomePosition { get; }

    public float Yaw => _ecs.Get<WorldComponent>(Entity).Rotation;

    public float Radius => _ecs.Get<MonsterCombatComponent>(Entity).BodyRadius;

    public int Health => _ecs.Get<CreatureComponent>(Entity).Health;

    public int MaxHealth => (int)_ecs.Get<MonsterCombatComponent>(Entity).Stats.MaxHealth;

    public bool IsDead => !_ecs.IsAlive(Entity) || Health <= 0;

    public bool IsAttacking => _manager.Now < _attackEnd;

    public bool CanMove => !IsAttacking && !Behavior.IsRooted && (_battlePose || !Behavior.WalksOnlyInBattle);

    public bool IsRooted => Behavior.IsRooted;

    public bool CanAttack => !IsAttacking;

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

    public void Update(double now, float elapsed)
    {
        if (!IsRevealed || IsDead)
        {
            return;
        }

        Advance(now, elapsed);
        ResolveHits(now);

        Machine.Update(elapsed);
    }

    // ---- Movement -----------------------------------------------------------------------------

    private void Advance(double now, float elapsed)
    {
        if (_destination is not { } destination)
        {
            return;
        }

        var position = Position;
        var offset = new Vector2(destination.X - position.X, destination.Y - position.Y);
        var distance = offset.Length();
        var step = Speed * elapsed;

        if (distance <= Math.Max(step, Arrived))
        {
            Place(new Vector3(destination.X, destination.Y, position.Z), Yaw);

            _destination = null;

            SendStop();

            return;
        }

        var direction = offset / distance;
        var yaw = AiMath.YawOf(direction.X, direction.Y);

        Place(new Vector3(position.X + direction.X * step, position.Y + direction.Y * step, position.Z), yaw);

        if (now - _lastMoveSent >= MoveResend)
        {
            SendMove(now);
        }
    }

    private void Place(Vector3 position, float yaw)
    {
        _ecs.Set(Entity, _ecs.Get<WorldComponent>(Entity) with { Position = position, Rotation = yaw });
    }

    public void Stop(bool send)
    {
        var moved = _destination is not null;

        _destination = null;

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

        if (_manager.Navigation.Clamp(Position, destination) is not { } reachable)
        {
            return false;
        }

        _run = run;
        _destination = reachable;

        var position = Position;

        Place(position, AiMath.YawTo(position, reachable));

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
            Stop(true);
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

    // ---- Seeing -------------------------------------------------------------------------------

    public AiSkill? GetSkill(int slot)
    {
        if (slot is < 0 or >= AiProfile.SkillSlots || Behavior.Skills[slot] == 0)
        {
            return null;
        }

        return _manager.Services?.Behaviors.GetSkill(Behavior.Skills[slot]);
    }

    public AiTarget? GetTarget(int id)
    {
        return _manager.FindPlayer(id);
    }

    public AiTarget? FindEnemy(float distance)
    {
        return _manager.NearestPlayer(Position, distance);
    }

    public int CountEnemies(float distance)
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

        Broadcast(new MovementIdleBtResponse
        {
            Actor = Actor,
            Position = Position,
            Yaw = Yaw,
            Animation = _actions.GetAnimationIndex(Behavior.ActionFile, name),
            DefaultStep = 0,
            Delay = -1f
        });

        return length;
    }

    public bool UseSkill(AiSkill skill, AiTarget target)
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

        Stop(true);

        FaceTo(target.Position);

        _attackEnd = now + duration;
        _pending.Clear();

        foreach (var hit in _actions.GetHits(skill.Id, Behavior.ActionFile))
        {
            _pending.Add(new PendingHit(now + hit.Time, hit, skill.Id));
        }

        _pending.Sort((first, second) => first.Time.CompareTo(second.Time));

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

            var area = new AttackArea(pending.Hit.Trigger, Position, Yaw);

            _manager.HitPlayers(this, pending.Skill, pending.Hit.Trigger, _manager.PlayersTouching(area));
        }
    }

    // ---- Telling the clients ------------------------------------------------------------------

    private void SendMove(double now)
    {
        if (_destination is not { } destination)
        {
            return;
        }

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

    private void Broadcast(OpenWorker.Hotspot.Messages.Abstractions.IResponseHotspotMessage message)
    {
        if (IsRevealed)
        {
            _manager.Broadcast(message);
        }
    }
}
