using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Batch.Actions;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The parts of a monster: the hit bones a blow lands on, the life of the parts, and what a broken part turns the monster into.
/// </summary>
[TestClass]
public sealed class MonsterPartsTest
{
    private const string MachinaFile = "MOB_9037_DeusExMachina_All";

    private static readonly AttackTrigger Circle = new(0, 0, 0, 0, 0f, 360f, Vector3.Zero, 0, 0, 450, 200, 0);

    private static string Folder(string variable)
    {
        var folder = Environment.GetEnvironmentVariable(variable);

        if (string.IsNullOrEmpty(folder))
        {
            Assert.Inconclusive($"{variable} is not set.");
        }

        return folder;
    }

    [TestMethod]
    public void ABlowLandsOnThePartOfTheNearestBoneUnlessTheCylinderIsNearer()
    {
        var area = new AttackArea(Circle, Vector3.Zero, 0f);
        BonePlace[] bones = [new(new Vector3(0, 460, 50), 30, 2), new(new Vector3(0, 300, 50), 30, 1)];

        Assert.IsTrue(area.Strikes(new Vector3(0, 2000, 0), 50, 200, bones, out var part), "the bones are hit where the cylinder is not");
        Assert.AreEqual((byte)1, part);

        Assert.IsTrue(area.Strikes(new Vector3(0, 100, 0), 50, 200, bones, out part));
        Assert.AreEqual((byte)0, part, "the cylinder is nearer than any bone");

        Assert.IsFalse(area.Strikes(new Vector3(0, 0, 1000), 50, 200, [], out _), "a body above the area without bones is not hit");
        Assert.IsTrue(area.Strikes(new Vector3(0, 0, 1000), 50, 200, [new(new Vector3(0, 100, 100), 30, 2)], out part), "its bones still are");
        Assert.AreEqual((byte)2, part);
    }

    [TestMethod]
    public void TheHitBonesOfAMonsterComeFromTheFileBesideItsActionFileAndItsAnimations()
    {
        var actions = new SkillActionProvider(TestTables.Load<SkillRow>("tb_Skill"), NullLogger<SkillActionProvider>.Instance) { Folder = Folder("OPENWORKER_TEST_ACTION") };

        var body = actions.GetHitBody(MachinaFile);

        Assert.IsNotNull(body);
        CollectionAssert.AreEqual(new byte[] { 1, 1, 2, 2, 2, 1 }, body.Bones.Select(e => e.Part).ToArray());
        Assert.IsTrue(body.HasCylinder);

        var stand = actions.GetAnimation(MachinaFile, "N_Stand")?.Bones;

        Assert.IsNotNull(stand);
        Assert.AreEqual(body.Bones.Count, stand.Bones);
        Assert.IsGreaterThan(0, stand.Count);
        Assert.IsGreaterThan(300f, stand.At(0, 0f).X, "the left hand is on the left of the model");
        Assert.IsLessThan(-300f, stand.At(2, 0f).X, "the right hand on the right");
    }
}
