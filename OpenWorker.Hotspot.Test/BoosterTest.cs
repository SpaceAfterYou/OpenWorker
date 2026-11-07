using System.Text.Json;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class BoosterTest
{
    private const long Now = 1_800_000_000;

    private static BoosterDefinition Booster(short id, BoosterTime time, int duration = 60_000, short group = 1, params BoosterEffectValue[] effects)
    {
        return new BoosterDefinition(id, group, time, duration, effects);
    }

    [TestMethod]
    public void EndlessBoosterHasNoRemainingTime()
    {
        var booster = Booster(1, BoosterTime.Endless);

        Assert.AreEqual(BoosterLoadEntry.Endless, BoosterRules.GetRemainSeconds(booster, new BoosterEntry(), Now));
        Assert.IsFalse(BoosterRules.IsOver(booster, new BoosterEntry(), Now));
    }

    [TestMethod]
    public void BoosterUntilAMomentCountsToIt()
    {
        var booster = Booster(1, BoosterTime.Until);
        var entry = new BoosterEntry { Remain = Now + 90 };

        Assert.AreEqual(90, BoosterRules.GetRemainSeconds(booster, entry, Now));
        Assert.IsFalse(BoosterRules.IsOver(booster, entry, Now + 89));
        Assert.IsTrue(BoosterRules.IsOver(booster, entry, Now + 90));
        Assert.AreEqual(0, BoosterRules.GetRemainSeconds(booster, entry, Now + 500));
    }

    [TestMethod]
    public void BoosterWithTimeLeftCountsMillisecondsAsSeconds()
    {
        var booster = Booster(1, BoosterTime.Online);
        var entry = new BoosterEntry { Remain = 90_500 };

        Assert.AreEqual(90, BoosterRules.GetRemainSeconds(booster, entry, Now));
        Assert.IsFalse(BoosterRules.IsOver(booster, entry, Now));

        entry.Remain = 0;

        Assert.IsTrue(BoosterRules.IsOver(booster, entry, Now));
    }

    [TestMethod]
    public void TimeGoesOnlyWhereTheBoosterSays()
    {
        Assert.IsTrue(BoosterRules.Consumes(BoosterTime.District, BoosterConsumeArea.District));
        Assert.IsFalse(BoosterRules.Consumes(BoosterTime.District, BoosterConsumeArea.Maze));
        Assert.IsTrue(BoosterRules.Consumes(BoosterTime.Maze, BoosterConsumeArea.Maze));
        Assert.IsFalse(BoosterRules.Consumes(BoosterTime.Maze, BoosterConsumeArea.District));
        Assert.IsTrue(BoosterRules.Consumes(BoosterTime.Online, BoosterConsumeArea.Maze));
        Assert.IsFalse(BoosterRules.Consumes(BoosterTime.Until, BoosterConsumeArea.District));
        Assert.IsFalse(BoosterRules.Consumes(BoosterTime.Endless, BoosterConsumeArea.District));
    }

    [TestMethod]
    public void ConsumeTakesTheTimeAndTellsIfItDid()
    {
        var booster = Booster(1, BoosterTime.District);
        var entry = new BoosterEntry { Remain = 60_000 };

        Assert.IsFalse(BoosterRules.Consume(booster, entry, BoosterConsumeArea.Maze, 1000));
        Assert.AreEqual(60_000, entry.Remain);

        Assert.IsTrue(BoosterRules.Consume(booster, entry, BoosterConsumeArea.District, 1000));
        Assert.AreEqual(59_000, entry.Remain);
    }

    [TestMethod]
    public void ChargeAddsTheTimeOfTheBooster()
    {
        var timed = Booster(1, BoosterTime.Online);
        var until = Booster(2, BoosterTime.Until);
        var entry = new BoosterEntry();

        BoosterRules.Charge(timed, entry, 60_000, Now);
        BoosterRules.Charge(timed, entry, 60_000, Now);

        Assert.AreEqual(120_000, entry.Remain);

        var calendar = new BoosterEntry();

        BoosterRules.Charge(until, calendar, 90_000, Now);

        Assert.AreEqual(Now + 90, calendar.Remain);

        // A booster still running goes on from its end.
        BoosterRules.Charge(until, calendar, 30_000, Now + 10);

        Assert.AreEqual(Now + 120, calendar.Remain);
    }

    [TestMethod]
    public void ChargeOfAnEndlessBoosterChangesNothing()
    {
        var entry = new BoosterEntry();

        BoosterRules.Charge(Booster(1, BoosterTime.Endless), entry, 60_000, Now);

        Assert.AreEqual(0, entry.Remain);
    }

    [TestMethod]
    public void EffectsAddUpByRateAndByValue()
    {
        var exp = new BoosterEffectValue(BoosterEffect.IncExp, BoosterApply.Rate, 0.2f);
        var more = new BoosterEffectValue(BoosterEffect.IncExp, BoosterApply.Rate, 0.3f);
        var flat = new BoosterEffectValue(BoosterEffect.IncExp, BoosterApply.Value, 10);
        var other = new BoosterEffectValue(BoosterEffect.IncGoldDrop, BoosterApply.Rate, 5);

        BoosterDefinition[] boosters = [Booster(1, BoosterTime.Online, 1, 1, exp, other), Booster(2, BoosterTime.Online, 1, 2, more, flat)];

        Assert.AreEqual(0.5f, BoosterRules.GetRate(boosters, BoosterEffect.IncExp), 0.0001f);
        Assert.AreEqual(10f, BoosterRules.GetValue(boosters, BoosterEffect.IncExp), 0.0001f);
        Assert.AreEqual(100f + 50f + 10f, BoosterRules.Apply(boosters, BoosterEffect.IncExp, 100f), 0.0001f);
        Assert.AreEqual(100f, BoosterRules.Apply(boosters, BoosterEffect.IncEtherDrop, 100f), 0.0001f);
    }

    [TestMethod]
    public void BookSurvivesTheStore()
    {
        var book = new BoosterBook { Boosters = [new BoosterEntry { Booster = 2154, Remain = 3_600_000, Account = true }] };

        var back = JsonSerializer.Deserialize<BoosterBook>(JsonSerializer.Serialize(book))!;

        Assert.HasCount(1, back.Boosters);
        Assert.AreEqual((short)2154, back.Boosters[0].Booster);
        Assert.AreEqual(3_600_000, back.Boosters[0].Remain);
        Assert.IsTrue(back.Boosters[0].Account);
    }

    [TestMethod]
    public void TableRowsBecomeDefinitions()
    {
        var catalog = new BoosterCatalog(TestTables.Load<BoosterRow>("tb_Booster"));

        Assert.IsTrue(catalog.TryGet(1, out var party));
        Assert.AreEqual(BoosterTime.District, party.Time);
        Assert.AreEqual(60_000, party.DurationMilliseconds);
        Assert.AreEqual((short)1, party.Group);
        CollectionAssert.AreEqual(new[] { new BoosterEffectValue(BoosterEffect.IncExp, BoosterApply.Rate, 2000f) }, party.Effects.ToArray());

        Assert.IsTrue(catalog.TryGet(2154, out var item));
        Assert.AreEqual(BoosterTime.Online, item.Time);
        Assert.AreEqual(3_600_000, item.DurationMilliseconds);
        Assert.AreEqual((short)2151, item.Group);
        Assert.HasCount(3, item.Effects);

        Assert.IsTrue(catalog.TryGet(101, out var echelon));
        Assert.AreEqual(BoosterTime.Endless, echelon.Time);
    }

    [TestMethod]
    public void EveryBoosterOfTheTableHasAnEffectAndATimeThatSuitsIt()
    {
        var catalog = new BoosterCatalog(TestTables.Load<BoosterRow>("tb_Booster"));

        foreach (var booster in catalog.All)
        {
            Assert.IsTrue(Enum.IsDefined(booster.Time), $"{booster.Id}");
            Assert.IsNotEmpty(booster.Effects, $"{booster.Id}");
            // The apply column of a status effect holds the buff to give.
            Assert.IsTrue(booster.Effects.All(e => Enum.IsDefined(e.Effect) && (e.Effect == BoosterEffect.Status || Enum.IsDefined(e.Apply))), $"{booster.Id}");

            // A booster that runs out by itself and has no time of its own has to be given a time.
            if (booster.Time is BoosterTime.District or BoosterTime.Maze or BoosterTime.Online)
            {
                Assert.IsGreaterThan(0, booster.DurationMilliseconds, $"{booster.Id}");
            }
        }
    }
}
