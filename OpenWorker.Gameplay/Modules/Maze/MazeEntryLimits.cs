using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Types;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.World.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Maze;

/// <summary>
/// How often a person may enter a maze in a day, and when a maze is open at all.
/// <list type="bullet">
/// <item>A maze that limits entering (<c>EntryLimit</c> of its server row) is counted for the person when the person comes into
/// it, not at the portal (<see cref="Entered"/>); the portal only asks whether the count is below the limit
/// (<see cref="Check"/>). Mazes of one group share one count.</item>
/// <item>The counts clear when a new game day begins (the day of the daily missions, as the fatigue): the client is told by
/// 04/44, and by 04/43 as the person enters the world, so the board shows what is left.</item>
/// <item>The casual raids of a portal take turns by the day of the week (<see cref="MazeDailyPortalRow"/>); one that is not
/// open today is refused. A maze with windows in <see cref="MazeOpenControlRow"/> is open only in them.</item>
/// <item>A person may get up from a fall without paying <see cref="FreeRevivesPerDay"/> times a day in a maze.</item>
/// </list>
/// A change marks <see cref="PersonSections.MiscState"/> and the flush writes the book.
/// </summary>
public sealed partial class MazeEntryLimits : IMiscTick
{
    /// <summary>The person has entered the maze as often as it may today.</summary>
    public const ushort LimitReached = 55053;

    /// <summary>The casual raid of the maze is not open today.</summary>
    public const ushort NotOpenToday = 53138;

    /// <summary>The maze is closed at this time of the day.</summary>
    public const ushort Closed = 55060;

    /// <summary>The kind of maze that takes turns by the day of the week (a casual raid).</summary>
    public const byte CasualRaid = 2;

    /// <summary>
    /// How many times a day a person may get up in a maze without paying: the stack size of the item that counts them.
    /// </summary>
    public const int FreeRevivesPerDay = 3;

    private readonly World _world;
    private readonly PersonStateStore _store;
    private readonly TimeProvider _time;
    private readonly IOptions<DailyMissionOptions> _options;
    private readonly ILogger<MazeEntryLimits> _logger;
    private readonly InventoryService? _inventory;
    private readonly RowIndex<ushort, MazeRuleRow> _rules;
    private readonly RowIndex<ushort, MazeDailyPortalRow> _portals;
    private readonly Dictionary<uint, MazeOpenControlRow> _windows;
    private readonly Dictionary<ushort, ushort[]> _groups;

    public MazeEntryLimits(
        World world,
        PersonStateStore store,
        ReadOnlyCollection<MazeRuleRow> rules,
        ReadOnlyCollection<MazeDailyPortalRow> portals,
        ReadOnlyCollection<MazeOpenControlRow> windows,
        TimeProvider time,
        IOptions<DailyMissionOptions> options,
        ILogger<MazeEntryLimits> logger,
        InventoryService? inventory = null)
    {
        _world = world;
        _store = store;
        _time = time;
        _options = options;
        _logger = logger;
        _inventory = inventory;
        _rules = rules.IndexBy(e => e.Id);
        _portals = portals.IndexBy(e => e.Id);
        _windows = [];

        foreach (var window in windows)
        {
            _windows.TryAdd(window.Id, window);
        }

        _groups = rules
            .Where(e => e.EntryLimitGroup != 0)
            .GroupBy(e => e.EntryLimitGroup)
            .ToDictionary(e => e.Key, e => e.Select(r => r.Id).ToArray());
    }

    private static QueryDescription Persons { get; } = new QueryDescription().WithAll<MazeEntryComponent, ServerSessionComponent>();

    /// <summary>
    /// The book of the entries as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<MazeEntryComponent>(PersonStateKind.MazeEntry, PersonSections.MiscState, e => e.Book);

    private TimeSpan Reset => _options.Value.ResetHour is { } hour ? TimeSpan.FromHours(hour) : DailyMissionRules.DefaultResetTime;

    /// <summary>
    /// The start of the game day that is running, in UTC.
    /// </summary>
    public DateTime Today => GameDay.Get(_time.GetUtcNow(), Reset).Start.UtcDateTime;

    /// <summary>
    /// Loads the entries of the person as it enters a district or a maze; a day that is over since the book was written
    /// clears it.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await _store.LoadAsync<MazeEntryBook>(person, PersonStateKind.MazeEntry, cancellationToken) ?? new MazeEntryBook();

        if (!_world.IsAlive(player))
        {
            return;
        }

        _world.Set(player, new MazeEntryComponent { Person = person, Book = book });

        if (book.Day < Today && book.StartDay(Today))
        {
            _world.MarkDirty(player, PersonSections.MiscState);
        }
    }

    /// <summary>
    /// Tells the client how often the person has entered the mazes today, as the person enters the world.
    /// </summary>
    public void SendCounts(Entity player)
    {
        if (!TryGet(player, out var book) || !_world.Has<ServerSessionComponent>(player))
        {
            return;
        }

        var actor = _world.Get<ActorComponent>(player);
        var counts = new List<MazeLimitCountValue>();

        foreach (var (maze, count) in book.Mazes)
        {
            Add(counts, actor, maze, count);
        }

        foreach (var (group, count) in book.Groups)
        {
            foreach (var maze in _groups.GetValueOrDefault((ushort)Math.Clamp(group, 0, ushort.MaxValue)) ?? [])
            {
                Add(counts, actor, maze, count);
            }
        }

        // The client counts the list in a byte.
        _world.Get<ServerSessionComponent>(player).Send(new WorldEnterMazeLimitCountLoadResponse
        {
            Counts = counts.Take(byte.MaxValue).ToArray()
        });
    }

    /// <summary>
    /// How often the person has entered the maze today, or, for a maze of a group, the group.
    /// </summary>
    public int CountOf(Entity player, int maze)
    {
        if (!TryGet(player, out var book) || !_rules.TryGet(ToKey(maze), out var rule))
        {
            return 0;
        }

        return rule.EntryLimitGroup != 0
            ? book.Groups.GetValueOrDefault(rule.EntryLimitGroup)
            : book.Mazes.GetValueOrDefault(maze);
    }

    /// <summary>
    /// Whether the person may enter the maze today by its daily limit and its day of the week. Null when it may, else the
    /// code of the refusal; a maze the server table has no row for is not limited.
    /// </summary>
    public ushort? Check(Entity player, int maze)
    {
        if (!_rules.TryGet(ToKey(maze), out var rule))
        {
            return null;
        }

        var counted = rule;

        // A casual raid takes turns with the others of its portal: it must be open today, and the first maze of the day
        // is the one the entries are counted for.
        if (rule.Type == CasualRaid)
        {
            if (!OpensToday(rule, out var first))
            {
                return rule.EntryLimit != 0 ? LimitReached : NotOpenToday;
            }

            if (!_rules.TryGet(ToKey(first), out counted))
            {
                return rule.EntryLimit != 0 ? LimitReached : null;
            }
        }

        if (rule.EntryLimit == 0)
        {
            return null;
        }

        return CountOf(player, counted.Id) >= counted.EntryLimit ? LimitReached : null;
    }

    /// <summary>
    /// Whether the maze is open at this time of the day: it has no window today, or the time is in one of them.
    /// </summary>
    public bool IsOpen(int maze)
    {
        var now = _time.GetUtcNow();
        var day = (int)now.DayOfWeek + 1;
        var found = false;

        // The windows of a day are numbered one after the other: the first number that is missing ends them.
        for (var number = 1; number <= MazeOpenControlRow.MaxWindows; number++)
        {
            if (!_windows.TryGetValue(MazeOpenControlRow.KeyOf(maze, day, number), out var window))
            {
                break;
            }

            found = true;

            var opens = now.Date.AddHours(window.OpenTime / 100).AddMinutes(window.OpenTime % 100);

            if (opens <= now.UtcDateTime && now.UtcDateTime <= opens.AddMinutes(window.RemainMinutes))
            {
                return true;
            }
        }

        return !found;
    }

    /// <summary>
    /// The person has come into the maze: the item the maze asks for is taken and a maze that limits entering counts it
    /// (<see cref="TellCount"/> tells the client the new count). False when the item is not there any more (it was there at the portal), which the
    /// caller answers by sending the person away.
    /// </summary>
    public bool Entered(Entity player, int maze)
    {
        if (!_rules.TryGet(ToKey(maze), out var rule))
        {
            return true;
        }

        if (rule.RequiredItem is > 0 and <= int.MaxValue && _inventory is not null &&
            !_inventory.TryTake(player, (int)rule.RequiredItem, Math.Max(1, (int)rule.RequiredItemCount)))
        {
            LogNoItem(_logger, maze, (int)rule.RequiredItem);

            return false;
        }

        if (!TryGet(player, out var book) || rule.EntryLimit == 0)
        {
            return true;
        }

        var counted = rule;

        if (rule.Type == CasualRaid && (!OpensToday(rule, out var first) || !_rules.TryGet(ToKey(first), out counted)))
        {
            LogNotCounted(_logger, maze);

            return true;
        }

        Refresh(player, book);

        if (counted.EntryLimitGroup != 0)
        {
            book.Groups[counted.EntryLimitGroup] = book.Groups.GetValueOrDefault(counted.EntryLimitGroup) + 1;
        }

        else
        {
            book.Mazes[counted.Id] = book.Mazes.GetValueOrDefault(counted.Id) + 1;
        }

        _world.MarkDirty(player, PersonSections.MiscState);

        return true;
    }

    /// <summary>
    /// Tells the client the count of the maze the player has come into (and of every maze that shares it). Apart from
    /// <see cref="Entered"/> so that it goes when the client has the maze, not while it is still loading it.
    /// </summary>
    public void TellCount(Entity player, int maze)
    {
        if (!_rules.TryGet(ToKey(maze), out var rule) || rule.EntryLimit == 0)
        {
            return;
        }

        var counted = rule;

        if (rule.Type == CasualRaid && (!OpensToday(rule, out var first) || !_rules.TryGet(ToKey(first), out counted)))
        {
            return;
        }

        Update(player, counted);
    }

    /// <summary>
    /// Sets the count of the maze (and of its group) back to nothing, as an item that renews a casual raid does.
    /// </summary>
    public void Reopen(Entity player, int maze)
    {
        if (!TryGet(player, out var book) || !_rules.TryGet(ToKey(maze), out var rule))
        {
            return;
        }

        if (rule.EntryLimitGroup != 0)
        {
            book.Groups.Remove(rule.EntryLimitGroup);
        }

        else
        {
            book.Mazes.Remove(rule.Id);
        }

        _world.MarkDirty(player, PersonSections.MiscState);
        Update(player, rule);
    }

    /// <summary>
    /// How many free revives the person used today.
    /// </summary>
    public int FreeRevivesUsed(Entity player)
    {
        return TryGet(player, out var book) ? book.FreeRevives : 0;
    }

    /// <summary>
    /// Uses one of the free revives of the day; false when all of them are used.
    /// </summary>
    public bool TryUseFreeRevive(Entity player)
    {
        if (!TryGet(player, out var book))
        {
            return false;
        }

        Refresh(player, book);

        if (book.FreeRevives >= FreeRevivesPerDay)
        {
            return false;
        }

        book.FreeRevives++;
        _world.MarkDirty(player, PersonSections.MiscState);

        return true;
    }

    /// <summary>
    /// Clears the counts of every person whose game day has turned, and tells the client.
    /// </summary>
    public ValueTask TickAsync(CancellationToken cancellationToken)
    {
        var players = new List<Entity>();

        _world.Query(Persons, entity => players.Add(entity));

        foreach (var player in players)
        {
            if (TryGet(player, out var book))
            {
                Refresh(player, book);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Starts the new game day for the person when one has begun since the book was last looked at: the counts clear and the
    /// client is told. True when it did.
    /// </summary>
    private bool Refresh(Entity player, MazeEntryBook book)
    {
        var today = Today;

        if (book.Day >= today)
        {
            return false;
        }

        book.StartDay(today);
        _world.MarkDirty(player, PersonSections.MiscState);

        if (_world.Has<ServerSessionComponent>(player))
        {
            _world.Get<ServerSessionComponent>(player).Send(new WorldEnterMazeLimitCountResetResponse { Actor = _world.Get<ActorComponent>(player) });
        }

        LogNewDay(_logger, today);

        return true;
    }

    /// <summary>
    /// Tells the client the count of the maze, and of every maze that shares it.
    /// </summary>
    private void Update(Entity player, in MazeRuleRow counted)
    {
        if (!_world.Has<ServerSessionComponent>(player))
        {
            return;
        }

        var session = _world.Get<ServerSessionComponent>(player);
        var actor = _world.Get<ActorComponent>(player);
        var count = (ushort)Math.Clamp(CountOf(player, counted.Id), 0, ushort.MaxValue);
        var mazes = counted.EntryLimitGroup != 0 && _groups.TryGetValue(counted.EntryLimitGroup, out var members) ? members : [counted.Id];

        foreach (var maze in mazes)
        {
            session.Send(new WorldEnterMazeLimitCountUpdateResponse { Actor = actor, Maze = maze, Count = count });
        }
    }

    private static void Add(List<MazeLimitCountValue> counts, ActorValue actor, int maze, int count)
    {
        if (count > 0 && maze is > 0 and <= ushort.MaxValue)
        {
            counts.Add(new MazeLimitCountValue { Actor = actor, Maze = (ushort)maze, Count = (ushort)Math.Min(count, ushort.MaxValue) });
        }
    }

    /// <summary>
    /// Whether the portal of the casual raid opens it on the game day that is running, and the first maze of that day.
    /// </summary>
    private bool OpensToday(in MazeRuleRow rule, out int first)
    {
        first = 0;

        if (!_portals.TryGet(rule.PortalId, out var portal))
        {
            return false;
        }

        var day = GameDay.Get(_time.GetUtcNow(), Reset).Start.DayOfWeek;

        return portal.Opens(day, rule.Id, out first);
    }

    private bool TryGet(Entity player, out MazeEntryBook book)
    {
        if (_world.IsAlive(player) && _world.TryGet<MazeEntryComponent>(player, out var component) && component.Book is { } loaded)
        {
            book = loaded;

            return true;
        }

        book = null!;

        return false;
    }

    private static ushort ToKey(int maze) => (ushort)Math.Clamp(maze, 0, ushort.MaxValue);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The entries into the mazes start a new game day at {Day}")]
    private static partial void LogNewDay(ILogger logger, DateTime day);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The item {Item} the maze {Maze} asks for is gone from the bag of the player who came in")]
    private static partial void LogNoItem(ILogger logger, int maze, int item);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The entry into the casual raid {Maze} is not counted: no portal opens it today")]
    private static partial void LogNotCounted(ILogger logger, int maze);
}
