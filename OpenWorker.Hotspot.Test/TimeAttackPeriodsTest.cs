using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Ranking;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class TimeAttackPeriodsTest
{
    private const short Map = 21011;
    private const int Potion = 100;
    private const int Unknown = 200;

    // A reward of the client's table that its item table lacks, and the medibox given in its place.
    private const int CrystalPotion = 610901000;
    private const int BitMedibox = 610101001;

    /// <summary>
    /// A period ends at midnight of Monday, once: a server that starts later ends the one it missed, and each place the table
    /// rewards gets its item by a letter of the game to its account, in stacks.
    /// </summary>
    [TestMethod]
    public async Task APeriodEndsAtMondayMidnightOnceAndEachRewardedPlaceGetsALetter()
    {
        // Sunday, a minute before the end of the week.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 23, 59, 0, TimeSpan.Zero));
        time.SetLocalTimeZone(TimeZoneInfo.Utc);

        using var world = World.Create();
        var store = new Store();
        var periods = Periods(world, store, time);

        await periods.TickAsync(CancellationToken.None);

        Assert.AreEqual(new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc), store.Ends.Single(), "the period that ended before the start");

        await periods.TickAsync(CancellationToken.None);
        Assert.HasCount(1, store.Asked, "an ended period is not asked for again");

        time.Advance(TimeSpan.FromMinutes(1));
        await periods.TickAsync(CancellationToken.None);
        await periods.TickAsync(CancellationToken.None);

        Assert.AreEqual(new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc), store.Ends[^1]);
        Assert.HasCount(2, store.Ends);

        // Place 1 gets five potions in stacks of two; place 2 its stand-in for a reward the item table lacks; place 3 names
        // an item the game lacks and nothing stands in for.
        var letters = store.Letters[^1];

        Assert.HasCount(2, letters);

        var letter = letters[0];

        Assert.AreEqual(1001, letter.Account);
        Assert.AreEqual(GameLetters.OwnText, letter.SubType);
        CollectionAssert.AreEqual(new short[] { 2, 2, 1 }, letter.Items.Select(e => e.Count).ToArray());
        Assert.IsTrue(letter.Items.All(e => e.Item == Potion));

        Assert.AreEqual(1002, letters[1].Account);
        Assert.AreEqual((BitMedibox, (short)20), (letters[1].Items.Single().Item, letters[1].Items.Single().Count));
    }

    /// <summary>
    /// The game master ends the period at once, at this moment.
    /// </summary>
    [TestMethod]
    public async Task EndingNowEndsThePeriodAtThisMoment()
    {
        var now = new DateTimeOffset(2026, 11, 18, 15, 30, 0, TimeSpan.Zero);
        using var world = World.Create();
        var store = new Store();

        Assert.AreEqual(2, await Periods(world, store, new FakeTimeProvider(now)).EndNowAsync());
        Assert.AreEqual(now.UtcDateTime, store.Ends.Single());
    }

    private static TimeAttackPeriods Periods(World world, Store store, TimeProvider time)
    {
        var catalog = new ItemCatalog(
            new ReadOnlyCollection<ItemRow>([
                TestRows.Build<ItemRow>(("Id", Potion), ("Stack", (short)2)),
                TestRows.Build<ItemRow>(("Id", BitMedibox), ("Stack", (short)50))
            ]),
            new ReadOnlyCollection<ItemClassifyRow>([]),
            new ReadOnlyCollection<InvenSlotExtendRow>([]),
            new ReadOnlyCollection<BankSlotExtendRow>([]));

        var rewards = TimeAttackRewards.From((Map, 1, Potion, 5), (Map, 2, CrystalPotion, 20), (Map, 3, Unknown, 1));

        return new TimeAttackPeriods(world, store, rewards, new InventoryService(world, catalog, new LocalItemSerials()), time: time);
    }

    private sealed class Store : IRankingStore
    {
        public List<DateTime> Asked { get; } = [];

        public List<DateTime> Ends { get; } = [];

        public List<IReadOnlyList<PostAccountDraft>> Letters { get; } = [];

        public Task<bool> SubmitAsync(RankingKind kind, int scope, int person, int time, int score, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<(IReadOnlyList<RankingRow> Top, RankingRow? Mine)> RankingAsync(RankingKind kind, int scope, int person, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(IReadOnlyList<RankingRow>, RankingRow?)>(([], null));
        }

        public Task<IReadOnlyList<RankingPlace>?> PeriodPlacesAsync(RankingKind kind, int scope, DateTime endedAt, int places, CancellationToken cancellationToken = default)
        {
            Asked.Add(endedAt);

            IReadOnlyList<RankingPlace>? found = Ends.Contains(endedAt) ? null : [new(1, 1, 1001), new(2, 2, 1002), new(3, 3, 1003)];

            return Task.FromResult(found);
        }

        public Task<IReadOnlyList<PostPersistent>?> EndPeriodAsync(RankingKind kind, int scope, DateTime endedAt, IReadOnlyList<PostAccountDraft> letters, CancellationToken cancellationToken = default)
        {
            Ends.Add(endedAt);
            Letters.Add(letters);

            IReadOnlyList<PostPersistent> written = letters.Select(e => new PostPersistent { Kind = PostKind.Account, ReceiverAccountId = e.Account, SenderName = e.SenderName, Title = e.Title, Body = e.Body }).ToList();

            return Task.FromResult<IReadOnlyList<PostPersistent>?>(written);
        }
    }
}
