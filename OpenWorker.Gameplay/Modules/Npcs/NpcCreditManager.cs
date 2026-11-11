using Arch.Core;
using Microsoft.Extensions.Logging;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs.Components;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Responses;

namespace OpenWorker.Gameplay.Modules.Npcs;

/// <summary>
/// The credit a person earns with a group of NPCs by buying, repairing, making, upgrading, exchanging, evolving and riding
/// at them. Each grade it reaches gives an item and the benefits of its row: a part off the price of a service, a better
/// chance of an upgrade, items of a shop it may buy. What a day gives is limited by the grade, and the day starts at the
/// hour the other daily things of the game start. A change marks <see cref="PersonSections.MiscState"/> and the flush writes
/// the book.
/// </summary>
public sealed partial class NpcCreditManager(
    World world,
    PersonStateStore store,
    NpcCreditTables tables,
    InventoryService inventory,
    TimeProvider time,
    ILogger<NpcCreditManager> logger,
    BoosterManager? boosters = null,
    Random? random = null,
    AchievementManager? achievements = null)
{
    /// <summary>
    /// The benefits and the boosters are parts out of this.
    /// </summary>
    public const int Parts = 10_000;

    /// <summary>
    /// The hour of the morning the credit of a day starts again, as the other daily things of the game.
    /// </summary>
    public static readonly TimeSpan DayStart = TimeSpan.FromHours(9);

    /// <summary>
    /// The most groups the client takes in one list: it counts them in a byte.
    /// </summary>
    private const int MaxGroups = byte.MaxValue;

    private readonly Random _random = random ?? Random.Shared;

    /// <summary>
    /// The book of the credit as the flush writes it.
    /// </summary>
    public static IPersonDocumentSource Document { get; } =
        new PersonDocumentSource<NpcCreditComponent>(PersonStateKind.NpcCredit, PersonSections.MiscState, e => e.Book);

    /// <summary>
    /// Loads the credit of the person as it enters a city, starts a new day where one has passed, gives what an earlier
    /// grade left for lack of room, and tells the client every group.
    /// </summary>
    public async ValueTask EnterAsync(Entity player, int person, CancellationToken cancellationToken)
    {
        var book = await store.LoadAsync<NpcCreditBook>(person, PersonStateKind.NpcCredit, cancellationToken) ?? new NpcCreditBook();

        if (!world.IsAlive(player))
        {
            return;
        }

        world.Set(player, new NpcCreditComponent { Person = person, Book = book });

        if (book.StartDay(Today()) | GiveRewards(player, book))
        {
            world.MarkDirty(player, PersonSections.MiscState);
        }

        Send(player, new PersonNpcCreditLoadResponse { Credits = book.Groups.Take(MaxGroups).Select(e => e.ToValue()).ToArray() });
    }

    /// <summary>
    /// The grade of the person with a group, 0 for a group it has no credit with.
    /// </summary>
    public byte GradeOf(Entity player, int group)
    {
        return TryGet(player, out var book) && book.Find(group) is { } entry ? entry.Grade : (byte)0;
    }

    /// <summary>
    /// The value of a benefit the grade of the person with the group of the NPC gives, out of ten thousand; 0 without it.
    /// </summary>
    public int Benefit(Entity player, ServiceNpc npc, CreditBenefit benefit)
    {
        return tables.BenefitsOf(npc.Group, GradeOf(player, npc.Group)).Where(e => e.Type == (byte)benefit).Select(e => (int)e.Rate)
            .FirstOrDefault();
    }

    /// <summary>
    /// Whether the grade of the person with the group of the NPC lets it buy the item at a shop that asks for a grade.
    /// </summary>
    public bool Unlocks(Entity player, ServiceNpc npc, int item)
    {
        return tables.BenefitsOf(npc.Group, GradeOf(player, npc.Group))
            .Any(e => e.Type == (byte)CreditBenefit.GradeItems && (e.Item1 == item || e.Item2 == item || e.Item3 == item));
    }

    /// <summary>
    /// A price of a service of the NPC with the part its credit benefit and the boosters of the person that lower prices
    /// take off it, never below nothing.
    /// </summary>
    public long Discount(Entity player, ServiceNpc npc, CreditBenefit benefit, long price)
    {
        var parts = Benefit(player, npc, benefit) + (boosters?.GetRate(player, BoosterEffect.ReduceDec) ?? 0);

        if (price <= 0 || parts <= 0)
        {
            return Math.Max(0, price);
        }

        return Math.Max(0, price - (long)(price * (double)parts / Parts));
    }

    /// <summary>
    /// What the credit with the group of the NPC adds to the chance of an upgrade, out of ten thousand. The boosters of the
    /// person work on the chance of the upgrade itself, where it is rolled.
    /// </summary>
    public int UpgradeBonus(Entity player, ServiceNpc npc)
    {
        return Benefit(player, npc, CreditBenefit.UpgradeChance);
    }

    /// <summary>
    /// The person did what raises its credit with the group of the NPC: the credit grows by a roll between the least and the
    /// most of the row of that kind, up to what the grade allows in a day, the grades it passes give their items, and the
    /// client is told.
    /// </summary>
    public void Raise(Entity player, ServiceNpc npc, CreditRaise raise)
    {
        if (npc.Group == 0 || !TryGet(player, out var book) || !tables.TryRaise(npc.Group, raise, out var row) ||
            row.Maximum <= 0)
        {
            return;
        }

        var points = _random.Next(row.Minimum, Math.Max(row.Minimum, row.Maximum) + 1);

        book.StartDay(Today());

        if (Add(book, npc.Group, points, out var gained) is not { } entry)
        {
            return;
        }

        GiveRewards(player, book);

        world.MarkDirty(player, PersonSections.MiscState);

        LogRaised(logger, npc.Group, (byte)raise, entry.Grade, entry.Point);
        Send(player, new PersonNpcCreditUpdateResponse { Credit = entry.ToValue() });

        // What counts is what the day allowed, not what was rolled.
        achievements?.Count(player, AchievementType.NpcCredit, gained, npc.Group);
    }

    /// <summary>
    /// Adds credit to a group as far as the day of its grade allows, passing grades on the way; null when nothing was added.
    /// </summary>
    private NpcCreditEntry? Add(NpcCreditBook book, int group, int points, out int added)
    {
        added = 0;

        var entry = book.Find(group);
        var created = entry is null;

        entry ??= new NpcCreditEntry { Group = group, Day = Today() };

        if (points < 1 || entry.Grade >= NpcCreditTables.MaxGrade || !tables.TryGrade(group, entry.Grade + 1, out var next))
        {
            return null;
        }

        var room = next.DayLimit - entry.DayPoint;

        if (room <= 0)
        {
            return null;
        }

        var gained = Math.Min(points, room);

        added = gained;
        entry.DayPoint += (short)gained;

        var credit = entry.Point + gained;

        while (credit >= next.NeedCredit)
        {
            credit -= next.NeedCredit;
            entry.Grade = next.Grade;

            if (next.RewardItem != 0 && next.RewardCount > 0)
            {
                book.Rewards.Add(new NpcCreditReward { Item = next.RewardItem, Count = next.RewardCount });
            }

            if (entry.Grade >= NpcCreditTables.MaxGrade || !tables.TryGrade(group, entry.Grade + 1, out next))
            {
                credit = 0;
                break;
            }
        }

        entry.Point = credit;
        entry.UpdatedAt = time.GetUtcNow().ToUnixTimeSeconds();

        if (created)
        {
            book.Groups.Add(entry);
        }

        return entry;
    }

    /// <summary>
    /// Gives the items of the grades reached, as far as they fit the bags; the rest waits for room.
    /// </summary>
    private bool GiveRewards(Entity player, NpcCreditBook book)
    {
        var given = false;

        foreach (var reward in book.Rewards.ToArray())
        {
            if (!inventory.TryGive(player, reward.Item, reward.Count))
            {
                continue;
            }

            book.Rewards.Remove(reward);
            given = true;
        }

        return given;
    }

    private long Today() => GameDay.Get(time.GetLocalNow(), DayStart).Start.ToUnixTimeSeconds();

    private bool TryGet(Entity player, out NpcCreditBook book)
    {
        if (world.IsAlive(player) && world.TryGet<NpcCreditComponent>(player, out var component) && component.Book is not null)
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "The credit with group {Group} rose by a raise of kind {Raise}: grade {Grade}, {Point} points")]
    private static partial void LogRaised(ILogger logger, int group, byte raise, byte grade, int point);
}
