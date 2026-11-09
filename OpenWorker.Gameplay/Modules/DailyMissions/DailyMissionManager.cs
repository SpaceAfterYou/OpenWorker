using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.DailyMissions.Components;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.DailyMissions.Enums;
using OpenWorker.Hotspot.Modules.DailyMissions.Responses;
using OpenWorker.Hotspot.Modules.DailyMissions.Types;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Gameplay.Modules.Persistence;

namespace OpenWorker.Gameplay.Modules.DailyMissions;

/// <summary>
/// Every day a person is given a few missions for every district: to kill something, clear a maze or collect
/// something. The person accepts the ones it wants, and the progress counts while the mission is open.
/// </summary>
public sealed partial class DailyMissionManager(
    World world,
    PersonStateStore store,
    DailyMissionCatalog catalog,
    IRewardGranter rewards,
    TimeProvider time,
    IOptions<DailyMissionOptions> options,
    ILogger<DailyMissionManager> logger) : IMiscTick
{
    /// <summary>
    /// The mission is accepted, finished or not on the list of the person.
    /// </summary>
    private const ushort AlreadyAccepted = 54001;
    private const ushort AlreadyEnded = 54002;
    private const ushort NotAvailable = 54004;

    /// <summary>
    /// The tracker is full.
    /// </summary>
    private const ushort TooManyHelpers = 54006;

    private static readonly QueryDescription Everyone = new QueryDescription().WithAll<DailyMissionComponent>();

    /// <summary>
    /// The time of day the special missions are drawn again, in hours.
    /// </summary>
    private TimeSpan Reset => options.Value.ResetHour is { } hour ? TimeSpan.FromHours(hour) : DailyMissionRules.DefaultResetTime;

    private Random Random { get; } = new();

    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<DailyMissionBook>(person, PersonStateKind.DailyMission, cancellationToken) ?? new DailyMissionBook();

        book.Normalize(catalog);

        world.Set(player, new DailyMissionComponent { Person = person, Book = book });

        var now = time.GetLocalNow();
        var drawn = NeedsDraw(book, now);

        if (drawn)
        {
            DrawAll(book, now);
        }

        EndRunOut(book, now);

        SendList(player, book, now, false);

        if (drawn)
        {
            await SaveAsync(player, book, cancellationToken);
        }
    }

    /// <summary>
    /// The person accepts a mission.
    /// </summary>
    public async ValueTask AcceptAsync(Entity player, int mission, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        var entry = book.Find(mission);

        if (entry is null)
        {
            SendError(player, DailyMissionOpcode.Accept, NotAvailable);
            return;
        }

        if (entry.State is DailyMissionState.Fail or DailyMissionState.Success)
        {
            SendError(player, DailyMissionOpcode.Accept, AlreadyEnded);
            return;
        }

        if (entry.State == DailyMissionState.Accept)
        {
            SendError(player, DailyMissionOpcode.Accept, AlreadyAccepted);
            return;
        }

        if (!catalog.TryGet(mission, out var definition))
        {
            return;
        }

        var now = time.GetLocalNow();

        // A mission of the day is accepted all day, the others only while they are open.
        if (definition.Type != DailyMissionType.Special && !DailyMissionRules.IsOpen(definition, entry, now))
        {
            SendError(player, DailyMissionOpcode.Accept, NotAvailable);
            return;
        }

        entry.State = DailyMissionState.Accept;
        entry.Value = 0;
        entry.Accept = now;
        entry.Helper = book.HelperCount < DailyMissionRules.MaxHelpers;

        world.Get<ServerSessionComponent>(player)
            .Send(new DailyMissionAcceptResponse
            {
                Mission = mission,
                Helper = entry.Helper ? DailyMissionHelperState.Add : DailyMissionHelperState.Del
            });

        await SaveAsync(player, book, cancellationToken);
    }

    /// <summary>
    /// The person puts a mission in the tracker or takes it out.
    /// </summary>
    public async ValueTask SetHelperAsync(Entity player, int mission, DailyMissionHelperState helper, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book) || book.Find(mission) is not { } entry)
        {
            return;
        }

        var add = helper == DailyMissionHelperState.Add;

        if (add == entry.Helper)
        {
            return;
        }

        if (add && book.HelperCount >= DailyMissionRules.MaxHelpers)
        {
            SendError(player, DailyMissionOpcode.Helper, TooManyHelpers);
            return;
        }

        entry.Helper = add;

        world.Get<ServerSessionComponent>(player)
            .Send(new DailyMissionHelperResponse { Mission = mission, Helper = helper });

        await SaveAsync(player, book, cancellationToken);
    }

    /// <summary>
    /// The person has killed a monster. The conditions are the kinds of attack that were used, one bit for each
    /// <see cref="DailyMissionCondition"/>.
    /// </summary>
    public ValueTask OnMonsterKilledAsync(Entity player, int monster, uint conditions, CancellationToken cancellationToken)
    {
        // A kill without a known kind of attack is a plain one.
        conditions |= 1;

        return ProgressAsync(
            player,
            1,
            e => e.Finish == DailyMissionFinish.Kill && DailyMissionRules.CountsKill(e, catalog, monster, conditions),
            cancellationToken);
    }

    /// <summary>
    /// The person has cleared a maze, alone or with a party.
    /// </summary>
    public ValueTask OnMazeClearedAsync(Entity player, DailyMissionMazeClear clear, bool withParty, CancellationToken cancellationToken)
    {
        return ProgressAsync(
            player,
            1,
            e => e.Target == DailyMissionTarget.Maze &&
                 (e.Finish == DailyMissionFinish.MazeClear || withParty && e.Finish == DailyMissionFinish.PartyMazeClear) &&
                 DailyMissionRules.CountsMazeClear(e, clear),
            cancellationToken);
    }

    /// <summary>
    /// The person has got items.
    /// </summary>
    public ValueTask OnItemCollectedAsync(Entity player, int item, short count, CancellationToken cancellationToken)
    {
        return ProgressAsync(
            player,
            count,
            e => e.Finish == DailyMissionFinish.Collect && e.Target == DailyMissionTarget.Item && e.TargetId == item,
            cancellationToken);
    }

    /// <summary>
    /// Draws the missions again for the persons whose day has ended and ends the ones that have run out.
    /// </summary>
    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        world.Query(in Everyone, (Entity entity, ref DailyMissionComponent component) =>
        {
            if (component.Book is not null)
            {
                players.Add(entity);
            }
        });

        var now = time.GetLocalNow();

        foreach (var player in players)
        {
            if (!TryGet(player, out var book))
            {
                continue;
            }

            if (NeedsDraw(book, now))
            {
                DrawAll(book, now);
                SendList(player, book, now, true);
                await SaveAsync(player, book, cancellationToken);
                continue;
            }

            var ended = EndRunOut(book, now);

            if (ended.Count > 0)
            {
                SendUpdate(player, book, ended, now);
                await SaveAsync(player, book, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Draws the missions again whatever the time. A tool for testing.
    /// </summary>
    public async ValueTask RedrawAsync(Entity player, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        var now = time.GetLocalNow();

        DrawAll(book, now);
        SendList(player, book, now, true);

        await SaveAsync(player, book, cancellationToken);
    }

    private async ValueTask ProgressAsync(Entity player, short add, Func<DailyMissionDefinition, bool> counts, CancellationToken cancellationToken)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        var now = time.GetLocalNow();
        var changed = new List<DailyMissionEntry>();

        foreach (var entry in book.Missions.Where(e => e.State == DailyMissionState.Accept).ToArray())
        {
            if (!catalog.TryGet(entry.Mission, out var definition) ||
                !DailyMissionRules.IsOpen(definition, entry, now) ||
                !counts(definition))
            {
                continue;
            }

            entry.Value = (short)Math.Min(entry.Value + add, short.MaxValue);

            if (entry.Value < definition.MissionCount || await TryFinishAsync(player, entry, definition, cancellationToken))
            {
                changed.Add(entry);
            }
        }

        if (changed.Count == 0 || !world.IsAlive(player))
        {
            return;
        }

        SendUpdate(player, book, changed, now);

        await SaveAsync(player, book, cancellationToken);
    }

    // The reward goes first, so a mission whose reward cannot be given stays open and is tried again with the next step.
    private async ValueTask<bool> TryFinishAsync(Entity player, DailyMissionEntry entry, DailyMissionDefinition definition, CancellationToken cancellationToken)
    {
        if (!await rewards.TryGrantAsync(player, new Reward(definition.Rewards), RewardSource.DailyMission, cancellationToken))
        {
            LogRewardNotGiven(entry.Mission);
            return false;
        }

        entry.State = DailyMissionState.Success;
        entry.Helper = false;

        return true;
    }

    // The special missions are of a day, so the person gets new ones when the day is over.
    private bool NeedsDraw(DailyMissionBook book, DateTimeOffset now)
    {
        return book.Missions.Count == 0 ||
               book.Missions.Any(e => catalog.TryGet(e.Mission, out var definition) &&
                                      definition.Type == DailyMissionType.Special &&
                                      !DailyMissionRules.IsOpen(definition, e, now));
    }

    private void DrawAll(DailyMissionBook book, DateTimeOffset now)
    {
        var ids = new List<int>();

        foreach (var type in new[] { DailyMissionType.Special, DailyMissionType.Guerrilla, DailyMissionType.Event })
        {
            ids.AddRange(DailyMissionRules.Draw(catalog, type, now, Random));
        }

        book.Missions = [];

        foreach (var id in ids)
        {
            if (catalog.TryGet(id, out var definition))
            {
                book.Missions.Add(NewEntry(definition, now));
            }
        }
    }

    private DailyMissionEntry NewEntry(DailyMissionDefinition definition, DateTimeOffset now)
    {
        var (start, end) = DailyMissionRules.GetWindow(definition, now, Reset);

        return new DailyMissionEntry
        {
            Mission = definition.Id,
            State = DailyMissionState.None,
            Accept = start,
            Start = start,
            End = end
        };
    }

    // A mission that was accepted and is no longer open has failed.
    private List<DailyMissionEntry> EndRunOut(DailyMissionBook book, DateTimeOffset now)
    {
        var ended = new List<DailyMissionEntry>();

        foreach (var entry in book.Missions.Where(e => e.State == DailyMissionState.Accept))
        {
            if (catalog.TryGet(entry.Mission, out var definition) &&
                definition.Type != DailyMissionType.Special &&
                !DailyMissionRules.IsOpen(definition, entry, now))
            {
                entry.State = DailyMissionState.Fail;
                entry.Helper = false;
                ended.Add(entry);
            }
        }

        return ended;
    }

    private DailyMissionValue ToValue(DailyMissionEntry entry, DateTimeOffset now)
    {
        catalog.TryGet(entry.Mission, out var definition);

        return new DailyMissionValue
        {
            Mission = entry.Mission,
            Type = definition?.Type ?? DailyMissionType.None,
            State = entry.State,
            Helper = entry.Helper ? DailyMissionHelperState.Add : DailyMissionHelperState.Del,
            Value = entry.Value,
            Accept = entry.Accept,
            Start = entry.Start,
            End = entry.End,
            Remain = DailyMissionRules.GetRemain(now, entry.Start),
            Duration = DailyMissionRules.GetDuration(now, entry.Start, entry.End)
        };
    }

    private void SendList(Entity player, DailyMissionBook book, DateTimeOffset now, bool todayInit)
    {
        var groups = book.Missions
            .GroupBy(e => catalog.TryGet(e.Mission, out var definition) ? definition.District : 0)
            .OrderBy(e => e.Key)
            .Select(e => new DailyMissionGroupValue
            {
                District = e.Key,
                Missions = e.Select(entry => ToValue(entry, now)).ToArray()
            })
            .ToArray();

        world.Get<ServerSessionComponent>(player)
            .Send(new DailyMissionListResponse { TodayInit = todayInit, Groups = groups });
    }

    private void SendUpdate(Entity player, DailyMissionBook book, IReadOnlyCollection<DailyMissionEntry> changed, DateTimeOffset now)
    {
        world.Get<ServerSessionComponent>(player)
            .Send(new DailyMissionUpdateResponse { List = changed.Select(e => ToValue(e, now)).ToArray() });
    }

    private void SendError(Entity player, DailyMissionOpcode command, ushort code)
    {
        world.Get<ServerSessionComponent>(player)
            .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.DailyMission, command), code));
    }

    private bool TryGet(Entity player, out DailyMissionBook book)
    {
        if (world.IsAlive(player) && world.TryGet<DailyMissionComponent>(player, out var component) && component.Book is not null)
        {
            book = component.Book;
            return true;
        }

        book = null!;
        return false;
    }

    /// <summary>
    /// The flush of the person writes the book (<see cref="PersonSections.MiscState"/>); nothing waits for the database here.
    /// </summary>
    private ValueTask SaveAsync(Entity player, DailyMissionBook book, CancellationToken cancellationToken)
    {
        world.MarkDirty(player, PersonSections.MiscState);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The reward of the daily mission {Mission} could not be given.")]
    private partial void LogRewardNotGiven(int mission);
}
