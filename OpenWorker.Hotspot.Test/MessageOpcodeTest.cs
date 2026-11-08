using System.Globalization;
using System.Reflection;
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
    public void ConvertsToTheGroupFromTheGroupByte()
    {
        GroupOpcode group = new MessageOpcode(Group, Command);

        Assert.AreEqual(GroupOpcode.Character, group);
    }

    [TestMethod]
    public void ConvertsToACommandFromTheCommandByte()
    {
        SystemOpcode system = new MessageOpcode((byte)GroupOpcode.System, (byte)SystemOpcode.Ping);
        CharacterOpcode character = new MessageOpcode(Group, Command);

        Assert.AreEqual(SystemOpcode.Ping, system);
        Assert.AreEqual((CharacterOpcode)Command, character);
    }

    [TestMethod]
    public void EveryCommandEnumReadsTheCommandByte()
    {
        var opcode = new MessageOpcode(Group, Command);
        var converted = 0;

        foreach (var method in typeof(MessageOpcode).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (method.Name != "op_Implicit" || method.ReturnType == typeof(short) || method.ReturnType == typeof(GroupOpcode))
            {
                continue;
            }

            var result = method.Invoke(null, [opcode]);

            Assert.AreEqual(Command, Convert.ToByte(result, CultureInfo.InvariantCulture), $"{method.ReturnType.Name} must read the command.");

            converted++;
        }

        // One conversion for each command enum; a new one that is not in the file would go unchecked.
        var commandEnums = typeof(GroupOpcode).Assembly.GetTypes()
            .Count(e => e is { IsEnum: true, Namespace: not null } && e.Namespace == typeof(GroupOpcode).Namespace && e != typeof(GroupOpcode));

        Assert.AreEqual(commandEnums, converted);
    }

    [TestMethod]
    public void ConvertsToTheValueWithTheGroupFirst()
    {
        short value = new MessageOpcode(Group, Command);

        Assert.AreEqual((short)(Group | Command << 8), value);
    }

    [TestMethod]
    public void ReadsTheGroupBeforeTheCommand()
    {
        using var reader = new BinaryReader(new MemoryStream([Group, Command]));

        var opcode = new MessageOpcode(reader);

        Assert.AreEqual(Group, opcode.Group);
        Assert.AreEqual(Command, opcode.Command);
    }
}
