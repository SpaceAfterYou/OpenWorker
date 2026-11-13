using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Messages.Response.World;

/// <summary>
/// The time of the server, in seconds since 1970. It is read from the clock when the message is written, so the writer is
/// written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct WorldCurrentDateClientMessage : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    public const GroupOpcode Group = GroupOpcode.World;
    public const WorldOpcode Command = WorldOpcode.CurDate;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        writer.WriteDateTimeOffset(DateTimeOffset.UtcNow);
    }

#endregion Interface: IWireWritable
}
