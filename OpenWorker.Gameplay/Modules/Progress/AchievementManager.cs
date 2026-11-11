using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Progress;

/// <summary>
/// What came of a request for the reward of an achievement.
/// </summary>
/// <param name="Given">The reward was given; the client is told with the answer of the request.</param>
/// <param name="Error">The code of the error answer, 0 for no answer at all.</param>
/// <param name="Titles">The titles of the reward, which whoever keeps the titles gives.</param>
public sealed record AchievementReward(bool Given, ushort Error, IReadOnlyList<int> Titles)
{
    public static AchievementReward Silent { get; } = new(false, 0, []);
}

/// <summary>
/// The achievements of a person: the counters of its chains, told to the client when the person enters and whenever they
/// move, and the rewards of the achievements reached. The rest of the game tells the manager what the person does
/// (<see cref="OnMonsterKilled"/>, <see cref="OnMazeCleared"/>, <see cref="OnLevelReached"/>, <see cref="OnQuestFinished"/>,
/// <see cref="OnHelperGained"/>, <see cref="OnGoldPicked"/>, <see cref="OnEtherPicked"/>); a change marks
/// <see cref="PersonSections.Progress"/> and the flush writes the book.
/// </summary>
public sealed partial class AchievementManager(
    World world,
    PersonStateStore store,
    AchievementCatalog catalog,
    ProgressItems items,
    InventoryService inventory,
    ReadOnlyCollection<MonsterRow> monsters,
    ILogger<AchievementManager> logger)
{
    /// <summary>The gold of the reward would go past what a person can hold.</summary>
    public const ushort GoldOverflow = 52056;

    /// <summary>The battle points of the reward would go past what a person can hold.</summary>
    public const ushort BattlePointOverflow = 52057;

    /// <summary>The ether of the reward would go past what a person can hold.</summary>
    public const ushort EtherOverflow = 52058;

    /// <summary>The items of the reward do not fit the bags.</summary>
    public const ushort DoesNotFit = 52013;

    /// <summary>
    /// The most entries a list of the client may have: it counts them in a signed byte.
    /// </summary>
    private const int MaxListLength = sbyte.MaxValue;

    /// <summary>
    /// A maze cleared within this many seconds counts as a fast clear.
    /// </summary>
    private const int FastClearSeconds = 180;

    private readonly RowIndex<int, MonsterRow> _monsters = monsters.IndexBy(e => e.Id);

    /// <summary>
    /// The book of the achievements as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<AchievementComponent>(PersonStateKind.Achievements, PersonSections.Progress, e => e.Book);

    public AchievementCatalog Catalog => catalog;

    /// <summary>
    /// Loads the achievements of the person, starts the chains its level opens and tells the client.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<AchievementBook>(person, PersonStateKind.Achievements, cancellationToken) ?? new AchievementBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        var level = world.TryGet<LevelComponent>(player, out var rank) ? rank.Level : 1;

        book.Normalize(catalog);
        book.Open(catalog, level);
        book.CatchUp(catalog, level);

        world.Set(player, new AchievementComponent { Person = person, Book = book });

        Send(player, new PersonAchieveSelectResponse
        {
            RewardBits = book.Rewarded,
            Achievements = book.Values().Take(MaxListLength).ToArray(),
            Categories = book.Categories(catalog)
        });
    }

    public void OnMonsterKilled(Entity player, int monster)
    {
        if (!_monsters.TryGet(monster, out var row))
        {
            return;
        }

        var events = new List<(AchievementType, int)>(4)
        {
            (AchievementType.MonsterKilled, row.Id),
            (AchievementType.MonsterKilled, row.Type),
            (AchievementType.FactionKilled, row.Faction)
        };

        AchievementType? byRank = row.Rank switch
        {
            0 => AchievementType.WeakMonsterKilled,
            1 => AchievementType.NormalMonsterKilled,
            2 => AchievementType.StrongMonsterKilled,
            3 => AchievementType.MiddleBossKilled,
            4 => AchievementType.BossKilled,
            5 => AchievementType.RaidBossKilled,
            _ => null
        };

        if (byRank is { } type)
        {
            events.Insert(0, (type, 0));
        }

        Raise(player, events, 1);
    }

    /// <summary>
    /// A maze is cleared: counted for the maze and for any maze, as a fast clear when it took three minutes at most, for
    /// its kind when it is not a plain maze, and for the rank of the run when it has one.
    /// </summary>
    public void OnMazeCleared(Entity player, int maze, int seconds, int kind, byte? rank = null)
    {
        var events = new List<(AchievementType, int)>(5)
        {
            (AchievementType.MazeCleared, maze),
            (AchievementType.MazeCleared, 0)
        };

        // A clear by rank counts for the type of its rank, from SS (rank 5) down to F (rank 0).
        if (rank is { } cleared && cleared <= AchievementType.MazeClearedRankF - AchievementType.MazeClearedRankSS)
        {
            events.Insert(0, ((AchievementType)((int)AchievementType.MazeClearedRankF - cleared), 0));
        }

        if (seconds <= FastClearSeconds)
        {
            events.Insert(0, (AchievementType.MazeClearedFast, 0));
        }

        if (kind != 0)
        {
            events.Add((AchievementType.SpecialMazeCleared, kind));
        }

        Raise(player, events, 1);
    }

    /// <summary>
    /// The person is at a new level: the counter of the level is the level, and chains that open at it start.
    /// </summary>
    public void OnLevelReached(Entity player, int level)
    {
        if (!TryGet(player, out var book))
        {
            return;
        }

        if (book.Open(catalog, level))
        {
            world.MarkDirty(player, PersonSections.Progress);

            Send(player, new PersonAchieveSelectResponse
            {
                RewardBits = book.Rewarded,
                Achievements = book.Values().Take(MaxListLength).ToArray(),
                Categories = book.Categories(catalog)
            });
        }

        Raise(player, [(AchievementType.LevelReached, 0)], level);
    }

    public void OnQuestFinished(Entity player, QuestContents contents)
    {
        AchievementType? type = contents switch
        {
            QuestContents.Main => AchievementType.MainQuestFinished,
            QuestContents.Side => AchievementType.SideQuestFinished,
            QuestContents.Repeat => AchievementType.RepeatQuestFinished,
            _ => null
        };

        if (type is { } finished)
        {
            Raise(player, [(finished, 0)], 1);
        }
    }

    public void OnHelperGained(Entity player, int helper)
    {
        Raise(player, [(AchievementType.HelperGained, helper)], 1);
    }

    public void OnGoldPicked(Entity player, int gold)
    {
        Raise(player, [(AchievementType.GoldPicked, 0)], gold);
    }

    public void OnEtherPicked(Entity player, int ether)
    {
        Raise(player, [(AchievementType.EtherPicked, 0)], ether);
    }

    /// <summary>
    /// Counts events for the chains of the person and tells the client what moved, in one message.
    /// </summary>
    public void Raise(Entity player, IReadOnlyList<(AchievementType Type, int Target)> events, long amount)
    {
        if (amount <= 0 || !TryGet(player, out var book))
        {
            return;
        }

        var updates = new List<AchieveUpdateValue>();

        foreach (var (type, target) in events)
        {
            if (book.Raise(catalog, (ushort)type, target, amount) is { } update)
            {
                updates.Add(update);
            }
        }

        if (updates.Count == 0)
        {
            return;
        }

        world.MarkDirty(player, PersonSections.Progress);

        Send(player, new PersonAchieveUpdateResponse
        {
            Updates = updates.Take(MaxListLength).ToArray(),
            RewardBits = book.Rewarded,
            Categories = book.Categories(catalog)
        });
    }

    /// <summary>
    /// The person takes the reward of an achievement it has reached: once, all of it or nothing. The items go into the bags,
    /// the gold, battle points and ether into the wallet; the titles are handed back for whoever keeps them. An achievement
    /// that is not reached, unknown or already rewarded gets no answer, as the client asks only for one it shows as ready.
    /// </summary>
    public AchievementReward TakeReward(Entity player, int achievement)
    {
        if (!TryGet(player, out var book))
        {
            return AchievementReward.Silent;
        }

        var claim = book.Check(catalog, achievement, out var rule);

        if (claim != AchievementClaim.Ready)
        {
            LogNotGiven(logger, achievement, claim);
            return AchievementReward.Silent;
        }

        if (Overflows(player, ShopCurrency.Gold, rule.Gold))
        {
            return new AchievementReward(false, GoldOverflow, []);
        }

        if (Overflows(player, ShopCurrency.Bp, rule.BattlePoints))
        {
            return new AchievementReward(false, BattlePointOverflow, []);
        }

        if (Overflows(player, ShopCurrency.Ether, rule.Ether))
        {
            return new AchievementReward(false, EtherOverflow, []);
        }

        var given = items.Resolve(player, rule.Items);

        if (!items.TryGive(player, given))
        {
            return new AchievementReward(false, DoesNotFit, []);
        }

        book.SetRewarded(rule.CompleteBit);

        Pay(player, ShopCurrency.Gold, rule.Gold);
        Pay(player, ShopCurrency.Bp, rule.BattlePoints);
        Pay(player, ShopCurrency.Ether, rule.Ether);

        world.MarkDirty(player, PersonSections.Progress);

        return new AchievementReward(true, 0, rule.Titles);
    }

    private bool Overflows(Entity player, ShopCurrency currency, long amount)
    {
        return !inventory.CanReceive(player, currency, amount);
    }

    private void Pay(Entity player, ShopCurrency currency, long amount)
    {
        if (amount > 0)
        {
            inventory.TryAddCurrency(player, currency, amount);
        }
    }

    private bool TryGet(Entity player, out AchievementBook book)
    {
        if (world.IsAlive(player) && world.TryGet<AchievementComponent>(player, out var component) && component.Book is not null)
        {
            book = component.Book;
            return true;
        }

        book = null!;
        return false;
    }

    private void Send(Entity player, IResponseHotspotMessage message)
    {
        if (world.IsAlive(player) && world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(message);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The reward of achievement {Achievement} is not given: {Claim}")]
    private static partial void LogNotGiven(ILogger logger, int achievement, AchievementClaim claim);
}
