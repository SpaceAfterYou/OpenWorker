using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.Domain.Enums;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SkillActionProviderTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";
    private const string ActionsVariable = "OPENWORKER_TEST_ACTION";

    private const int ChainSkill = 52000711;
    private const int ChainAttack = 1201;
    private const int MouseTrapSkill = 24000111;
    private const int SwordSkill = 12011011;
    private const int ControlSkill = 34000111;
    private const int QuickBite = 1301020244;
    private const int Whirl = 1301050183;

    private static SkillActionProvider Open()
    {
        var tables = Environment.GetEnvironmentVariable(TablesVariable);
        var actions = Environment.GetEnvironmentVariable(ActionsVariable);

        // One reason per variable, as CI reads the reasons of a skip.
        if (string.IsNullOrEmpty(tables))
        {
            Assert.Inconclusive($"{TablesVariable} is not set.");
        }

        if (string.IsNullOrEmpty(actions))
        {
            Assert.Inconclusive($"{ActionsVariable} is not set.");
        }

        using var reader = new BinaryReader(File.OpenRead(Path.Join(tables, "tb_Skill.res")));

        var rows = new SkillRow[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = new SkillRow(reader);
        }

        return new SkillActionProvider(new ReadOnlyCollection<SkillRow>(rows), NullLogger<SkillActionProvider>.Instance) { Folder = actions };
    }

    [TestMethod]
    public void ChainOfTheSkillIsFoundByItsAttack()
    {
        var provider = Open();

        Assert.IsTrue(provider.TryGetChain(ChainSkill, Hero.Stella, ChainAttack, out var chain));
        Assert.IsTrue(chain.IsChain);
        Assert.IsNotNull(chain.Chain);
        Assert.IsNotNull(chain.Projectile);
        Assert.AreEqual(ChainAttack, chain.Id);

        Assert.IsFalse(provider.TryGetChain(ChainSkill, Hero.Stella, 99, out _), "an attack the skill does not have");
        Assert.IsFalse(provider.TryGetChain(ChainSkill, Hero.Haru, ChainAttack, out _), "the file of another hero");
        Assert.IsFalse(provider.TryGetChain(SwordSkill, Hero.Haru, 1, out _), "a melee attack is no chain");
        Assert.IsFalse(provider.TryGetProjectile(ChainSkill, Hero.Stella, ChainAttack, out _), "a chain is no projectile");
    }

    /// <summary>
    /// A skill that controls a monster brings it in at the moment of its start and lets it go at the moment of its end, after a
    /// loop of the table's time that the file has no animation for.
    /// </summary>
    [TestMethod]
    public void ControlLastsThroughTheLoopTime()
    {
        var controls = Open().GetControls(ControlSkill, Hero.Lily);

        Assert.HasCount(2, controls);
        Assert.AreNotEqual(0, controls[0].Event.Monster);
        Assert.IsTrue(controls[0].Event.Follows);
        Assert.AreEqual(0, controls[1].Event.Monster);
        Assert.IsGreaterThan(controls[0].Time + 7f, controls[1].Time);
    }

    [TestMethod]
    public void ChainDoesNotHitByTime()
    {
        var provider = Open();

        Assert.IsFalse(provider.GetHits(ChainSkill, Hero.Stella).Any(e => e.Trigger.IsChain));
    }

    [TestMethod]
    public void OnlyTheViperTrapFollowsTheMouse()
    {
        var provider = Open();

        Assert.IsTrue(provider.FollowsMouse(MouseTrapSkill));
        Assert.IsFalse(provider.FollowsMouse(SwordSkill));
        Assert.IsFalse(provider.FollowsMouse(-1));
    }

    [TestMethod]
    public void RangeComesFromTheTable()
    {
        var provider = Open();

        Assert.AreEqual(350f, provider.GetMaxRange(SwordSkill));
        Assert.AreEqual(0f, provider.GetMaxRange(-1));
    }

    [TestMethod]
    public void SkillPlaysTheStepsTheTableNames()
    {
        var provider = Open();
        var viper = provider.GetAnimations(MouseTrapSkill, Hero.Erwin)!;

        Assert.IsNotNull(viper.Start);
        Assert.IsNotNull(viper.Loop);
        Assert.IsNotNull(viper.Extra);
        Assert.IsNotNull(viper.End);
        Assert.AreEqual(8f, viper.LoopTime, "the table gives milliseconds");
        Assert.IsFalse(viper.Start.Repeats, "a start plays once");
        Assert.IsTrue(viper.Extra.Repeats, "a wait goes on until something ends it");

        var sword = provider.GetAnimations(SwordSkill, Hero.Haru)!;

        Assert.IsNull(sword.Start);
        Assert.IsNull(sword.Loop);
        Assert.IsNotNull(sword.End);
        Assert.IsNull(provider.GetAnimations(-1, Hero.Haru));
    }

    /// <summary>
    /// A monster plays its skill as the clients do: a stretch of an animation at the speed its file gives it, and a loop that starts
    /// over by itself for the time of the table, its blows judged again each time round.
    /// </summary>
    [TestMethod]
    public void AMonsterSkillPlaysAtTheSpeedOfItsAnimationsAndLoopsForTheTableTime()
    {
        var provider = Open();

        // The first 0.82 s of the bite play 2.2 times faster: its blow at 0.994 lands 0.45 s sooner.
        var bite = provider.GetTimeline(QuickBite, "MOB_0251_CanineTooth_Veryhard");

        Assert.AreEqual((0.82f / 2.2f) + (0.994f - 0.82f), bite.Hits[0].Time, 0.01f);
        Assert.AreEqual((0.82f / 2.2f) + (2.167f - 0.82f), bite.Clips[0].Length, 0.01f);

        // A loop of 0.533 s for 1.59 s goes round three times, its 14 blows each time, and the end follows it.
        var whirl = provider.GetTimeline(Whirl, "MOB_9052_MumblePuppet");
        var loopBlows = whirl.Hits.Where(e => e.Trigger.Id is >= 151 and <= 164).ToArray();
        var loopStart = whirl.Clips[1].Began;

        Assert.HasCount(42, loopBlows);
        Assert.IsTrue(loopBlows.All(e => e.Time >= loopStart && e.Time < loopStart + 1.59f));
        Assert.AreEqual(loopStart + 1.59f, whirl.Clips[^1].Began, 0.001f);
    }
}
