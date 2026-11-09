using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;

namespace OpenWorker.Hotspot.Handler.Attributes;

/// <summary>
/// The opcode of a message and the way it goes. A message names its typed command constant and gives it as a byte:
/// <c>[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]</c>, with <c>Command</c> a constant of the enum
/// of the group (<see cref="SystemOpcode" /> for <see cref="GroupOpcode.System" />). The handler table reads it to find the
/// message of a handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class HotspotMessageAttribute(GroupOpcode group, byte command, HotspotMessageDirection direction) : Attribute
{
    public MessageOpcode Opcode { get; } = new(group, command);

    public HotspotMessageDirection Direction { get; } = direction;
}
