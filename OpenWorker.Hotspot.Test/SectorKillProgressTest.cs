using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SectorKillProgressTest
{
    private static SectorKillProgress WithMonsters(ClearType type, int ratio, int count, params byte[] steps)
    {
        var progress = new SectorKillProgress(type, ratio, steps);

        for (var i = 0; i < count; i++)
        {
            progress.Added();
        }

        return progress;
    }

    [TestMethod]
    public void KillFinishesAtItsRatio()
    {
        var progress = WithMonsters(ClearType.Kill, 50, 4);

        progress.Removed();
        Assert.IsFalse(progress.IsCleared(0));

        progress.Removed();
        Assert.IsTrue(progress.IsCleared(0));
    }

    [TestMethod]
    public void PerfectKillWaitsForEverything()
    {
        var progress = WithMonsters(ClearType.KillPerpect, 100, 2);

        progress.Removed();
        Assert.IsFalse(progress.IsCleared(0));

        progress.Removed();
        Assert.IsFalse(progress.IsCleared(1), "A box that has not spawned yet still has to die.");
        Assert.IsTrue(progress.IsCleared(0));
    }

    [TestMethod]
    public void OtherSectorsAreLeftToTheirScript()
    {
        var progress = WithMonsters(ClearType.Always, 100, 1);

        progress.Removed();

        Assert.IsFalse(progress.IsCleared(0));
    }

    [TestMethod]
    public void NewWavesLowerTheShareOfTheDead()
    {
        var progress = WithMonsters(ClearType.Kill, 100, 2);

        progress.Removed();
        Assert.AreEqual(50f, progress.KilledPercent);

        progress.Added();
        Assert.AreEqual(100f - 2f / 3f * 100f, progress.KilledPercent, 0.001f);
    }

    [TestMethod]
    public void StepOpensOnceAtItsRatio()
    {
        var progress = WithMonsters(ClearType.KillPerpect, 100, 2, 50);

        progress.MarkCurrentStepSpawned();
        Assert.IsFalse(progress.TryOpenNextStep());

        progress.Removed();
        Assert.IsTrue(progress.TryOpenNextStep());
        Assert.AreEqual(1, progress.Step);
        Assert.IsFalse(progress.IsCurrentStepSpawned);
        Assert.IsFalse(progress.TryOpenNextStep(), "The new step has not spawned yet.");

        progress.MarkCurrentStepSpawned();
        Assert.IsFalse(progress.TryOpenNextStep(), "There are no more ratios.");
    }

    [TestMethod]
    public void ZeroRatioNeverOpensAStep()
    {
        var progress = WithMonsters(ClearType.Kill, 100, 1, 0);

        progress.MarkCurrentStepSpawned();
        progress.Removed();

        Assert.IsFalse(progress.TryOpenNextStep());
    }

    [TestMethod]
    public void StoppedStepWaits()
    {
        var progress = WithMonsters(ClearType.Kill, 100, 1, 50);

        progress.MarkCurrentStepSpawned();
        progress.Removed();
        progress.Stopped = true;
        Assert.IsFalse(progress.TryOpenNextStep());

        progress.Stopped = false;
        Assert.IsTrue(progress.TryOpenNextStep());
    }
}
