using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The monsters of a maze are as strong as their group says for the number of members the party has: one to four, a monster
/// of another kind than the plain one counting as a party of one.
/// </summary>
[TestClass]
public sealed class PartyRevisionTest
{
    private const uint Group = 5;
    private const int Plain = 100;
    private const int Boss = 101;
    private const int Ungrouped = 102;

    private static void Text(BinaryWriter writer) => writer.Write((ushort)0);

    /// <summary>
    /// A row of the monster table of the server in the layout of the file, with only what the strength needs.
    /// </summary>
    private static Action<BinaryWriter> Monster(uint id, byte type, uint group)
    {
        return writer =>
        {
            writer.Write(id);
            Text(writer);
            Text(writer);
            writer.Write(new byte[42]);
            writer.Write((byte)10);
            writer.Write(new byte[9]);
            writer.Write(type);
            writer.Write(new byte[3]);
            Text(writer);
            Text(writer);
            writer.Write(new byte[47]);
            Text(writer);
            writer.Write(new byte[2]);
            Text(writer);
            writer.Write(new byte[8]);
            Text(writer);
            Text(writer);
            Text(writer);
            writer.Write(new byte[3]);
            Text(writer);
            Text(writer);
            Text(writer);
            writer.Write(new byte[18]);
            Text(writer);
            Text(writer);
            Text(writer);
            writer.Write(new byte[62]);
            writer.Write(group);
            writer.Write(new byte[186]);
        };
    }

    private static Action<BinaryWriter> Rates(uint id, byte members, float health, float attack, float defense, float armor, uint recover)
    {
        return writer =>
        {
            writer.Write(id);
            writer.Write(Group);
            writer.Write(members);
            writer.Write(health);
            writer.Write(attack);
            writer.Write(defense);
            writer.Write(1f);
            writer.Write(1f);
            writer.Write(armor);
            writer.Write(999999u);
            writer.Write(recover);
        };
    }

    private static ReadOnlyCollection<T> Rows<T>(params Action<BinaryWriter>[] rows)
    {
        return new ReadOnlyCollection<T>(rows.Select(write =>
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            write(writer);
            writer.Flush();
            stream.Position = 0;

            return (T)Activator.CreateInstance(typeof(T), new BinaryReader(stream))!;
        }).ToArray());
    }

    private static PartyRevision Revision()
    {
        return new PartyRevision(
            Rows<MonsterReviseGroupRow>(Monster(Plain, 0, Group), Monster(Boss, 4, Group), Monster(Ungrouped, 0, 99)),
            Rows<PartyReviseRatesRow>(
                Rates(1, 1, 0.5f, 1f, 1f, 20f, 2000),
                Rates(2, 2, 1f, 1.5f, 2f, 20f, 2000),
                Rates(3, 3, 1.5f, 1.5f, 2f, 20f, 2000),
                Rates(4, 4, 2f, 2f, 3f, 30f, 4000)));
    }

    private static MonsterCombatComponent Base()
    {
        var damage = new DamageStats { AttackMin = 80.9f, AttackMax = 100.9f, Defense = 50.9f };

        return new MonsterCombatComponent
        {
            Stats = new CombatStats { Level = 10, MaxHealth = 1000.9f, Physical = damage, Magical = damage },
            Defense = DefenseType.SuperArmor
        };
    }

    [TestMethod]
    public void AMonsterIsAsStrongAsItsGroupMakesItForTheMembersOfTheParty()
    {
        var revision = Revision();
        var alone = revision.Apply(Base(), Plain, 1);
        var four = revision.Apply(Base(), Plain, 4);

        // The rates multiply the whole number of the stat, and a party of one is a row of its own.
        Assert.AreEqual(500f, alone.Stats.MaxHealth);
        Assert.AreEqual(2000f, four.Stats.MaxHealth);
        Assert.AreEqual(200f, four.Stats.Physical.AttackMax);
        Assert.AreEqual(160f, four.Stats.Magical.AttackMin);
        Assert.AreEqual(150f, four.Stats.Physical.Defense);

        // The super armor is a share of the life, and takes the time the row gives to come back.
        Assert.AreEqual(100f, alone.MaxSuperArmor);
        Assert.AreEqual(600f, four.MaxSuperArmor);
        Assert.AreEqual(four.MaxSuperArmor, four.SuperArmor);
        Assert.AreEqual(4000, four.SuperArmorRecovery);

        Assert.AreEqual(2000f, revision.Apply(Base(), Plain, 9).Stats.MaxHealth, "More than four members count as four");
        Assert.AreEqual(500f, revision.Apply(Base(), Plain, 0).Stats.MaxHealth, "Nobody counts as one");
    }

    [TestMethod]
    public void AMonsterOfAnotherKindOrWithoutARowKeepsToItsOwnTable()
    {
        var revision = Revision();

        Assert.AreEqual(500f, revision.Apply(Base(), Boss, 4).Stats.MaxHealth, "A monster that is not plain counts as a party of one");
        Assert.IsFalse(revision.IsRevised(Boss), "A monster that is not plain does not follow the party when it changes");

        Assert.AreEqual(Base(), revision.Apply(Base(), Ungrouped, 4), "A group with no row for the members changes nothing");
        Assert.AreEqual(Base(), revision.Apply(Base(), 7, 4), "A monster the table does not know changes nothing");
    }
}
