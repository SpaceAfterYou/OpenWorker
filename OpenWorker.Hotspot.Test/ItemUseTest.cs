using System.Collections.ObjectModel;
using Arch.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using OpenWorker.Domain.Enums;
using OpenWorker.Extensions;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The use of an item (08/11): the checks every item goes through in the order of the original server, the places a kind of
/// item is for, the cooldown of its group and the limit of a day. Needs the client tables (OPENWORKER_TEST_RES).
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class ItemUseTest
{
    /// <summary>
    /// The place items act in, as a test sets it, and the buffs it was asked to give.
    /// </summary>
    private sealed class Field : IItemField
    {
        public ItemPlace Place { get; set; } = ItemPlace.District;

        public List<int> Given { get; } = [];

        public List<int> Auras { get; } = [];

        public ItemPlace PlaceOf(Entity player) => Place;

        public bool GiveAura(Entity player, int aura)
        {
            Auras.Add(aura);

            return true;
        }

        public bool GiveBuff(Entity player, int buff)
        {
            Given.Add(buff);

            return true;
        }
    }

    /// <summary>
    /// The revive items as a test sets them: the code its check answers with, and the calls that got somebody up.
    /// </summary>
    private sealed class Revives : IReviveItems
    {
        public ushort? Refusal { get; set; }

        public List<ReviveItemKind> GotUp { get; } = [];

        public ushort? Check(Entity player, ReviveItemKind kind) => Refusal;

        public void GetUp(Entity player, ReviveItemKind kind) => GotUp.Add(kind);
    }

    /// <summary>
    /// The slots a test has put up in a trade or a shop.
    /// </summary>
    private sealed class Locks : IItemLocks
    {
        public HashSet<(StorageGroup, short)> Held { get; } = [];

        public bool IsLocked(Entity player, StorageGroup storage, short slot) => Held.Contains((storage, slot));
    }

    private static ItemPlace Maze { get; } = new(true, true, true, false, false);

    private sealed class Rig : IDisposable
    {
        public ItemSetupFixture Fixture { get; } = new();

        public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));

        public Field Field { get; } = new();

        public Revives Revives { get; } = new();

        public Locks Locks { get; } = new();

        public ItemUseLimits Limits { get; }

        public ItemUseService Use { get; }

        public ReadOnlyCollection<ItemRow> Items { get; } = ItemSetupFixture.Load<ItemRow>("tb_Item");

        public AchievementChains? Chains { get; }

        public Rig(bool counting = false)
        {
            Chains = counting ? new AchievementChains(Fixture.World, (AchievementType.ItemUsed, 0)) : null;
            Chains?.Enter(Fixture.Player);

            Limits = new ItemUseLimits(Fixture.World, new PersonStateStore(null!), Time, Options.Create(new DailyMissionOptions()));
            Use = new ItemUseService(Fixture.World, Fixture.Inventory, Fixture.Catalog, Limits, new([]), new([]), new([]), Field, revive: Revives, achievements: Chains?.Manager, locks: Locks);
        }

        /// <summary>
        /// A potion of the tables a person of the first level may use: it gives a buff and has a cooldown of a few seconds.
        /// </summary>
        public ItemRow Potion(Func<ItemRow, bool>? also = null)
        {
            return Items.First(e => Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EFFECT } &&
                                    e is { SubType: 0, EffectType: 1, LevelLimit: <= 1, ClassLimit: 0, CooldownMilliseconds: >= 2000 } &&
                                    also?.Invoke(e) != false);
        }

        public ValueTask<ItemUseResult> UseAsync(ItemInstance item) =>
            Use.UseAsync(Fixture.Player, item.Storage, item.Slot, item.Serial, CancellationToken.None);

        public void Dispose() => Fixture.Dispose();
    }

    [TestMethod]
    public async Task APotionActsOnlyInAMazeThatAllowsItsKindAndOnlyOutsideTheInfiniteTower()
    {
        using var rig = new Rig();

        var potion = rig.Potion();
        var item = rig.Fixture.Give(potion.Id, 5);

        Assert.AreEqual(ItemUseCodes.WrongPlace, (await rig.UseAsync(item)).Error, "The use state of a potion names mazes only, so a city refuses it");

        rig.Field.Place = Maze with { ItemsAllowed = false };

        Assert.AreEqual(ItemUseCodes.WrongPlace, (await rig.UseAsync(item)).Error, "A maze that forbids potions refuses it");

        rig.Field.Place = Maze with { Infinite = true };

        Assert.AreEqual(ItemUseCodes.InfiniteTower, (await rig.UseAsync(item)).Error, "The infinite tower takes no item");
        Assert.AreEqual(5, item.Count, "Nothing was used up by a refusal");
        Assert.AreEqual(0, rig.Field.Given.Count, "No buff was given by a refusal");

        rig.Field.Place = Maze;

        var used = await rig.UseAsync(item);

        Assert.IsTrue(used.Ok, "The potion is used in a maze");
        Assert.IsTrue(used.Motion, "A potion plays a motion that the others see");
        Assert.AreEqual((potion.Id, item.Slot), (used.Item, used.Slot), "The motion names the item and its slot");
        CollectionAssert.AreEqual(new[] { potion.EffectValue }, rig.Field.Given, "The buff of the item is given");
        Assert.AreEqual(4, item.Count, "One potion is used up");
    }

    [TestMethod]
    public async Task ARevivalItemIsRefusedWithTheCodeOfTheMazeAndElseGetsTheFallenUpOrGivesItsBuff()
    {
        using var rig = new Rig();

        rig.Field.Place = Maze;

        var call = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EFFECT } && e is { SubType: 2, EffectType: 1 });
        var buff = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EFFECT } && e is { SubType: 1, EffectType: 1 });
        var calls = rig.Fixture.Give(call.Id, 2);
        var buffs = rig.Fixture.Give(buff.Id, 2);

        rig.Revives.Refusal = 52608;

        Assert.AreEqual(52608, (await rig.UseAsync(calls)).Error, "The maze's refusal is the answer");
        Assert.AreEqual(2, calls.Count, "A refused call is not spent");

        rig.Revives.Refusal = null;

        var used = await rig.UseAsync(calls);

        Assert.IsTrue(used.Ok && used.Motion, "A call is used with the motion of a potion");
        CollectionAssert.AreEqual(new[] { ReviveItemKind.Call }, rig.Revives.GotUp, "The call gets the fallen up");
        Assert.AreEqual(1, calls.Count);

        Assert.AreEqual(ItemUseCodes.Cooling, (await rig.UseAsync(buffs)).Error, "The calls and the buffs share a cooldown");

        rig.Time.Advance(TimeSpan.FromMilliseconds(buff.CooldownMilliseconds));

        Assert.IsTrue((await rig.UseAsync(buffs)).Ok, "The buff is used when the cooldown is over");
        CollectionAssert.AreEqual(new[] { buff.EffectValue }, rig.Field.Given, "It gives its buff and gets nobody up");
        Assert.AreEqual(1, rig.Revives.GotUp.Count);

        // The item of the party's buff puts its aura on the player.
        var aura = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EFFECT } && e is { SubType: 1, EffectType: 2 });
        var auras = rig.Fixture.Give(aura.Id, 1);

        rig.Time.Advance(TimeSpan.FromMilliseconds(aura.CooldownMilliseconds));

        Assert.IsTrue((await rig.UseAsync(auras)).Ok);
        CollectionAssert.AreEqual(new[] { aura.EffectValue }, rig.Field.Auras, "An aura item gives its aura");
    }

    [TestMethod]
    public async Task OnlyAPotionOrAFatigueItemThatIsUsedCountsForTheAchievements()
    {
        using var rig = new Rig(counting: true);

        rig.Field.Place = Maze;

        var item = rig.Fixture.Give(rig.Potion().Id, 5);

        Assert.IsTrue((await rig.UseAsync(item)).Ok);
        Assert.AreEqual(1, rig.Chains!.Counted(rig.Fixture.Player, AchievementType.ItemUsed));

        // Inside the cooldown the use is refused and counts for nothing.
        Assert.AreEqual(ItemUseCodes.Cooling, (await rig.UseAsync(item)).Error);
        Assert.AreEqual(1, rig.Chains.Counted(rig.Fixture.Player, AchievementType.ItemUsed));

        // An item of another kind is used without counting.
        var place = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EQUIP_SLOT_OPEN } && e.LevelLimit <= 1);

        Assert.IsTrue((await rig.UseAsync(rig.Fixture.Give(place.Id, 1))).Ok);
        Assert.AreEqual(1, rig.Chains.Counted(rig.Fixture.Player, AchievementType.ItemUsed));
    }

    [TestMethod]
    public async Task AnItemOfferedInATradeOrAShopIsNotUsedUntilItIsTakenBack()
    {
        using var rig = new Rig();

        rig.Field.Place = Maze;

        var item = rig.Fixture.Give(rig.Potion().Id, 2);

        rig.Locks.Held.Add((item.Storage, item.Slot));

        Assert.AreEqual(ItemUseCodes.NotUsable, (await rig.UseAsync(item)).Error, "An offered item is refused as one that is not there");
        Assert.AreEqual(2, item.Count);

        rig.Locks.Held.Clear();

        Assert.IsTrue((await rig.UseAsync(item)).Ok, "Taken back, it is used");
    }

    [TestMethod]
    public async Task AnItemIsCheckedForLevelThenClassThenFallingThenItsCooldown()
    {
        using var rig = new Rig();

        rig.Field.Place = Maze with { Down = true };

        var potion = rig.Potion();
        var high = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.EFFECT } && e is { SubType: 0, LevelLimit: > 1 });

        // Gear of another class: the checks every item goes through come before what its kind does.
        var other = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.ABILITY } && e is { LevelLimit: <= 1, ClassLimit: > 0 } &&
                                         e.ClassLimit != (byte)Hero.Haru && rig.Fixture.Catalog.HomeOf(e.Id) == StorageGroup.Common);

        var tooHigh = rig.Fixture.Give(high.Id, 1);
        var forAnother = rig.Fixture.Give(other.Id, 1);
        var plain = rig.Fixture.Give(potion.Id, 3);

        Assert.AreEqual(ItemUseCodes.LevelTooLow, (await rig.UseAsync(tooHigh)).Error, "The level comes first, before a fall");
        Assert.AreEqual(ItemUseCodes.OtherClass, (await rig.UseAsync(forAnother)).Error, "Then the class");
        Assert.AreEqual(ItemUseCodes.Fallen, (await rig.UseAsync(plain)).Error, "Then a person who is down uses no potion");

        rig.Field.Place = Maze;

        Assert.IsTrue((await rig.UseAsync(plain)).Ok, "On its feet it is used");
        Assert.AreEqual(ItemUseCodes.Cooling, (await rig.UseAsync(plain)).Error, "A second use inside the cooldown is refused");
    }

    [TestMethod]
    public async Task TheCooldownOfAGroupBindsEveryItemOfTheGroupAndEndsAfterItsWholeSeconds()
    {
        using var rig = new Rig();

        rig.Field.Place = Maze;

        var first = rig.Potion();
        var sibling = rig.Potion(e => e.Id != first.Id && e.CooldownGroup == first.CooldownGroup);
        var stranger = rig.Potion(e => e.CooldownGroup != first.CooldownGroup);

        var one = rig.Fixture.Give(first.Id, 3);
        var two = rig.Fixture.Give(sibling.Id, 3);
        var three = rig.Fixture.Give(stranger.Id, 3);

        Assert.IsTrue((await rig.UseAsync(one)).Ok);
        Assert.AreEqual(ItemUseCodes.Cooling, (await rig.UseAsync(two)).Error, "Another item of the group cools too");
        Assert.IsTrue((await rig.UseAsync(three)).Ok, "An item of another group does not");

        var seconds = first.CooldownMilliseconds / 1000;

        rig.Time.Advance(TimeSpan.FromSeconds(seconds - 1));

        Assert.AreEqual(ItemUseCodes.Cooling, (await rig.UseAsync(one)).Error, "A second before the end it still cools");

        rig.Time.Advance(TimeSpan.FromSeconds(1));

        Assert.IsTrue((await rig.UseAsync(one)).Ok, "At the end of the time the group is free");
    }

    [TestMethod]
    public void AKindLimitedForADayIsRefusedAfterThreeUsesAndFreeAgainOnTheNextGameDay()
    {
        using var rig = new Rig();

        const int type = (int)ItemUseType.FP;

        for (var i = 0; i < ItemUseLimits.DailyUses; i++)
        {
            Assert.IsTrue(rig.Limits.CanUseKind(rig.Fixture.Player, type), $"Use {i + 1} of the day is allowed");
            rig.Limits.CountUse(rig.Fixture.Player, type);
        }

        Assert.IsFalse(rig.Limits.CanUseKind(rig.Fixture.Player, type), "The fourth use of the day is refused");
        Assert.IsTrue(rig.Limits.CanUseKind(rig.Fixture.Player, (int)ItemUseType.FP_FREE), "Another kind counts for itself");

        // The game day starts at eleven in the morning: noon is past the start of today, so the next start is tomorrow's.
        rig.Time.Advance(TimeSpan.FromHours(23));

        Assert.IsTrue(rig.Limits.CanUseKind(rig.Fixture.Player, type), "Past the start of the next game day the count starts again");
        Assert.AreEqual(0, rig.Limits.UsesOf(rig.Fixture.Player, type));
    }

    [TestMethod]
    public void ARequestThatNamesAnItemTakesOnlyAnItemOfItsKind()
    {
        using var rig = new Rig();

        var potion = rig.Fixture.Give(rig.Potion().Id, 1);
        var reset = rig.Items.First(e => rig.Fixture.Catalog.Classify(e.Id) is { ItemUseType: ItemUseType.SKILL_RESET_ALL });
        var item = rig.Fixture.Give(reset.Id, 1);

        Assert.AreSame(item, rig.Use.ItemOfService(rig.Fixture.Player, item.Storage, item.Slot, ItemUseType.SKILL_RESET_ALL), "The item of the kind is the one");
        Assert.IsNull(rig.Use.ItemOfService(rig.Fixture.Player, item.Storage, item.Slot, ItemUseType.SKILL_RESET_SINGLE), "It is not an item of another kind");
        Assert.IsNull(rig.Use.ItemOfService(rig.Fixture.Player, potion.Storage, potion.Slot, ItemUseType.SKILL_RESET_ALL), "A potion is not a reset");
        Assert.IsNull(rig.Use.ItemOfService(rig.Fixture.Player, item.Storage, (short)(item.Slot + 1), ItemUseType.SKILL_RESET_ALL), "An empty slot has none");
    }
}
