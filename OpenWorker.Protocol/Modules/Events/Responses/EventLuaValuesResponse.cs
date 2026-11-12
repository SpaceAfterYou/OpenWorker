using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Events.Responses;

/// <summary>
/// Names that the client hands one by one to the function of its script that handles event values. Written by hand
/// because a name the client cannot hold is refused before anything is written.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct EventLuaValuesResponse : IResponseHotspotMessage, IWireWritable
{
    /// <summary>
    /// The client keeps a name in a buffer of 30 bytes and drops the whole message if one is longer.
    /// </summary>
    public const int MaxNameLength = 29;

#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Event;
    private const EventOpcode Command = EventOpcode.LuaValues;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<string> Scripts { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        if (Scripts.Any(e => e.Length is 0 or > MaxNameLength))
        {
            throw new InvalidOperationException("The name of an event value has to fit the buffer of the client.");
        }

        writer.WriteLength(WireLength.Byte, Scripts.Count);

        foreach (var script in Scripts)
        {
            writer.WriteUtf8AsciiStringWithoutTerminator(script);
        }
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }

#endregion Interface: IWireWritable
}
