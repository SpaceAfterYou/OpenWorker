using System.Globalization;
using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.Skill.Responses;
using OpenWorker.Hotspot.Modules.Skill.Types;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Ai;

namespace OpenWorker.Lua;

/// <summary>
/// The escort: a monster the script names walks the escort points of the map from the one its spawn box gives, by its own
/// mind and fighting the monsters on the way, and the monsters go for it while the players cannot hit it. Each point it passes is told to the clients and runs the function the
/// point names; at the last one it leaves the maze, and a moment later the condition the script set is done for everyone in
/// the maze who works on it. If it falls, the quest of the party fails for everyone who has the escort's episode.
/// <para>
/// The quest of the party: the script names an episode, the condition it starts with and the sector it starts in. A player
/// who loads the maze in that sector while working on that condition starts it; the boxes of the map that name
/// <c>OnPartyQuestEvent</c> then tell the script the sector the player is in, once for each sector. A player of the quest who
/// leaves the maze fails it; when the last of them is gone the escorted falls.
/// </para>
/// </summary>
public partial class LuaMaze
{
    /// <summary>
    /// How long after the arrival the condition of the escort is done.
    /// </summary>
    private const float EscortConditionDelay = 5f;

    private const string PartyQuestFunction = "OnPartyQuestEvent";

    private int _escortEpisode;
    private int _escortMonster;
    private int _escortCondition;

    /// <summary>
    /// The escorted monster on its way and the point it heads for.
    /// </summary>
    private (Entity Monster, EscortBody? Body, EscortPoint Point)? _escort;

    /// <summary>
    /// The monsters of the escort's kind that have been looked at, so that one with no way is not looked at again.
    /// </summary>
    private readonly HashSet<Entity> _escortSeen = [];

    private int _partyQuest;
    private int _partyStartCondition;
    private int _partyStartSector;
    private PartyQuestState _partyState;
    private readonly HashSet<Entity> _partyPlayers = [];
    private readonly HashSet<int> _partyConditions = [];
    private readonly HashSet<int> _partySectorsTold = [];

    private enum PartyQuestState
    {
        None,
        Running,
        Over
    }

    /// <summary>
    /// The monster of the table id <paramref name="monster"/> is escorted, for the episode. The id may come as a number or
    /// as text; anything else names no monster.
    /// </summary>
    [LuaMember]
    public void SetEscortMonster(int episode, LuaValue monster, LuaValue animation)
    {
        _escortEpisode = episode;
        _escortMonster = monster.TryRead<double>(out var number) && double.IsFinite(number) && number is >= int.MinValue and <= int.MaxValue
            ? (int)number
            : monster.TryRead<string>(out var text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

        Logger.CallSetEscortMonster(episode, _escortMonster);

        if (_escortMonster != 0)
        {
            creatureManager.UseServerStats(_escortMonster);
        }
    }

    /// <summary>
    /// The condition that is done when the escorted arrives.
    /// </summary>
    [LuaMember]
    public void SetEscortCondition(int condition)
    {
        Logger.CallSetEscortCondition(condition);

        _escortCondition = condition;
    }

    [LuaMember]
    public void InitPartyQuest(int quest, int condition, int sector)
    {
        Logger.CallInitPartyQuest(quest, condition, sector);

        _partyQuest = quest;
        _partyStartCondition = condition;
        _partyStartSector = sector;
    }

    /// <summary>
    /// A player steps into a box that runs a function of the script. A box of the quest of the party tells the script the
    /// sector the player is in instead, once for each sector, and only while the quest runs.
    /// </summary>
    [LuaIgnoreMember]
    public async ValueTask RunFunctionBoxAsync(Entity player, string function, int box)
    {
        if (function != PartyQuestFunction)
        {
            await State.OnExecuteLuaFunction(function, box, this);

            return;
        }

        if (_partyState != PartyQuestState.Running || !Ecs.IsAlive(player) || !Ecs.Has<WorldComponent>(player))
        {
            return;
        }

        if (SectorAt(Ecs.Get<WorldComponent>(player).Position) is { } sector && _partySectorsTold.Add(sector.Id))
        {
            await State.OnPartyQuestEvent(sector.Id, this);
        }
    }

    /// <summary>
    /// A player has loaded the maze. One who works on the first condition of the quest of the party starts it in the sector
    /// it starts in, or joins it elsewhere when it runs and takes the conditions done so far; one who has the episode at
    /// another condition fails it.
    /// </summary>
    private void JoinPartyQuest(Entity player)
    {
        if (_partyQuest == 0 || !Ecs.IsAlive(player) || !Ecs.Has<WorldComponent>(player))
        {
            return;
        }

        if (!questManager.HasOpenCondition(player, _partyStartCondition))
        {
            if (questManager.HasActiveEpisode(player, _partyQuest))
            {
                Run(FailPartyQuestAsync(player));
            }

            return;
        }

        var inStart = SectorAt(Ecs.Get<WorldComponent>(player).Position)?.Id == _partyStartSector;

        if (_partyState != PartyQuestState.Over && inStart)
        {
            _partyState = PartyQuestState.Running;
            _partyPlayers.Add(player);

            return;
        }

        if (_partyState != PartyQuestState.Running || inStart)
        {
            return;
        }

        var joined = false;

        foreach (var condition in _partyConditions)
        {
            if (questManager.HasOpenCondition(player, condition))
            {
                joined = true;
                questManager.CompleteCondition(player, condition);
            }
        }

        if (joined)
        {
            _partyPlayers.Add(player);
        }
    }

    /// <summary>
    /// A player of the quest of the party leaves the maze and fails it. When the last of them is gone the quest is over,
    /// and the escorted falls.
    /// </summary>
    private void LeavePartyQuest(Entity player)
    {
        if (_partyPlayers.Remove(player))
        {
            Run(FailPartyQuestAsync(player));
        }

        if (_partyState != PartyQuestState.Running || _partyPlayers.Count > 0)
        {
            return;
        }

        _partyState = PartyQuestState.Over;

        DropEscorted();
    }

    /// <summary>
    /// The escorted on its way falls now, as a game master tells it to; the next step of the maze takes its fall. False when
    /// no escort is on its way or the player is not in this maze.
    /// </summary>
    [LuaIgnoreMember]
    public bool FellEscorted(Entity player)
    {
        return !Closed && Find(player) is not null && DropEscorted();
    }

    /// <summary>
    /// The life of the escorted on its way goes to nothing, and the clients see it fall.
    /// </summary>
    private bool DropEscorted()
    {
        if (_escort is not { } escort || !Ecs.IsAlive(escort.Monster))
        {
            return false;
        }

        Ecs.Set(escort.Monster, Ecs.Get<CreatureComponent>(escort.Monster) with { Health = 0 });
        creatureManager.Broadcast(new MonsterDieResponse { Actor = Ecs.Get<ActorComponent>(escort.Monster) });

        return true;
    }

    private async ValueTask FailPartyQuestAsync(Entity player)
    {
        await questManager.FailAsync(player, _partyQuest);
    }

    private void ResetPartyQuest()
    {
        _partyQuest = 0;
        _partyStartCondition = 0;
        _partyStartSector = 0;
        _partyState = PartyQuestState.None;
        _partyConditions.Clear();
        _partyPlayers.Clear();
    }

    /// <summary>
    /// One step of the escort: the monster the script named starts its way once the clients see it, its blows land, and it
    /// passes its point, arrives or falls. Its own mind walks it to the point and fights on the way.
    /// </summary>
    private void StepEscort()
    {
        if (_escortMonster == 0)
        {
            return;
        }

        if (_escort is null)
        {
            FindEscorted();
        }

        if (_escort is not { } escort)
        {
            return;
        }

        if (escort.Body is { Strikes.Count: > 0 } body)
        {
            var strikes = body.Strikes.ToArray();

            body.Strikes.Clear();

            foreach (var strike in strikes)
            {
                Run(EscortStrikeAsync(strike));
            }
        }

        if (creatureManager.Helpers.Defended.Health(escort.Monster) <= 0)
        {
            EscortFalls(escort.Monster, escort.Point);

            return;
        }

        if (!Touches(escort.Point, Ecs.Get<WorldComponent>(escort.Monster).Position))
        {
            return;
        }

        escort.Body?.StopMoving(true);

        var next = escort.Point.NextPointList[0];

        if (next == 0)
        {
            EscortArrives(escort.Monster, escort.Point);

            return;
        }

        TellEscort(escort.Point, EscortStatus.Passed);

        // A way that goes on to a point that is not an escort point stops there.
        if (FindEscortPoint(next) is not { } point)
        {
            _escort = null;

            return;
        }

        _escort = escort with { Point = point };

        escort.Body?.Follow(Centre(point));
    }

    private void FindEscorted()
    {
        foreach (var creature in creatureManager.Living)
        {
            if (!Ecs.IsAlive(creature) || _escortSeen.Contains(creature) || Ecs.Get<ActorComponent>(creature).Type != ActorType.Monster ||
                Ecs.Get<CreatureComponent>(creature).Identifier != _escortMonster)
            {
                continue;
            }

            // The way starts when the clients see the monster.
            if (creatureManager.BodyOf(creature) is { IsRevealed: false })
            {
                continue;
            }

            _escortSeen.Add(creature);

            if (FindEscortPoint(Ecs.Get<NonPlayableCreatureComponent>(creature).Waypoint) is not { } point)
            {
                continue;
            }

            creatureManager.Helpers.Defended.Add(creature);

            var body = creatureManager.Escort(creature);

            body?.Follow(Centre(point));

            _escort = (creature, body, point);

            TellEscort(point, EscortStatus.Started);

            return;
        }
    }

    /// <summary>
    /// The monsters a blow of the escorted hit take it as from any fighter; those that die are nobody's kills, and their
    /// sectors are looked at.
    /// </summary>
    private async ValueTask EscortStrikeAsync(EscortStrike strike)
    {
        if (Closed || !Ecs.IsAlive(strike.Escorted))
        {
            return;
        }

        var standing = strike.Targets.Select((target, i) => (Target: target, Part: strike.Parts[i])).Where(e => Ecs.IsAlive(e.Target) && Ecs.Get<CreatureComponent>(e.Target).Health > 0).ToArray();
        var targets = standing.Select(e => e.Target).ToArray();

        if (targets.Length == 0)
        {
            return;
        }

        var attacker = Ecs.Get<WorldComponent>(strike.Escorted);
        var stats = _buffs.StatsOf(strike.Escorted, Ecs.Get<MonsterCombatComponent>(strike.Escorted).Stats);
        var skill = combat.GetSkill(strike.Skill);
        var hits = standing.Select(e => ApplyDamage(strike.Escorted, e.Target, e.Part, stats, skill, strike.Trigger.DamageRate, strike.Trigger, strike.Skill, HitKinds.Of(strike.Trigger))).ToArray();

        creatureManager.TellHitSlow(hits, strike.Trigger);

        creatureManager.Broadcast(new SkillActionBroadcastResponse
        {
            SkillDmgValueList = hits,
            SkillAction = default(SkillActionValue) with
            {
                SkillActorInfo = default(SkillActorInfoValue) with
                {
                    UxActorId = Ecs.Get<ActorComponent>(strike.Escorted),
                    SkillPosInfo = default(SkillActionPosInfoValue) with
                    {
                        Position = attacker.Position,
                        Angle = attacker.Rotation,
                        MotionClass = -1
                    }
                },
                Skill = strike.Skill,
                TriggerIndex = (short)strike.Trigger.Id,
                ContinuousHit = 1
            }
        });

        _buffs.GiveFromHit(strike.Escorted, strike.Trigger, targets);

        var dead = targets.Where(e => Ecs.Get<CreatureComponent>(e).Health == 0).Select(RemoveDead).ToArray();

        foreach (var (identifier, place, _) in dead)
        {
            if (Closed)
            {
                return;
            }

            await State.OnDie(identifier, place.SpawnBox, this);
        }

        foreach (var sector in dead.Select(e => e.Place.Sector).Distinct())
        {
            if (Closed)
            {
                return;
            }

            await CheckSectorAsync(sector);
        }
    }

    /// <summary>
    /// The escorted has reached the last point: it leaves the maze, and a moment later the condition of the escort is done for
    /// everyone in the maze who works on it.
    /// </summary>
    private void EscortArrives(Entity monster, EscortPoint point)
    {
        _escort = null;

        TellEscort(point, EscortStatus.Arrived);

        creatureManager.Helpers.Defended.Remove(monster);

        Later(0f, () => DestroyAsync([monster], false));
        Later(EscortConditionDelay, CompleteEscortCondition);
    }

    private ValueTask CompleteEscortCondition()
    {
        var condition = _escortCondition;

        if (condition == 0)
        {
            return ValueTask.CompletedTask;
        }

        _partyConditions.Add(condition);

        foreach (var player in Present())
        {
            questManager.CompleteCondition(player, condition);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The escorted has fallen: everyone who has the escort's episode fails the quest of the party, which is over, and the light
    /// of its point goes out.
    /// </summary>
    private void EscortFalls(Entity monster, EscortPoint point)
    {
        _escort = null;

        var quest = _partyQuest;

        foreach (var player in Present())
        {
            if (quest != 0 && questManager.HasActiveEpisode(player, _escortEpisode))
            {
                Run(FailEscortAsync(player, quest));
            }
        }

        ResetPartyQuest();

        Logger.EscortStatus(_escortMonster, point.Id, EscortStatus.Ended);
        creatureManager.Broadcast(new MazeEscortStatusResponse { Point = point.Id, Status = EscortStatus.Ended });

        creatureManager.Helpers.Defended.Remove(monster);

        if (Ecs.IsAlive(monster))
        {
            creatureManager.Remove(monster);
        }
    }

    private async ValueTask FailEscortAsync(Entity player, int quest)
    {
        await questManager.FailAsync(player, quest);
    }

    /// <summary>
    /// The clients hear what became of the escort at the point. A point passed or reached runs the function it names first.
    /// </summary>
    private void TellEscort(EscortPoint point, EscortStatus status)
    {
        Logger.EscortStatus(_escortMonster, point.Id, status);

        if (status != EscortStatus.Started && point.Function.Length > 0)
        {
            var function = point.Function;
            var id = point.Id;

            Later(0f, () => State.OnExecuteLuaFunction(function, id, this));
        }

        creatureManager.Broadcast(new MazeEscortStatusResponse { Point = point.Id, Status = status });
    }

    private EscortPoint? FindEscortPoint(int id)
    {
        foreach (var point in Batch.EventPoint.EscortPoints)
        {
            if (point.Id == id && id != 0)
            {
                return point;
            }
        }

        return null;
    }

    private static Vector3 Centre(EscortPoint point) => (point.PosTopLeft + point.PosBottomRight) / 2f;

    /// <summary>
    /// Whether the place lies in the box of the point, seen from above: the box is the size of the point around its centre,
    /// turned as the point is.
    /// </summary>
    private static bool Touches(EscortPoint point, Vector3 place)
    {
        var centre = Centre(point);
        var angle = -point.Rotation * MathF.PI / 180f;
        var offset = new Vector2(place.X - centre.X, place.Y - centre.Y);
        var local = new Vector2(offset.X * MathF.Cos(angle) - offset.Y * MathF.Sin(angle), offset.X * MathF.Sin(angle) + offset.Y * MathF.Cos(angle));

        return Math.Abs(local.X) <= Math.Abs(point.Size.X) / 2f && Math.Abs(local.Y) <= Math.Abs(point.Size.Y) / 2f;
    }
}
