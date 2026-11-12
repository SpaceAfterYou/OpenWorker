using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Gameplay.Modules.Tower.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.InfiniteTower.Responses;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Tower;

/// <summary>
/// A stage of the tower: its chapter, its place in the chapter (1 to 5), its maze, the maze of the stage after it (0 after the
/// last) and what clearing it gives.
/// </summary>
public readonly record struct TowerStage(int Chapter, int Stage, short Maze, short NextMaze, int Item, int Count, int BattlePoint)
{
    /// <summary>
    /// The stage counted over the whole tower: stage + 5 × (chapter − 1).
    /// </summary>
    public int Overall => Stage + InfiniteTower.StagesPerChapter * (Chapter - 1);
}

/// <summary>
/// Why a chapter of the tower may not be entered: the request the client waits on and the code it shows.
/// </summary>
public readonly record struct TowerRefusal(MessageOpcode Request, ushort Code);

/// <summary>
/// The infinite tower (Steel Grave): fifteen chapters of five stages, each stage a maze of its own. A person climbs alone,
/// a chapter from its first stage on, each stage cleared leading to the next; a fall ends the climb.
/// <list type="bullet">
/// <item>Entering a chapter (28/02) asks, in the order of the reference: a row of the client's table for the chapter, that the
/// chapter is at most the one after the last cleared, a row of the maze of its first stage, the count of the day below the
/// limit of that maze (the server's table), no party, the soul weapon in hand (<see cref="CheckEnter"/>).</item>
/// <item>The first stage of a chapter counts an entry of the day; every stage entered counts for achievement 46
/// (<see cref="Entered"/>).</item>
/// <item>A cleared stage gives the item and the battle points of its row of the table, by post when the item does not fit,
/// tells the client (28/04), raises the last cleared chapter and stage, and records the time for the ranking
/// (<see cref="Cleared"/>).</item>
/// <item>The count clears when a new game day begins (the day of the daily missions, as the entries into mazes); a ticket of
/// the tower clears it too, once the day's entries are used up (<see cref="UseTicket"/>).</item>
/// </list>
/// The client is told how far the person has come (28/01) as it enters a district and whenever that changes. A change marks
/// <see cref="PersonSections.MiscState"/> and the flush writes the book.
/// </summary>
public sealed partial class InfiniteTower : IMiscTick
{
    /// <summary>The stages of a chapter.</summary>
    public const int StagesPerChapter = 5;

    /// <summary>The kind of maze that is a stage of the tower.</summary>
    public const byte TowerMazeKind = 7;

    /// <summary>The chapter asked for is beyond the one after the last cleared.</summary>
    public const ushort WrongChapter = 58100;

    /// <summary>The person has entered the tower as often as it may today.</summary>
    public const ushort LimitReached = 58101;

    /// <summary>The tower is climbed alone: a person in a party may not enter.</summary>
    public const ushort InParty = 58102;

    /// <summary>The tables have nothing for the chapter or its first stage.</summary>
    public const ushort InvalidData = 58103;

    /// <summary>The stage the person asks to leave is not cleared yet, or is no stage of the tower.</summary>
    public const ushort StageNotCleared = 58104;

    /// <summary>The person asks for the next stage while it is in no maze.</summary>
    public const ushort NotInMaze = 55003;

    /// <summary>The person has no soul weapon in hand: answered on the request of a world entry, as the reference does.</summary>
    public const ushort NoWeapon = MazeAdmission.NoWeapon;

    /// <summary>A ticket of the tower is used while entries of the day remain.</summary>
    public const ushort EntriesLeft = 52613;

    // What the reference names a letter of the rewards of a maze.
    private const byte RewardLetter = 5;
    private const byte MazeRewardLetter = 1;

    public static readonly MessageOpcode EnterChapterRequest = MessageOpcode.From(GroupOpcode.InfiniteTower, InfiniteTowerOpcode.EnterChapter);
    public static readonly MessageOpcode WorldEnterRequest = MessageOpcode.From(GroupOpcode.World, WorldOpcode.EnterReq);

    private readonly World _world;
    private readonly PersonStateStore _store;
    private readonly MazeEntryLimits _days;
    private readonly ILogger<InfiniteTower> _logger;
    private readonly InventoryService? _inventory;
    private readonly AchievementManager? _achievements;
    private readonly IPostStore? _post;
    private readonly IRankingStore? _ranking;
    private readonly RowIndex<short, InfiniteTowerRow> _chapters;
    private readonly RowIndex<ushort, MazeRuleRow> _rules;
    private readonly Dictionary<short, TowerStage> _stages = [];

    public InfiniteTower(
        World world,
        PersonStateStore store,
        ReadOnlyCollection<InfiniteTowerRow> chapters,
        ReadOnlyCollection<MazeRuleRow> rules,
        MazeEntryLimits days,
        ILogger<InfiniteTower> logger,
        InventoryService? inventory = null,
        AchievementManager? achievements = null,
        IPostStore? post = null,
        IRankingStore? ranking = null)
    {
        _world = world;
        _store = store;
        _days = days;
        _logger = logger;
        _inventory = inventory;
        _achievements = achievements;
        _post = post;
        _ranking = ranking;
        _chapters = chapters.IndexBy(e => e.Id);
        _rules = rules.IndexBy(e => e.Id);

        foreach (var row in chapters)
        {
            var stages = Stages(row);

            for (var index = 0; index < stages.Length; index++)
            {
                var next = index + 1 < stages.Length ? stages[index + 1].Maze : (short)0;

                _stages.TryAdd(stages[index].Maze, stages[index] with { NextMaze = next });
            }
        }
    }

    private static QueryDescription Persons { get; } = new QueryDescription().WithAll<InfiniteTowerComponent, ServerSessionComponent>();

    /// <summary>
    /// The book of the climb as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<InfiniteTowerComponent>(PersonStateKind.InfiniteTower, PersonSections.MiscState, e => e.Book);

    /// <summary>
    /// The five stages of a row of the table, the ones without a maze left out.
    /// </summary>
    private static TowerStage[] Stages(InfiniteTowerRow row)
    {
        TowerStage Stage(int stage, short maze, int item, byte count, int points) => new(row.Id, stage, maze, 0, item, count, points);

        return new[]
            {
                Stage(1, row.Maze1, row.RewardItem1, row.RewardCount1, row.RewardBattlePoint1),
                Stage(2, row.Maze2, row.RewardItem2, row.RewardCount2, row.RewardBattlePoint2),
                Stage(3, row.Maze3, row.RewardItem3, row.RewardCount3, row.RewardBattlePoint3),
                Stage(4, row.Maze4, row.RewardItem4, row.RewardCount4, row.RewardBattlePoint4),
                Stage(5, row.Maze5, row.RewardItem5, row.RewardCount5, row.RewardBattlePoint5)
            }
            .Where(e => e.Maze > 0)
            .ToArray();
    }

    /// <summary>
    /// The stage the maze is, when it is one of the tower.
    /// </summary>
    public bool TryGetStage(int maze, out TowerStage stage)
    {
        if (maze is > 0 and <= short.MaxValue)
        {
            return _stages.TryGetValue((short)maze, out stage);
        }

        stage = default;

        return false;
    }

    /// <summary>
    /// The mazes of the five stages of a chapter, in order; empty for a chapter the table does not have.
    /// </summary>
    public IReadOnlyList<short> MazesOf(int chapter)
    {
        return chapter is > 0 and <= short.MaxValue && _chapters.TryGet((short)chapter, out var row)
            ? Stages(row).Select(e => e.Maze).ToArray()
            : [];
    }

    /// <summary>
    /// Loads the climb of the person as it enters a district or a maze; a day that is over since the book was written clears
    /// its count.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await _store.LoadAsync<InfiniteTowerBook>(person, PersonStateKind.InfiniteTower, cancellationToken) ?? new InfiniteTowerBook();

        if (!_world.IsAlive(player))
        {
            return;
        }

        _world.Set(player, new InfiniteTowerComponent { Person = person, Book = book });

        if (book.Day < _days.Today && book.StartDay(_days.Today))
        {
            _world.MarkDirty(player, PersonSections.MiscState);
        }
    }

    /// <summary>
    /// Tells the client how far the person has climbed and how often it went up today (28/01).
    /// </summary>
    public void Send(Entity player)
    {
        if (!TryGet(player, out var book) || !_world.Has<ServerSessionComponent>(player))
        {
            return;
        }

        _world.Get<ServerSessionComponent>(player).Send(new InfiniteTowerLoadInfoResponse
        {
            ClearChapter = (short)Math.Clamp(book.ClearChapter, 0, short.MaxValue),
            ClearStage = (short)Math.Clamp(book.ClearStage, 0, short.MaxValue),
            // The reference sends what is left of a time it sets at the start of the day, which is always past.
            LeftTime = 0,
            Count = (short)Math.Clamp(book.Entries, 0, short.MaxValue)
        });
    }

    /// <summary>
    /// Whether the person may climb the chapter, in the order of the reference. Null when it may, with the maze of the first
    /// stage in <paramref name="maze"/>; else the request to answer and the code.
    /// </summary>
    public TowerRefusal? CheckEnter(Entity player, int chapter, bool inParty, out short maze)
    {
        maze = 0;

        if (chapter is <= 0 or > short.MaxValue || !_chapters.TryGet((short)chapter, out var row) || Stages(row) is not [var first, ..])
        {
            return new TowerRefusal(EnterChapterRequest, InvalidData);
        }

        if (!TryGet(player, out var book))
        {
            return new TowerRefusal(EnterChapterRequest, InvalidData);
        }

        if (chapter > book.ClearChapter + 1)
        {
            return new TowerRefusal(EnterChapterRequest, WrongChapter);
        }

        if (!_rules.TryGet((ushort)first.Maze, out var rule))
        {
            return new TowerRefusal(EnterChapterRequest, InvalidData);
        }

        Refresh(player, book);

        if (rule.EntryLimit != 0 && book.Entries >= rule.EntryLimit)
        {
            return new TowerRefusal(EnterChapterRequest, LimitReached);
        }

        if (inParty)
        {
            return new TowerRefusal(EnterChapterRequest, InParty);
        }

        if (_inventory?.InventoryOf(player) is { } bag && bag.At(StorageGroup.AbilityEquip, (int)AbilityEquipSlot.SoulWeapon) is null)
        {
            return new TowerRefusal(WorldEnterRequest, NoWeapon);
        }

        maze = first.Maze;

        return null;
    }

    /// <summary>
    /// The person has come into a stage of the tower and the client has it: the first stage of a chapter counts an entry of
    /// the day and tells the client, every stage counts for its achievement.
    /// </summary>
    public void Entered(Entity player, int maze)
    {
        if (!TryGetStage(maze, out var stage))
        {
            return;
        }

        if (stage.Stage == 1 && TryGet(player, out var book))
        {
            Refresh(player, book);
            book.Entries++;
            _world.MarkDirty(player, PersonSections.MiscState);
            Send(player);
        }

        _achievements?.Count(player, AchievementType.TowerStageEntered);
    }

    /// <summary>
    /// The person has cleared a stage of the tower in <paramref name="seconds"/>: it gets the reward of the stage, the client
    /// shows it (28/04), the climb goes up and the time is kept for the ranking. False for a maze that is no stage.
    /// </summary>
    public bool Cleared(Entity player, int maze, int seconds)
    {
        if (!TryGetStage(maze, out var stage) || !_world.IsAlive(player) || !_world.Has<ServerSessionComponent>(player))
        {
            return false;
        }

        seconds = Math.Max(0, seconds);

        if (stage.Item > 0 && stage.Count > 0 && _inventory is not null && !_inventory.TryGive(player, stage.Item, stage.Count) &&
            _post is not null && _world.TryGet<ClaimsComponent>(player, out var claims))
        {
            _ = MailAsync(player, claims.Account, stage.Item, stage.Count);
        }

        if (stage.BattlePoint > 0)
        {
            _inventory?.AddUpToLimit(player, ShopCurrency.Bp, stage.BattlePoint);
        }

        _world.Get<ServerSessionComponent>(player).Send(new InfiniteTowerRewardResponse
        {
            Item = stage.Item,
            Count = (short)Math.Clamp(stage.Count, 0, short.MaxValue),
            BattlePoint = stage.BattlePoint,
            ClearTime = seconds
        });

        if (TryGet(player, out var book))
        {
            var chapter = stage.Stage == StagesPerChapter && book.ClearChapter < stage.Chapter ? stage.Chapter : book.ClearChapter;
            var overall = Math.Max(book.ClearStage, stage.Overall);

            if (chapter != book.ClearChapter || overall != book.ClearStage)
            {
                book.ClearChapter = chapter;
                book.ClearStage = overall;
                _world.MarkDirty(player, PersonSections.MiscState);
                Send(player);
            }
        }

        if (_ranking is not null && _world.TryGet<ActorComponent>(player, out var actor))
        {
            _ = RecordAsync(actor.Identifier, stage.Maze, seconds);
        }

        LogCleared(_logger, stage.Chapter, stage.Stage, seconds);

        return true;
    }

    /// <summary>
    /// A ticket of the tower is used: it clears the count of the day once the entries of the day are used up. Null when it
    /// did, else the code of the refusal.
    /// </summary>
    public ushort? UseTicket(Entity player)
    {
        if (!TryGet(player, out var book))
        {
            return EntriesLeft;
        }

        Refresh(player, book);

        if (book.Entries < Limit())
        {
            return EntriesLeft;
        }

        book.Entries = 0;
        _world.MarkDirty(player, PersonSections.MiscState);
        Send(player);

        return null;
    }

    /// <summary>
    /// How many chapters a person may enter in a day: the limit of the maze of the first stage of the first chapter (the stages
    /// of the tower share it).
    /// </summary>
    private int Limit()
    {
        return _chapters.TryGet(1, out var row) && _rules.TryGet((ushort)row.Maze1, out var rule) && rule.EntryLimit != 0
            ? rule.EntryLimit
            : int.MaxValue;
    }

    /// <summary>
    /// Clears the count of every person whose game day has turned, and tells the client.
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
    /// Starts the new game day for the person when one has begun since the book was last looked at: the count clears and the
    /// client is told.
    /// </summary>
    private void Refresh(Entity player, InfiniteTowerBook book)
    {
        var today = _days.Today;

        if (book.Day >= today)
        {
            return;
        }

        book.StartDay(today);
        _world.MarkDirty(player, PersonSections.MiscState);
        Send(player);
    }

    private bool TryGet(Entity player, out InfiniteTowerBook book)
    {
        if (_world.IsAlive(player) && _world.TryGet<InfiniteTowerComponent>(player, out var component) && component.Book is { } loaded)
        {
            book = loaded;

            return true;
        }

        book = null!;

        return false;
    }

    private async Task RecordAsync(int person, short maze, int seconds)
    {
        try
        {
            await _ranking!.SubmitAsync(RankingKind.InfiniteTower, maze, person, seconds, 0);
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogRankingFailed(_logger, e, person, maze);
        }
    }

    /// <summary>
    /// The item of a stage that does not fit goes as a letter of the game to the account of the player, who is told of it while
    /// still here.
    /// </summary>
    private async Task MailAsync(Entity player, int account, int item, int count)
    {
        try
        {
            var letter = await _post!.SendToAccountAsync(new PostAccountDraft(
                account,
                "SoulWorker",
                "Steel Grave",
                "The reward of a stage that did not fit.",
                0,
                [new PostItemPersistent { Item = item, Count = (short)Math.Clamp(count, 0, short.MaxValue), Endurance = 100 }],
                RewardLetter,
                MazeRewardLetter));

            var (_, unread) = await _post.CountUnreadAsync(0, account);

            if (_world.IsAlive(player) && _world.Has<ServerSessionComponent>(player))
            {
                _world.Get<ServerSessionComponent>(player).Send(new PostAccountRecvResponse
                {
                    PostCount = (short)Math.Min(unread, short.MaxValue),
                    Value = PostLetters.Account(letter, DateTime.UtcNow)
                });
            }
        }

        catch (Exception e) when (e is not OperationCanceledException)
        {
            LogMailFailed(_logger, e, account, item);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stage {Stage} of chapter {Chapter} of the tower cleared in {Seconds} seconds")]
    private static partial void LogCleared(ILogger logger, int chapter, int stage, int seconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "The clear of person {Person} in the stage {Maze} of the tower could not be ranked")]
    private static partial void LogRankingFailed(ILogger logger, Exception exception, int person, int maze);

    [LoggerMessage(Level = LogLevel.Error, Message = "The item {Item} of a stage of the tower could not be sent to account {Account} by post")]
    private static partial void LogMailFailed(ILogger logger, Exception exception, int account, int item);
}
