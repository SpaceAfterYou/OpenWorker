using OpenWorker.Gameplay.Modules.DailyMissions;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MazeReporterTest
{
    private static uint Bit(DailyMissionCondition condition) => 1u << (int)condition;

    [TestMethod]
    public void PlainAttackKillsAsAPlainAttack()
    {
        Assert.AreEqual(Bit(DailyMissionCondition.NormalAttack), MazeReporter.GetKillConditions(1, false));
    }

    [TestMethod]
    public void SkillsProperKillAsSkills()
    {
        Assert.AreEqual(Bit(DailyMissionCondition.SkillAttack), MazeReporter.GetKillConditions(0, false));
        Assert.AreEqual(Bit(DailyMissionCondition.SkillAttack), MazeReporter.GetKillConditions(18, false));
    }

    [TestMethod]
    public void AttackFromTheGroundAndThrownWeaponHaveTheirOwnKinds()
    {
        Assert.AreEqual(Bit(DailyMissionCondition.RiseAttack), MazeReporter.GetKillConditions(7, false));
        Assert.AreEqual(Bit(DailyMissionCondition.SubweaponAttack), MazeReporter.GetKillConditions(3, false));
    }

    [TestMethod]
    public void OtherKindsOfSkillCountForNothingButTheCritical()
    {
        Assert.AreEqual(0u, MazeReporter.GetKillConditions(16, false));
        Assert.AreEqual(Bit(DailyMissionCondition.Critical), MazeReporter.GetKillConditions(16, true));
    }

    [TestMethod]
    public void CriticalKillIsAlsoTheKindOfItsAttack()
    {
        var expected = Bit(DailyMissionCondition.SkillAttack) | Bit(DailyMissionCondition.Critical);

        Assert.AreEqual(expected, MazeReporter.GetKillConditions(0, true));
    }
}
