using System.Collections.ObjectModel;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Hotspot.Modules.Persons.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class CombatStatsTest
{
    private const string TablesVariable = "OPENWORKER_TEST_RES";

    private const int FloatsBeforeResists = 35;
    private const int Resists = 12;
    private const int FloatsAfterResists = 8;

    private static StatusRow Status(params (int Column, float Value)[] values)
    {
        var map = values.ToDictionary(e => e.Column, e => e.Value);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((short)1);

        for (var column = 1; column <= FloatsBeforeResists; column++)
        {
            writer.Write(map.GetValueOrDefault(column));
        }

        writer.Write(new byte[Resists]);

        for (var column = 0; column < FloatsAfterResists; column++)
        {
            writer.Write(map.GetValueOrDefault(FloatsBeforeResists + Resists + 1 + column));
        }

        writer.Flush();
        stream.Position = 0;

        return new StatusRow(new BinaryReader(stream));
    }

    private const int StrengthFirst = 1;
    private const int StrengthGrowth = 2;
    private const int AgilityFirst = 3;
    private const int VitalityFirst = 9;
    private const int VitalityGrowth = 10;
    private const int HealthPerVitality = 13;
    private const int PhysicalAttackRate = 18;
    private const int HealthFirst = 51;

    [TestMethod]
    public void StatsGrowWithLevelAndRoundToWhole()
    {
        var status = Status((StrengthFirst, 2.5f), (StrengthGrowth, 1.25f));

        Assert.AreEqual(3f, CombatStatCalculator.Calculate(status, 1, 0).Strength);
        Assert.AreEqual(33f, CombatStatCalculator.Calculate(status, 25, 0).Strength);
    }

    [TestMethod]
    public void HealthComesFromVitalityAndTheFirstValue()
    {
        var status = Status((VitalityFirst, 2.5f), (VitalityGrowth, 8f), (HealthPerVitality, 50f), (HealthFirst, 1000f));

        Assert.AreEqual(1150f, CombatStatCalculator.Calculate(status, 1, 0).MaxHealth);
    }

    [TestMethod]
    public void FlatBonusAddsToHealth()
    {
        var status = Status((HealthFirst, 1f));
        var stats = CombatStatCalculator.Calculate(status, 1, 0, new StatBonuses { Health = -100f });

        Assert.AreEqual(-99f, stats.MaxHealth);
    }

    [TestMethod]
    public void RangedClassesAttackWithAgility()
    {
        var status = Status((StrengthFirst, 1f), (AgilityFirst, 2f), (PhysicalAttackRate, 2f));

        Assert.AreEqual(20f, CombatStatCalculator.Calculate(status, 1, 0).Physical.AttackMax);
        Assert.AreEqual(40f, CombatStatCalculator.Calculate(status, 1, 2).Physical.AttackMax);
        Assert.AreEqual(16f, CombatStatCalculator.Calculate(status, 1, 0).Physical.AttackMin, 0.001f);
    }

    [TestMethod]
    public void ZeroStatsAreNotSentToTheClient()
    {
        var stats = CombatStatCalculator.Calculate(Status((HealthFirst, 10f)), 1, 0).ToStatList();

        Assert.HasCount(1, stats);
        Assert.AreEqual(STAT_TYPE.STAT_HP_MAX, stats[0].byIndex);
        Assert.AreEqual(10f, stats[0].statValue);
    }

    private static ReadOnlyCollection<T> Load<T>(string folder, string table)
    {
        using var reader = new BinaryReader(File.OpenRead(Path.Join(folder, $"{table}.res")));

        var rows = new T[reader.ReadInt32()];

        for (var index = 0; index < rows.Length; index++)
        {
            rows[index] = (T)Activator.CreateInstance(typeof(T), reader)!;
        }

        return new ReadOnlyCollection<T>(rows);
    }

    [TestMethod]
    public void TablesOfTheClientGiveWhatTheyGiveByHand()
    {
        var folder = Environment.GetEnvironmentVariable(TablesVariable);

        if (folder is null)
        {
            Assert.Inconclusive($"{TablesVariable} is not set.");
        }

        var tables = new CombatTables(
            Load<MonsterRow>(folder, "tb_Monster"),
            Load<StatusRow>(folder, "tb_Status"),
            Load<SkillRow>(folder, "tb_Skill"),
            Load<CharacterInfoRow>(folder, "tb_Character_Info"));

        var monster = tables.GetMonster(30103001);

        Assert.AreEqual(8, monster.Stats.Level);
        Assert.AreEqual(3601f, monster.Stats.MaxHealth);
        Assert.AreEqual(40f, monster.Stats.Physical.Defense);
        Assert.AreEqual(40f, monster.BodyRadius);
        Assert.AreEqual(130f, monster.BodyHeight);
        Assert.IsTrue(monster.HoldsSector);
        Assert.IsFalse(monster.CountsHits);

        var box = tables.GetMonster(31303511);

        Assert.AreEqual(5f, box.Stats.MaxHealth);
        Assert.IsTrue(box.CountsHits);
        Assert.IsFalse(box.HoldsSector);

        var hero = tables.GetPerson(Hero.Haru, 1);

        Assert.AreEqual(1150f, hero.MaxHealth);
        Assert.AreEqual(50f, hero.GrazeDamage);
        Assert.AreEqual(54f, hero.Physical.AttackMax, 0.01f);

        var skill = tables.GetSkill(12011011);

        Assert.AreEqual(73f, skill.DamageRate);
        Assert.AreEqual(1, skill.Attribute);
    }
}
