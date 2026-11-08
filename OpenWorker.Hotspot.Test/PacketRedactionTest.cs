using System.Text;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Modules.Login.Requests;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.SoulWorker.Network;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class PacketRedactionTest
{
    private static byte[] Message(GroupOpcode group, byte command, Action<BinaryWriter> body)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)group);
        writer.Write(command);

        body(writer);

        writer.Flush();

        return stream.ToArray();
    }

    // The client counts the UTF-16 text and one byte of the terminator.
    private static void WriteUnicode(BinaryWriter writer, string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);

        writer.Write((short)(bytes.Length + 1));
        writer.Write(bytes);
        writer.Write((byte)0);
    }

    private static void WriteAscii(BinaryWriter writer, string value)
    {
        writer.Write((short)value.Length);
        writer.Write(Encoding.ASCII.GetBytes(value));
    }

    private static void WriteFixed(BinaryWriter writer, string value, int size)
    {
        var bytes = new byte[size];

        Encoding.ASCII.GetBytes(value).CopyTo(bytes, 0);

        writer.Write(bytes);
    }

    private static BinaryReader Body(byte[] content)
    {
        var reader = new BinaryReader(new MemoryStream(content));

        reader.ReadBytes(2);

        return reader;
    }

    [TestMethod]
    public void LoginKeepsTheUsernameAndHidesThePassword()
    {
        var content = Message(GroupOpcode.Login, (byte)LoginOpcode.Req, writer =>
        {
            WriteUnicode(writer, "player");
            WriteUnicode(writer, "secret1");
            WriteUnicode(writer, "00-11-22-33-44-55");
        });

        PacketRedaction.Redact(content);

        var request = new LoginAuthRequest(Body(content));

        Assert.AreEqual("player", request.Username);
        Assert.AreEqual("*******", request.Password);
        Assert.AreEqual("00-11-22-33-44-55", request.MacAddress);
    }

    [TestMethod]
    public void NextHumanNetworkLoginHidesTheTicketOnly()
    {
        var content = Message(GroupOpcode.Login, (byte)LoginOpcode.FromNextHumanNetwork, writer =>
        {
            WriteFixed(writer, "ticket", 2048);
            WriteFixed(writer, "info", 4096);
            WriteFixed(writer, "user", 21);
            WriteFixed(writer, "00-11-22-33-44-55", 18);
        });

        PacketRedaction.Redact(content);

        var request = new LoginNextHumanNetworkRequest(Body(content));

        Assert.AreEqual("******", request.PurpleTicket);
        Assert.AreEqual("info", request.PurpleGameInfo);
        Assert.AreEqual("user", request.PurpleUserId);
    }

    [TestMethod]
    [DataRow((byte)CharacterOpcode.SecondPassword)]
    [DataRow((byte)CharacterOpcode.TradePassword)]
    public void CharacterPasswordsAreHidden(byte command)
    {
        var content = Message(GroupOpcode.Character, command, writer =>
        {
            writer.Write((byte)1);
            WriteAscii(writer, "4040");
        });

        PacketRedaction.Redact(content);

        Assert.AreEqual("****", new PersonSecondPasswordRequest(Body(content)).Password);
    }

    [TestMethod]
    public void BrokenLengthsDoNotThrow()
    {
        byte[] content = [(byte)GroupOpcode.Login, (byte)LoginOpcode.Req, 0xFF, 0x7F, 0x01];

        PacketRedaction.Redact(content);
        PacketRedaction.Redact([(byte)GroupOpcode.Login]);
        PacketRedaction.Redact([(byte)GroupOpcode.Character, (byte)CharacterOpcode.SecondPassword, 1, 0xF0, 0xFF]);

        CollectionAssert.AreEqual(new byte[] { (byte)GroupOpcode.Login, (byte)LoginOpcode.Req, 0xFF, 0x7F, 0x01 }, content);
    }

    [TestMethod]
    public void OtherMessagesStayAsTheyAre()
    {
        var content = Message(GroupOpcode.Move, (byte)MoveOpcode.MoveBt, writer => WriteAscii(writer, "4040"));
        var copy = content.ToArray();

        PacketRedaction.Redact(content);

        CollectionAssert.AreEqual(copy, content);
    }
}
