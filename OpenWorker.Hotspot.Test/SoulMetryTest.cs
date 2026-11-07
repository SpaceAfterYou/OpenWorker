using System.Text.Json;
using OpenWorker.Gameplay.Modules.SoulMetry;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SoulMetryTest
{
    private static SoulMetryDefinition ThreeObjects { get; } = new(
        32,
        1,
        [new SoulMetryObject(8111, 21221, 5000), new SoulMetryObject(8112, 21231, 5000), new SoulMetryObject(8113, 21241, 5000)],
        [new SoulMetryReward(2, 1001, 1)]);

    private static SoulMetryCatalog Catalog { get; } = SoulMetryCatalog.From([ThreeObjects]);

    [TestMethod]
    public void NewSoulMetryHasTheBitsOfItsObjectsCleared()
    {
        Assert.AreEqual((short)1016, ThreeObjects.InitialBits);
    }

    [TestMethod]
    public void SoulMetryStartsOnce()
    {
        var state = new SoulMetryState();

        Assert.IsTrue(state.TryAdd(ThreeObjects, out var bits));
        Assert.AreEqual((short)1016, bits);
        Assert.IsFalse(state.TryAdd(ThreeObjects, out _));
    }

    [TestMethod]
    public void FinishedSoulMetryDoesNotStartAgain()
    {
        var state = new SoulMetryState();

        Assert.IsTrue(state.TrySetComplete(ThreeObjects));
        Assert.IsTrue(state.IsComplete(ThreeObjects));
        Assert.IsFalse(state.TryAdd(ThreeObjects, out _));
    }

    [TestMethod]
    public void ObjectsAreFoundOneByOne()
    {
        var state = new SoulMetryState();
        state.TryAdd(ThreeObjects, out _);

        Assert.IsTrue(state.TryCollect(ThreeObjects, 8112, out var bits));
        Assert.AreEqual((short)1018, bits);
        Assert.IsFalse(state.IsReady(ThreeObjects));

        Assert.IsTrue(state.TryCollect(ThreeObjects, 8111, out bits));
        Assert.IsTrue(state.TryCollect(ThreeObjects, 8113, out bits));
        Assert.AreEqual((short)1023, bits);
        Assert.IsTrue(state.IsReady(ThreeObjects));
    }

    [TestMethod]
    public void ObjectIsFoundOnlyOnce()
    {
        var state = new SoulMetryState();
        state.TryAdd(ThreeObjects, out _);

        Assert.IsTrue(state.TryCollect(ThreeObjects, 8111, out _));
        Assert.IsFalse(state.TryCollect(ThreeObjects, 8111, out _));
    }

    [TestMethod]
    public void ForeignObjectAndNotStartedSoulMetryAreRefused()
    {
        var state = new SoulMetryState();

        Assert.IsFalse(state.TryCollect(ThreeObjects, 8111, out _));

        state.TryAdd(ThreeObjects, out _);

        Assert.IsFalse(state.TryCollect(ThreeObjects, 9999, out _));
    }

    [TestMethod]
    public void BitOutsideTheBitmapIsRefused()
    {
        var state = new SoulMetryState();
        var far = ThreeObjects with { CompleteBit = 128 * 8 };

        Assert.IsFalse(state.Fits(far));
        Assert.IsFalse(state.TrySetComplete(far));
        Assert.IsFalse(state.IsComplete(far));
    }

    [TestMethod]
    public void BitsOfDifferentSoulMetriesDoNotMix()
    {
        var state = new SoulMetryState();
        var other = ThreeObjects with { Id = 33, CompleteBit = 10 };

        state.TrySetComplete(other);

        Assert.IsTrue(state.IsComplete(other));
        Assert.IsFalse(state.IsComplete(ThreeObjects));
        Assert.AreEqual((byte)0b100, state.Complete[1]);
    }

    [TestMethod]
    public void LoadedStateIsMadeSafe()
    {
        var state = new SoulMetryState { Complete = new byte[3], Progress = { [32] = 1016, [999] = 5 } };

        state.Normalize(Catalog);

        Assert.HasCount(128, state.Complete);
        CollectionAssert.AreEquivalent(new short[] { 32 }, state.Progress.Keys.ToArray());
    }

    [TestMethod]
    public void StateSurvivesTheStore()
    {
        var state = new SoulMetryState { Progress = { [32] = 1017, [35] = 1023 } };
        state.TrySetComplete(ThreeObjects);

        var back = JsonSerializer.Deserialize<SoulMetryState>(JsonSerializer.Serialize(state))!;

        CollectionAssert.AreEqual(state.Progress.OrderBy(e => e.Key).ToArray(), back.Progress.OrderBy(e => e.Key).ToArray());
        CollectionAssert.AreEqual(state.Complete, back.Complete);
    }

    [TestMethod]
    public void TableRowsBecomeDefinitions()
    {
        var catalog = new SoulMetryCatalog(TestTables.Load<SoulMetryRow>("tb_soul_metry"));

        Assert.IsTrue(catalog.TryGet(32, out var first));
        Assert.AreEqual(1, first.CompleteBit);
        CollectionAssert.AreEqual(new[] { 8111, 8112, 8113 }, first.Objects.Select(e => e.Object).ToArray());
        CollectionAssert.AreEqual(new[] { 21221, 21231, 21241 }, first.Objects.Select(e => e.Maze).ToArray());
        CollectionAssert.AreEqual(new[] { new SoulMetryReward(2, 1001, 1) }, first.Rewards.ToArray());

        Assert.IsTrue(catalog.TryGet(35, out var two));
        Assert.HasCount(2, two.Rewards);

        Assert.IsTrue(catalog.TryGet(51, out var four));
        Assert.HasCount(4, four.Objects);
    }

    [TestMethod]
    public void EveryRowHasItsOwnBitAndObjects()
    {
        var rows = TestTables.Load<SoulMetryRow>("tb_soul_metry");
        var catalog = new SoulMetryCatalog(rows);

        Assert.AreEqual(rows.Count, catalog.All.Select(e => e.CompleteBit).Distinct().Count());

        foreach (var definition in catalog.All)
        {
            Assert.IsNotEmpty(definition.Objects, $"{definition.Id}");
            Assert.IsLessThanOrEqualTo(5, definition.Objects.Count);
            Assert.IsGreaterThan(0, definition.CompleteBit);
            Assert.IsTrue(new SoulMetryState().Fits(definition));
        }
    }
}
