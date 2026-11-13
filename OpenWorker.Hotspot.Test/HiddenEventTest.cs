using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Modules.HiddenEvents;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The hidden events of a maze: how a box starts one and picks its condition, how each kind of condition counts, and how the
/// clear decides an event still going.
/// </summary>
[TestClass]
public sealed class HiddenEventTest
{
    private const short Box = 6001;
    private const int Monster = 10101001;
    private const int PaidItem = 820000002;

    private static T Row<T>(Action<BinaryWriter> write) where T : struct
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        write(writer);
        stream.Position = 0;

        using var reader = new BinaryReader(stream);

        return (T)Activator.CreateInstance(typeof(T), reader)!;
    }

    private static HiddenEventRow Event(short id, params (int Condition, short Rate)[] conditions) => Row<HiddenEventRow>(w =>
    {
        w.Write(id);

        for (var i = 0; i < 5; i++)
        {
            w.Write(i < conditions.Length ? conditions[i].Condition : 0);
        }

        for (var i = 0; i < 5; i++)
        {
            w.Write(i < conditions.Length ? conditions[i].Rate : (short)0);
        }

        w.Write(819999999);
        w.Write((byte)1);
    });

    private static EventConditionRow Condition(int id, HiddenEventKind kind, HiddenEventTarget target, int targetId, int count) => Row<EventConditionRow>(w =>
    {
        w.Write(id);
        w.Write((short)6000);
        w.Write((byte)kind);
        w.Write((byte)target);
        w.Write(targetId);
        w.Write(count);
    });

    private static HiddenEventRewardRow Reward(ushort id, int item, byte count) => Row<HiddenEventRewardRow>(w =>
    {
        w.Write(id);
        w.Write(new byte[5 * 6]);
        w.Write(item);
        w.Write(count);
    });

    private static HiddenEventCatalog Catalog(HiddenEventRow[] events, params EventConditionRow[] conditions) => new(
        new ReadOnlyCollection<HiddenEventRow>(events),
        new ReadOnlyCollection<EventConditionRow>(conditions),
        new ReadOnlyCollection<HiddenEventRewardRow>([Reward((ushort)Box, PaidItem, 2)]),
        new ReadOnlyCollection<SkillRow>([]),
        random: new Random(7));

    private static HiddenEvent Running(HiddenEventKind kind, HiddenEventTarget target, int targetId, int count)
    {
        var started = Catalog([Event(Box, (1, 1))], Condition(1, kind, target, targetId, count)).Pick(Box)!;

        started.Begin();

        return started;
    }

    [TestMethod]
    public void ABoxStartsItsEventByItsChanceWithAConditionTheServerCounts()
    {
        // The jumps are not counted by the server: the kill is picked whatever its weight.
        var catalog = Catalog(
            [Event(Box, (1, 9000), (2, 1000)), Event((short)(Box + 1), (1, 10000))],
            Condition(1, HiddenEventKind.Reach, HiddenEventTarget.Jump, 0, 10),
            Condition(2, HiddenEventKind.Kill, HiddenEventTarget.Monster, Monster, 1));

        Assert.IsNull(catalog.TryStart(default(Entity), Box, 0f), "a box whose chance is nothing starts nothing");
        Assert.IsNull(catalog.Pick(Box + 1), "an event with no condition the server counts never starts");

        for (var i = 0; i < 20; i++)
        {
            var started = catalog.TryStart(default(Entity), Box, 10000f);

            Assert.IsNotNull(started, "a certain chance always starts the event");
            Assert.AreEqual(2, started.Condition.Id);
            Assert.AreEqual(HiddenEventState.Started, started.State);

            started.Begin();
            Assert.IsTrue(started.Count(HiddenEventTarget.Monster, Monster, 1));
            Assert.AreEqual(new HiddenEventReward(PaidItem, 2), started.Reward, "the reward is the server's");
        }
    }

    [TestMethod]
    public void EachKindOfConditionCountsWhatItNames()
    {
        var kill = Running(HiddenEventKind.Kill, HiddenEventTarget.Monster, Monster, 2);

        Assert.IsFalse(kill.Count(HiddenEventTarget.Monster, Monster + 1, 1), "another monster");
        Assert.IsFalse(kill.Count(HiddenEventTarget.Gold, Monster, 1), "another target");
        Assert.IsTrue(kill.Count(HiddenEventTarget.Monster, Monster, 1));
        Assert.AreEqual(HiddenEventState.Progress, kill.State);
        Assert.IsTrue(kill.Count(HiddenEventTarget.Monster, Monster, 1));
        Assert.AreEqual(HiddenEventState.Met, kill.State);
        Assert.IsFalse(kill.Count(HiddenEventTarget.Monster, Monster, 1), "a met event counts no more");

        var combo = Running(HiddenEventKind.Reach, HiddenEventTarget.MaxCombo, 0, 50);

        Assert.IsTrue(combo.Count(HiddenEventTarget.MaxCombo, 0, 30));
        Assert.IsFalse(combo.Count(HiddenEventTarget.MaxCombo, 0, 12), "a shorter combo keeps the longest");
        Assert.AreEqual(30, combo.Counted);
        Assert.IsTrue(combo.Count(HiddenEventTarget.MaxCombo, 0, 50));
        Assert.AreEqual(HiddenEventState.Met, combo.State);

        var gold = Running(HiddenEventKind.Collect, HiddenEventTarget.Gold, 0, 100);

        Assert.IsTrue(gold.Count(HiddenEventTarget.Gold, 800000001, 60));
        Assert.IsTrue(gold.Count(HiddenEventTarget.Gold, 800000001, 60));
        Assert.AreEqual(HiddenEventState.Met, gold.State, "amounts add up");
    }

    [TestMethod]
    public void TheClearMeetsAnEventThatOnlyAskedToKeepUnderACountAndFailsTheRest()
    {
        var untouched = Running(HiddenEventKind.UseLess, HiddenEventTarget.ErosionSkill, 0, 0);

        Assert.IsTrue(untouched.Finish());
        Assert.AreEqual(HiddenEventState.Met, untouched.State);

        var used = Running(HiddenEventKind.UseLess, HiddenEventTarget.ErosionSkill, 0, 0);

        Assert.IsTrue(used.Count(HiddenEventTarget.ErosionSkill, 0, 1));
        Assert.AreEqual(HiddenEventState.Failed, used.State, "one use past the count fails at once");
        Assert.IsFalse(used.Finish());
        Assert.IsNull(used.Reward);

        var unfinished = Running(HiddenEventKind.Kill, HiddenEventTarget.Monster, 0, 5);

        unfinished.Count(HiddenEventTarget.Monster, Monster, 1);

        Assert.IsTrue(unfinished.Finish());
        Assert.AreEqual(HiddenEventState.Failed, unfinished.State, "a count not reached by the clear fails");
        Assert.IsNull(unfinished.Reward);
    }
}
