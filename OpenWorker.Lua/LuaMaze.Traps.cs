using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Mapping;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;

namespace OpenWorker.Lua;

public partial class LuaMaze
{
    /// <summary>
    /// How often a trap of the map looks at what is in it, in seconds of game time.
    /// </summary>
    private const float TrapLook = 0.1f;

    /// <summary>
    /// The way a trap of the map is told to the client to point: along -Y, as anything with no yaw.
    /// </summary>
    private static readonly Vector3 TrapDirection = new(0f, -1f, 0f);

    private readonly List<MapTrap> _mapTraps = [];
    private readonly Dictionary<int, MapTrap> _numberedTraps = [];
    private readonly Dictionary<int, List<MapTrap>> _trapGroups = [];
    private readonly HashSet<Fighter> _sawSceneAttacker = [];
    private Entity? _sceneAttacker;
    private uint _trapSession;

    /// <summary>
    /// The monster that owns the traps the script lays, from the server's table of the maze; zero for a maze without one.
    /// </summary>
    public int SceneAttacker { get; init; }

    /// <summary>
    /// The traps of the map that still live.
    /// </summary>
    internal IReadOnlyList<MapTrap> MapTraps => _mapTraps;

    /// <summary>
    /// The trap the script laid under the number dies after <paramref name="time"/> seconds, and the number is free again.
    /// </summary>
    [LuaMember]
    public void SetTrapLifeTime(int index, float time)
    {
        if (_numberedTraps.Remove(index, out var trap))
        {
            trap.LiveFor(time);
        }
    }

    /// <summary>
    /// Every trap the script laid in the group dies after <paramref name="time"/> seconds, and the group is forgotten.
    /// </summary>
    [LuaMember]
    public void SetTrapLifeTimeGroup(int group, float time)
    {
        if (_trapGroups.Remove(group, out var traps))
        {
            foreach (var trap in traps)
            {
                trap.LiveFor(time);
            }
        }
    }

    /// <summary>
    /// Lays a trap of the scene attacker at a place. A number keeps it for <see cref="SetTrapLifeTime"/>, unless another trap
    /// holds the number already; a group keeps it with the others of the group.
    /// </summary>
    internal MapTrap? LayTrap(string function, Vector3 position, int skill, int? number = null)
    {
        return Lay(function, position, null, skill, number, null);
    }

    /// <summary>
    /// Lays a trap of the scene attacker in the middle of a position box of the map, facing the way the box faces.
    /// </summary>
    internal MapTrap? LayTrapInBox(string function, int box, int skill, int? number = null, int? group = null)
    {
        if (Batch.EventBox.CommonPositions.Find(box) is not { } found)
        {
            Logger.TrapWithoutBox(skill, box);

            return null;
        }

        return Lay(function, Vector3.Lerp(found.PosTopLeft, found.PosBottomRight, 0.5f), found.Rotation, skill, number, group);
    }

    private MapTrap? Lay(string function, Vector3 position, float? yaw, int skill, int? number, int? group)
    {
        if (Closed || !float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
        {
            return null;
        }

        if (SceneAttackerOf(function) is not { } owner || TrapAttackOf(skill) is not { } trigger)
        {
            return null;
        }

        var trap = new MapTrap(skill, trigger, position, yaw, ++_trapSession);

        _mapTraps.Add(trap);

        if (number is { } key)
        {
            _numberedTraps.TryAdd(key, trap);
        }

        if (group is { } kept)
        {
            if (!_trapGroups.TryGetValue(kept, out var traps))
            {
                _trapGroups.Add(kept, traps = []);
            }

            traps.Add(trap);
        }

        Logger.TrapLaid(skill, position.X, position.Y, position.Z);

        ShowSceneAttacker(owner);

        creatureManager.Broadcast(new SkillProjectileBroadcastResponse
        {
            Owner = Ecs.Get<ActorComponent>(owner),
            Projectile = default(ProjectileShotValue) with
            {
                Skill = skill,
                Attack = (short)trigger.Id,
                Position = position,
                Direction = TrapDirection,
                Session = trap.Session
            }
        });

        _schedule.Schedule(Wait(TrapLook), () => LookFromTrap(owner, trap));

        return trap;
    }

    /// <summary>
    /// The trap looks at what is in it: the players it reaches take its blow as from a monster, the monsters it reaches its buff.
    /// </summary>
    private void LookFromTrap(Entity owner, MapTrap trap)
    {
        if (Closed || !Ecs.IsAlive(owner))
        {
            return;
        }

        var hit = trap.Step(TrapLook, TargetsIn(trap));

        if (hit.Count > 0)
        {
            ShowSceneAttacker(owner);

            var players = hit.Where(e => Ecs.Get<ActorComponent>(e).Type == ActorType.User).ToArray();
            var monsters = hit.Except(players).ToArray();

            if (players.Length > 0)
            {
                creatureManager.HitPlayers(owner, trap.Skill, trap.Trigger, players, trap.Position);
            }

            if (monsters.Length > 0)
            {
                _buffs.GiveFromHit(owner, trap.Trigger, monsters);
            }
        }

        if (!trap.Active)
        {
            _mapTraps.Remove(trap);

            return;
        }

        _schedule.Schedule(Wait(TrapLook), () => LookFromTrap(owner, trap));
    }

    /// <summary>
    /// The players and the monsters of the maze in an area of a trap, those the trap reaches.
    /// </summary>
    private Func<AttackArea, IReadOnlyList<Entity>> TargetsIn(MapTrap trap)
    {
        return area =>
        {
            var players = trap.ReachesPlayers ? creatureManager.PlayersTouching(area) : [];

            if (!trap.ReachesMonsters)
            {
                return players;
            }

            var monsters = creatureManager.FindMonsters((place, body) => area.Touches(place, body.BodyRadius, body.BodyHeight));

            return [.. players, .. monsters];
        };
    }

    /// <summary>
    /// The attack a trap of the skill is made of: the first of the skill in the action file of the scene attacker.
    /// </summary>
    private AttackTrigger? TrapAttackOf(int skill)
    {
        var file = creatureManager.Services?.Behaviors.Get(SceneAttacker)?.ActionFile ?? string.Empty;
        var first = actions.GetHits(skill, file).Concat(actions.GetShots(skill, file)).OrderBy(e => e.Time).FirstOrDefault();

        if (first == default)
        {
            Logger.TrapWithoutAttack(skill, file);

            return null;
        }

        return first.Trigger;
    }

    /// <summary>
    /// The scene attacker of the maze, made the first time a trap needs it: it stands at the origin of the map, invincible and
    /// without a mind, and nothing but its traps knows it.
    /// </summary>
    private Entity? SceneAttackerOf(string function)
    {
        if (_sceneAttacker is { } made)
        {
            return made;
        }

        if (SceneAttacker <= 0)
        {
            NotDone(function);

            return null;
        }

        var body = combat.GetMonster(SceneAttacker);

        _sceneAttacker = Ecs.Create(
            new ActorComponent { Identifier = creatureManager.NextActor(), Type = ActorType.Monster },
            new CreatureComponent { Identifier = SceneAttacker, Level = body.Stats.Level, Health = Math.Max(1, (int)body.Stats.MaxHealth) },
            new NonPlayableCreatureComponent(),
            new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 },
            body with { Defense = DefenseType.Invincible });

        return _sceneAttacker;
    }

    /// <summary>
    /// The players who have loaded the maze and have not seen the scene attacker yet are shown it: the client finds the attack
    /// of a trap in the action file of its owner, and draws no trap of an owner it does not know.
    /// </summary>
    private void ShowSceneAttacker(Entity owner)
    {
        STMonsterInfo? shown = null;

        foreach (var member in _members)
        {
            if (!member.Loaded || member.Gone || !TrySession(member.Player, out var session) || !_sawSceneAttacker.Add(member))
            {
                continue;
            }

            shown ??= MonsterSnapshotMapper.ToStMonsterInfo(Ecs, owner, false);

            session.Send(new WorldOtherInfosMonsterResponse { Monsters = [shown.Value] });
        }
    }

    /// <summary>
    /// The scene attacker leaves with the maze.
    /// </summary>
    private void RemoveSceneAttacker()
    {
        if (_sceneAttacker is { } owner && Ecs.IsAlive(owner))
        {
            Ecs.Destroy(owner);
        }

        _sceneAttacker = null;
        _mapTraps.Clear();
        _numberedTraps.Clear();
        _trapGroups.Clear();
    }
}
