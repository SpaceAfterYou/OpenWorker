// This file is maintained by hand: nothing in the repository generates it.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.GameServer.SoulWorker.Network.DataTypes;

/// <summary>
/// The two bytes that open every message: the group, then the command inside the group. The commands of a group are the
/// enum named after it (<see cref="GroupOpcode.System" />, <see cref="SystemOpcode" />); a message writes its typed command
/// as a byte (<c>new(Group, (byte)Command)</c>), and code that holds one in a variable uses <see cref="From{TCommand}" />.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct MessageOpcode
{
    /// <summary>
    /// The command enum of each group, by the byte of the group: the enum named after the group.
    /// </summary>
    private static readonly Type?[] Commands = CommandsOfTheGroups();

    [field: FieldOffset(0)]
    public byte Group { get; }

    [field: FieldOffset(1)]
    public byte Command { get; }

    /// <summary>
    /// The two bytes as the client writes them, the group first: the index of the handler table.
    /// </summary>
    [field: FieldOffset(0)]
    public short Value { get; }

    public MessageOpcode(BinaryReader reader) => Value = reader.ReadInt16();

    public MessageOpcode(byte group, byte command) => (Group, Command) = (group, command);

    public MessageOpcode(GroupOpcode group, byte command) : this((byte)group, command) { }

    /// <summary>
    /// The opcode of a command of the group. Every command enum is a byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MessageOpcode From<TCommand>(GroupOpcode group, TCommand command) where TCommand : unmanaged, Enum
    {
        if (Unsafe.SizeOf<TCommand>() != sizeof(byte))
        {
            throw new ArgumentException($"{typeof(TCommand).Name} is not a byte, so it is no command.", nameof(command));
        }

        return new MessageOpcode(group, Unsafe.As<TCommand, byte>(ref command));
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Group:X2}:{Command:X2}");

    public string ToFullString() => string.Create(CultureInfo.InvariantCulture, $"{(GroupOpcode)Group}:{ToCommandString()}");

    /// <summary>
    /// The name of the command in the enum of its group. The client picks the group; a name for a log must not throw on one
    /// nobody knows.
    /// </summary>
    public string ToCommandString() => Commands[Group] is { } commands
        ? Enum.Format(commands, Command, "G")
        : string.Create(CultureInfo.InvariantCulture, $"0x{Command:X2}");

    private static Type?[] CommandsOfTheGroups()
    {
        var commands = new Type?[byte.MaxValue + 1];

        foreach (var group in Enum.GetValues<GroupOpcode>())
        {
            var name = $"{typeof(GroupOpcode).Namespace}.{group}Opcode";

            commands[(byte)group] = typeof(GroupOpcode).Assembly.GetType(name) is { IsEnum: true } type ? type : null;
        }

        return commands;
    }
}
