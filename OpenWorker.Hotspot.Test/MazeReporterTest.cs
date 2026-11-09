using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MazeReporterTest
{
    private static uint Bit(DailyMissionCondition condition) => 1u << (int)condition;

    /// <summary>
    /// A kill counts for the daily mission of the kind of attack that made it: a plain attack, a skill proper (with a
    /// choice or none named), an attack from the ground, a thrown weapon; any other kind of skill counts for nothing.
    /// </summary>
    [TestMethod]
    public void AKillCountsForTheKindOfItsAttack()
    {
        (byte Kind, uint Conditions)[] kinds =
        [
            (1, Bit(DailyMissionCondition.NormalAttack)),
            (0, Bit(DailyMissionCondition.SkillAttack)),
            (18, Bit(DailyMissionCondition.SkillAttack)),
            (7, Bit(DailyMissionCondition.RiseAttack)),
            (3, Bit(DailyMissionCondition.SubweaponAttack)),
            (16, 0u)
        ];

        foreach (var (kind, conditions) in kinds)
        {
            Assert.AreEqual(conditions, MazeReporter.GetKillConditions(kind, false), $"kind {kind}");
        }
    }

    [TestMethod]
    public void OtherKindsOfSkillCountForNothingButTheCritical()
    {
        Assert.AreEqual(Bit(DailyMissionCondition.Critical), MazeReporter.GetKillConditions(16, true));
    }

    [TestMethod]
    public void CriticalKillIsAlsoTheKindOfItsAttack()
    {
        var expected = Bit(DailyMissionCondition.SkillAttack) | Bit(DailyMissionCondition.Critical);

        Assert.AreEqual(expected, MazeReporter.GetKillConditions(0, true));
    }
}
