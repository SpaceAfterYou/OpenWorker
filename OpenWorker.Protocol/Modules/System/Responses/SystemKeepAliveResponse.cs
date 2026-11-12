using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.System.Responses;

/// <summary>
/// The answer to a keep-alive. The time goes as 64-bit ticks, not as the seconds the generator writes a time in, so the
/// writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct SystemKeepAliveResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.KeepAlive;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public TimeSpan Time { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        // The client reads it and does nothing with it.
        writer.WriteInt64(Time.Ticks);
    }

    public void Write(BinaryWriter writer)
    {
        WireBridge.Write(writer, this);
    }

#endregion Interface: IWireWritable
}
