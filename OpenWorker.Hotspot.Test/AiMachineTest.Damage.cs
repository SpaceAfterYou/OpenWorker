using System.Numerics;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Hotspot.Test;

public sealed partial class AiMachineTest
{
    private static Host Crowd()
    {
        var host = new Host { Player = Player(new Vector3(100, 0, 0)) };

        host.Others.Add(Player(new Vector3(100, 0, 0)) with { Id = 2000 });

        return host;
    }

    private static void Hit(AiMachine machine, Host host, int attacker, float hate, float pause = 0.6f)
    {
        machine.OnDamaged(attacker, hate);

        Run(machine, host, pause);
    }

    [TestMethod]
    public void TheOneWhoDidMostSinceTheLastLookIsTheTargetAndTheHateIsSpentAfterIt()
    {
        var host = Crowd();
        var builder = Fighter();

        builder.SetFindEnemyInfo(false, 0.1f, 1000f);

        var machine = Create(builder, host);

        machine.Start();

        Hit(machine, host, 1000, 1f);

        Assert.AreEqual(1000, machine.TargetId);

        machine.OnDamaged(1000, 1f);
        Hit(machine, host, 2000, 3f);

        Assert.AreEqual(2000, machine.TargetId);
        Assert.IsTrue(machine.AggroList.All(e => e.Value == 0f), "The look spends the hate.");

        Hit(machine, host, 1000, 1f);

        Assert.AreEqual(1000, machine.TargetId);
    }

    [TestMethod]
    public void ATauntHoldsTheMonsterOnItsGiverUntilItIsReleased()
    {
        var host = Crowd();
        var builder = Fighter();

        builder.SetFindEnemyInfo(false, 0.1f, 1000f);

        var machine = Create(builder, host);

        machine.Start();
        machine.Taunt(2000);

        Assert.AreEqual(2000, machine.TargetId);

        Hit(machine, host, 1000, 50f);

        Assert.AreEqual(2000, machine.TargetId, "Hate does not move a monster that is taunted.");

        machine.ReleaseTaunt();

        Hit(machine, host, 1000, 50f);

        Assert.AreEqual(1000, machine.TargetId);
    }

    private static AiProfileBuilder Escaper(float points)
    {
        var builder = Fighter();

        builder.SetEscapeInfo(points, 10000, 0f, 1000f, 5f, AiEscape.SkillKind, 77);

        return builder;
    }

    [TestMethod]
    public void BlowsAddUpToAnEscapeThatPlaysTheSkillOfTheScript()
    {
        var host = Crowd();
        var machine = Create(Escaper(3f), host);

        host.Known[77] = Skill(77);
        machine.Start();

        for (var blow = 0; blow < 3; blow++)
        {
            Hit(machine, host, 1000, 1f);
        }

        CollectionAssert.AreEqual(new[] { 77 }, host.Used.ToArray());
        Assert.HasCount(1, host.Escapes);
    }

    [TestMethod]
    public void BlowsInQuickSuccessionAddAFifthEachFromTheSecondOne()
    {
        var host = Crowd();
        var machine = Create(Escaper(10f), host);

        host.Known[77] = Skill(77);
        machine.Start();

        for (var blow = 0; blow < 10; blow++)
        {
            Hit(machine, host, 1000, 1f, 0.1f);
        }

        Assert.IsEmpty(host.Used, "Ten quick blows are worth less than ten.");
    }

    [TestMethod]
    public void ASkillEscapeIsNotPlayedWhileTheMonsterIsDownOrFlying()
    {
        var host = Crowd();
        var machine = Create(Escaper(3f), host);

        host.Known[77] = Skill(77);
        host.Down = true;
        machine.Start();

        for (var blow = 0; blow < 5; blow++)
        {
            Hit(machine, host, 1000, 1f);
        }

        Assert.IsEmpty(host.Used);
        Assert.IsEmpty(host.Escapes);
    }

    [TestMethod]
    public void AnEscapeOfTheOtherKindsGivesADefenseForTheTimeOfTheScript()
    {
        var host = Crowd();
        var builder = Fighter();

        builder.SetEscapeInfo(2f, 10000, 0f, 1000f, 5f, AiEscape.InvincibleKind, 1500);

        var machine = Create(builder, host);

        machine.Start();

        for (var blow = 0; blow < 3; blow++)
        {
            Hit(machine, host, 1000, 1f);
        }

        Assert.HasCount(1, host.Forced);
        Assert.AreEqual(AiDefense.Invincible, host.Forced[0].Defense);
        Assert.AreEqual(1.5f, host.Forced[0].Seconds, 0.001f);
        Assert.HasCount(1, host.Escapes);
    }

    [TestMethod]
    public void AMonsterCallsItsGroupOnceWhenItHasAnEnemyAndIsHurt()
    {
        var host = Crowd();
        var machine = Create(Fighter(), host);

        machine.Start();
        machine.CallsGroup = true;

        Hit(machine, host, 1000, 1f);
        Hit(machine, host, 1000, 1f);

        CollectionAssert.AreEqual(new[] { 1000 }, host.Alerted.ToArray());
    }

    [TestMethod]
    public void AMonsterCalledByItsGroupTurnsOnTheEnemyOfTheOtherOnlyWhenItHasNone()
    {
        var host = Crowd();
        var builder = Fighter();

        builder.SetFindEnemyInfo(false, 0.1f, 1000f);

        var machine = Create(builder, host);

        machine.Start();
        machine.Alert(2000);

        Assert.AreEqual(2000, machine.TargetId);

        machine.Alert(1000);

        Assert.AreEqual(2000, machine.TargetId);
    }

    [TestMethod]
    public void TheSkillOfTheScriptIsPlayedWhenTheSuperArmorIsWholeAgain()
    {
        var host = Crowd();
        var builder = Fighter();

        builder.SetSuperArmorSkill(88);
        host.Known[88] = Skill(88);

        var machine = Create(builder, host);

        machine.Start();
        machine.OnSuperArmorRestored();

        CollectionAssert.AreEqual(new[] { 88 }, host.Used.ToArray());
    }

    [TestMethod]
    public void AMonsterWithoutAnOwnerThatIsMadeToTraceWaits()
    {
        var host = Crowd();
        var machine = Create(Fighter(), host);

        machine.Start();
        machine.Force(AiState.TraceMove);

        Assert.AreEqual(AiState.Wait, machine.State);
    }
}
