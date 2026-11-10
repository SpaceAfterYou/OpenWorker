using System.Numerics;
using System.Text;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ActionFileTest
{
    private const string RealFilesVariable = "OPENWORKER_TEST_ACTION";

    /// <summary>The first version of the file that has the aura of an attack.</summary>
    private const short AuraSinceVersion = 242;

    private static byte[] Chunk(string tag, Action<BinaryWriter> body)
    {
        using var content = new MemoryStream();
        using (var writer = new BinaryWriter(content, Encoding.Latin1, leaveOpen: true))
        {
            body(writer);
        }

        using var result = new MemoryStream();
        using var output = new BinaryWriter(result);
        var name = Encoding.Latin1.GetBytes(tag);

        Array.Reverse(name);
        output.Write(1);
        output.Write(name);
        output.Write((int)content.Length);
        output.Write(content.ToArray());
        output.Write(1);
        output.Write(name);
        output.Flush();

        return result.ToArray();
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        writer.Write(value.Length);
        writer.Write(Encoding.Latin1.GetBytes(value));
    }

    private static void WriteEvent(BinaryWriter writer, short version, short type, float start)
    {
        writer.Write(Chunk("EVNT", e =>
        {
            e.Write(type);
            WriteString(e, "trigger");
            e.Write(7);
            e.Write(start);
            e.Write(-1f);

            if (version >= 204)
            {
                e.Write(0);
                e.Write(0);
            }

            if (type == 3)
            {
                e.Write((short)1);
                e.Write((short)6);
                e.Write((short)0);
                e.Write(11);

                foreach (var value in (ReadOnlySpan<float>)[40f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f])
                {
                    e.Write(value);
                }

                WriteProjectile(e, version);
            }
        }));
    }

    private static void WriteFloats(BinaryWriter writer, params ReadOnlySpan<float> values)
    {
        foreach (var value in values)
        {
            writer.Write(value);
        }
    }

    private const int AuraWritten = 325;

    private static void WriteProjectile(BinaryWriter e, short version)
    {
        WriteFloats(e, 10f, 20f, 30f, 500f, 2f);
        e.Write(true);
        e.Write(1);
        e.Write(false);
        e.Write(75f);
        e.Write(true);
        WriteFloats(e, 0.2f, 0f);
        e.Write(true);
        WriteFloats(e, 0f, 0f);
        e.Write((short)0);
        e.Write(new byte[2]);
        e.Write(version >= 218);
        e.Write((short)0);
        e.Write(0f);
        e.Write(new byte[2]);
        e.Write((short)6);
        WriteFloats(e, -7f, 40f, 0f);

        foreach (var _ in Enumerable.Range(0, 5))
        {
            WriteString(e, "text");
        }

        e.Write(new byte[1 + 2 + 4 + 4 + 4 + 12 + 4 + 4 + 4]);
        e.Write(new byte[1 + 4 + 4 + 4 + 4 + 4]);
        e.Write(7);
        e.Write(1);
        e.Write(0.5f);
        e.Write(1406);
        e.Write(9);
        e.Write(new byte[12 + 4 + 20 + 2]);
        e.Write(1);
        WriteFloats(e, 3f, 5f, 0.3f, 360f, 0f, -10f, 0f, 100f, 100f, 120f, 200f, 1f);
        WriteString(e, "text");
        e.Write(new byte[4 + 1 + 4]);
        e.Write(new byte[1 + 12 + 8]);
        WriteString(e, "text");
        e.Write(new byte[12]);
        WriteString(e, "text");
        WriteString(e, "text");
        e.Write(new byte[24]);
        e.Write(new byte[40]);
        e.Write(true);
        e.Write(4);
        e.Write(new byte[2 + 2 + 1 + 4 + 4]);
        e.Write(false);
        e.Write(true);
        // What later versions added at the end of an attack: a string, a flag, two strings, a flag, two numbers, two flags, two numbers
        // and a string, a short, the aura (from 242, so the attacks of version 236 end earlier), a flag and two numbers.
        if (version >= 242)
        {
            e.Write(new byte[4 + 1 + 4 + 4 + 1 + 8 + 2 + 8 + 4 + 2]);
            e.Write(AuraWritten);
            e.Write(new byte[1 + 8 + 1]);
        }
        else
        {
            e.Write(new byte[32]);
        }
    }

    private static void WriteAnimation(BinaryWriter writer, short version, string name)
    {
        WriteString(writer, name);
        writer.Write(new byte[10]);
        writer.Write(0f);

        if (version >= 235)
        {
            writer.Write((short)0);
        }

        writer.Write(1.5f);
        writer.Write(new byte[4]);
        writer.Write(3);
        WriteEvent(writer, version, 1, 0.1f);
        WriteEvent(writer, version, 3, 0.25f);
        WriteEvent(writer, version, 3, 0.75f);
        writer.Write(1);
        writer.Write(Chunk("FRMS", e => e.Write(new byte[8])));
        writer.Write(0);

        if (version >= 241)
        {
            writer.Write(0);
        }
    }

    private static MemoryStream Build(short version)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream, Encoding.Latin1, leaveOpen: true);

        writer.Write(Encoding.Latin1.GetBytes("VBIN"));
        writer.Write(0);
        writer.Write(Chunk("TRIG", e =>
        {
            e.Write(version);
            e.Write(Chunk("HEAD", head => head.Write(2)));
            e.Write(Chunk("HTBN", bones => bones.Write(new byte[12])));
            e.Write(Chunk("SEQU", sequence =>
            {
                WriteAnimation(sequence, version, "First");
                WriteAnimation(sequence, version, "Second");
            }));
        }));
        writer.Flush();
        stream.Position = 0;

        return stream;
    }

    /// <summary>
    /// The attacks of every animation are read on both sides of the version that added the rotation frames.
    /// </summary>
    [TestMethod]
    public void ReadsAttacksOfEveryAnimation()
    {
        foreach (var version in (short[])[236, 245])
        {
            var file = ActionFile.Read(Build(version));

            Assert.HasCount(2, file.Animations);

            var animation = file.Animations["second"];

            Assert.AreEqual(1.5f, animation.Length);
            Assert.HasCount(2, animation.Attacks);
            var attack = animation.Attacks[0];

            Assert.AreEqual(
                new AttackTrigger(51, 1, 6, 11, 0.25f, 40f, new Vector3(1, 2, 3), 4, 5, 6, 7, 8, 0.5f, 9, attack.Projectile, attack.Chain, 1406, AttackTrigger.SelfAndParty, version >= AuraSinceVersion ? AuraWritten : 0),
                attack);
            Assert.AreEqual(new ChainInfo(1, 3f, 5, 0.3f, new Vector3(0, -10, 0), 120f, 1f, true), attack.Chain);
            Assert.AreEqual(0.75f, animation.Attacks[1].Start);
            // The client numbers the attacks itself: the first animation took 1, so the second goes on from the next block.
            Assert.AreEqual(52, animation.Attacks[1].Id);
            Assert.AreEqual(
                new ProjectileInfo(new Vector3(10, 20, 30), 500f, 2f, true, 1, 75f, true, 0.2f, true, version >= 218, 6, new Vector3(-7, 40, 0), true, 4),
                attack.Projectile);
        }
    }

    [TestMethod]
    public void RefusesOtherFiles()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => ActionFile.Read(new MemoryStream(new byte[32])));
    }

    [TestMethod]
    public void CutFileFails()
    {
        var whole = Build(236).ToArray();

        Assert.ThrowsExactly<EndOfStreamException>(() => ActionFile.Read(new MemoryStream(whole[..(whole.Length / 2)])));
    }

    [TestMethod]
    public void ReadsTheFilesOfTheClient()
    {
        var folder = Environment.GetEnvironmentVariable(RealFilesVariable);

        if (folder is null)
        {
            Assert.Inconclusive($"{RealFilesVariable} is not set.");
        }

        foreach (var letter in "ABCDEF")
        {
            var file = ActionFile.Load(Path.Join(folder, $"PC_{letter}.adf"));

            Assert.IsNotEmpty(file.Animations, letter.ToString());
            Assert.IsTrue(file.Animations.Values.Any(e => e.Attacks.Count > 0), letter.ToString());
        }

        var attack = ActionFile.Load(Path.Join(folder, "PC_A.adf")).Animations["B_N_Attack_01"].Attacks[0];

        Assert.AreEqual(0.25f, attack.Start);
        Assert.AreEqual(450f, attack.Radius);
        Assert.AreEqual(new Vector3(0, 250, 0), attack.Center);
        Assert.AreEqual(1, attack.Id, "the first attack of the file");

        var thrown = ActionFile.Load(Path.Join(folder, "PC_A.adf")).Animations["B_Skill_Flash_Step"].Attacks[0];

        Assert.IsTrue(thrown.IsProjectile);
        Assert.AreEqual(1701, thrown.Id, "numbered by the client, block after block");
        Assert.AreEqual(5000f, thrown.Projectile!.Speed);
        Assert.AreEqual(100f, thrown.Projectile.Radius);
        Assert.IsTrue(thrown.Projectile.Penetrates);

        var chain = ActionFile.Load(Path.Join(folder, "PC_E.adf")).Animations["B_Skill_07_Attack"].Attacks.Single(e => e.Id == 1201);

        Assert.IsTrue(chain.IsChain);
        Assert.AreEqual(new ChainInfo(ChainInfo.FromFirst, 3f, 5, 1f, new Vector3(0, -10, 0), 100f, 1f, true), chain.Chain);
        Assert.AreEqual(3500f, chain.Projectile!.Speed);
        Assert.IsTrue(chain.Projectile.IsContinuous);
    }
}
