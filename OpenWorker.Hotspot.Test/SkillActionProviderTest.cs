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
    private const int ChainAttack = 105;
    private const int MouseTrapSkill = 24000111;
    private const int SwordSkill = 12011011;

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
        Assert.IsFalse(provider.TryGetChain(SwordSkill, Hero.Haru, 3, out _), "a melee attack is no chain");
        Assert.IsFalse(provider.TryGetProjectile(ChainSkill, Hero.Stella, ChainAttack, out _), "a chain is no projectile");
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
}
