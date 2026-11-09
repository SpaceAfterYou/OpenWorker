using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Test;

[TestClass]
public sealed class MessageOpcodeTest
{
    // Group and command bytes that differ, so that a conversion reading the wrong byte is noticed.
    private const byte Group = (byte)GroupOpcode.Character;
    private const byte Command = 0x22;

    [TestMethod]
    public void ATypedCommandGivesItsByte()
    {
        var opcode = MessageOpcode.From(GroupOpcode.Character, (CharacterOpcode)Command);

        Assert.AreEqual(Group, opcode.Group);
        Assert.AreEqual(Command, opcode.Command);
        Assert.AreEqual(new MessageOpcode(Group, Command), opcode);
    }

    [TestMethod]
    public void TheValueHasTheGroupFirst()
    {
        Assert.AreEqual((short)(Group | Command << 8), new MessageOpcode(Group, Command).Value);
    }

    [TestMethod]
    public void ReadsTheGroupBeforeTheCommand()
    {
        using var reader = new BinaryReader(new MemoryStream([Group, Command]));

        var opcode = new MessageOpcode(reader);

        Assert.AreEqual(Group, opcode.Group);
        Assert.AreEqual(Command, opcode.Command);
    }

    /// <summary>
    /// The name of a command is looked up in the enum named after its group, so a group whose enum is named otherwise would
    /// lose the names of its commands in the logs and the metrics.
    /// </summary>
    [TestMethod]
    public void EveryGroupNamesItsCommandsFromItsEnum()
    {
        var unnamed = Enum.GetValues<GroupOpcode>()
            .Where(group => new MessageOpcode(group, 0).ToCommandString().StartsWith("0x", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(unnamed, "Groups with no enum of commands: " + string.Join(", ", unnamed));
        Assert.AreEqual("System:Ping", MessageOpcode.From(GroupOpcode.System, SystemOpcode.Ping).ToFullString());
    }

    [TestMethod]
    public void NamesAnOpcodeOfAGroupNobodyKnowsWithoutThrowing()
    {
        Assert.AreEqual("240:0x01", new MessageOpcode(0xF0, 0x01).ToFullString());
    }
}
