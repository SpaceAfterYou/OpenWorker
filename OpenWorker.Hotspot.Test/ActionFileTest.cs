using System.Numerics;
using System.Text;
using OpenWorker.Batch.Actions;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class ActionFileTest
{
    private const string RealFilesVariable = "OPENWORKER_TEST_ACTION";

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

                // The rest of an attack is skipped.
                e.Write(new byte[64]);
            }
        }));
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

    [TestMethod]
    [DataRow((short)236)]
    [DataRow((short)245)]
    public void ReadsAttacksOfEveryAnimation(short version)
    {
        var file = ActionFile.Read(Build(version));

        Assert.HasCount(2, file.Animations);

        var animation = file.Animations["second"];

        Assert.AreEqual(1.5f, animation.Length);
        Assert.HasCount(2, animation.Attacks);
        Assert.AreEqual(
            new AttackTrigger(1, 6, 11, 0.25f, 40f, new Vector3(1, 2, 3), 4, 5, 6, 7, 8),
            animation.Attacks[0]);
        Assert.AreEqual(0.75f, animation.Attacks[1].Start);
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
    }
}
