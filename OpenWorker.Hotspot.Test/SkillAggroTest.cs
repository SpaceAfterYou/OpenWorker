using System.Collections.ObjectModel;
using OpenWorker.Gameplay.Ai;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class SkillAggroTest
{
    private const int Damage = 1000;

    /// <summary>
    /// A row of the server's skill table in the layout the file has it: only the skill, the kind of its hate and the value are
    /// filled.
    /// </summary>
    private static SkillAggroRow Row(uint skill, byte kind, ushort value)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(skill);
        writer.Write((ushort)0);
        writer.Write(new byte[139]);
        writer.Write(kind);
        writer.Write(value);
        writer.Write(new byte[2]);

        for (var animation = 0; animation < 4; animation++)
        {
            writer.Write((ushort)0);
        }

        writer.Write(new byte[14]);
        writer.Flush();

        stream.Position = 0;

        return new SkillAggroRow(new BinaryReader(stream));
    }

    [TestMethod]
    public void ASkillMadeToDrawHateEarnsMoreThanOneThatIsNot()
    {
        var aggro = new SkillAggro(new ReadOnlyCollection<SkillAggroRow>([Row(1, 0, 0), Row(2, 0, 1000), Row(3, 2, 1000)]));

        var plain = aggro.Hate(1, Damage, 0);
        var flat = aggro.Hate(2, Damage, 0);
        var percent = aggro.Hate(3, Damage, 0);

        Assert.AreEqual(Damage * 2 * 0.0002f * 2.5f, plain, 0.0001f);
        Assert.IsGreaterThan(plain, flat);
        Assert.AreEqual(plain * 6f, percent, plain * 0.01f, "A thousand percent more than the damage makes six times the hate here.");
        Assert.AreEqual(plain, aggro.Hate(9, Damage, 0), 0.0001f, "A skill the table does not know earns the hate of the damage.");
    }

    [TestMethod]
    public void TheLowestLevelOfTheGroupEarnsTheMostAndNobodyBeyondTheFourthEarnsAny()
    {
        var aggro = SkillAggro.None;

        Assert.IsGreaterThan(aggro.Hate(1, Damage, 1), aggro.Hate(1, Damage, 0));
        Assert.IsGreaterThan(aggro.Hate(1, Damage, 3), aggro.Hate(1, Damage, 2));
        Assert.AreEqual(0f, aggro.Hate(1, Damage, 4));
    }
}
