using System.Globalization;
using System.Numerics;
using Arch.Core;
using Lua;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Hotspot.Modules.Maze.Responses;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Hotspot.Modules.Monster.Responses;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

/// <summary>
/// The escort: a monster the script names walks the escort points of the map from the one its spawn box gives, and the
/// monsters go for it while the players cannot hit it. Each point it passes is told to the clients and runs the function the
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
    private (Entity Monster, EscortPoint Point, bool Walking)? _escort;

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

        if (_escort is { } escort && Ecs.IsAlive(escort.Monster))
        {
            Ecs.Set(escort.Monster, Ecs.Get<CreatureComponent>(escort.Monster) with { Health = 0 });
            creatureManager.Broadcast(new MonsterDieResponse { Actor = Ecs.Get<ActorComponent>(escort.Monster) });
        }
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
    /// One step of the escort: the monster the script named starts its way once the clients see it, walks to its point, and
    /// passes it, arrives or falls.
    /// </summary>
    private void StepEscort(float seconds)
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

        if (creatureManager.Helpers.Defended.Health(escort.Monster) <= 0)
        {
            EscortFalls(escort.Monster, escort.Point);

            return;
        }

        var place = Ecs.Get<WorldComponent>(escort.Monster).Position;

        if (!Touches(escort.Point, place))
        {
            _escort = escort with { Walking = WalkTowards(escort.Monster, Centre(escort.Point), seconds, escort.Walking) };

            return;
        }

        if (escort.Walking)
        {
            creatureManager.Arrive(escort.Monster, place);
        }

        var next = escort.Point.NextPointList[0];

        if (next == 0)
        {
            EscortArrives(escort.Monster, escort.Point);

            return;
        }

        TellEscort(escort.Point, EscortStatus.Passed);

        // A way that goes on to a point that is not an escort point stops there.
        _escort = FindEscortPoint(next) is { } point ? (escort.Monster, point, false) : null;
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
            creatureManager.Helpers.Defended.Lead(creature);

            _escort = (creature, point, false);

            TellEscort(point, EscortStatus.Started);

            return;
        }
    }

    /// <summary>
    /// The escorted walks on towards the point, at the pace of townsfolk. The clients are told of a walk when it starts.
    /// </summary>
    private bool WalkTowards(Entity monster, Vector3 target, float seconds, bool walking)
    {
        var place = Ecs.Get<WorldComponent>(monster);
        var offset = new Vector2(target.X - place.Position.X, target.Y - place.Position.Y);
        var distance = offset.Length();

        if (distance < 1f || !float.IsFinite(distance))
        {
            return walking;
        }

        if (!walking)
        {
            creatureManager.Walk(monster, target);
        }

        var step = Math.Min(distance, LuaCreatureManager.NpcWalkSpeed * seconds);
        var position = place.Position + new Vector3(offset / distance * step, 0f);

        if (creatureManager.Navigation.TryGetFloor(position, out var height))
        {
            position.Z = height;
        }

        Ecs.Set(monster, Ecs.Get<WorldComponent>(monster) with { Position = position });

        return true;
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
