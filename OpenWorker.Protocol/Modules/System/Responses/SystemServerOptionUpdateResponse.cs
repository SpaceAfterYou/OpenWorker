using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.System.Responses;

/// <summary>
/// The contents the server offers changed. They are a list of the server, not a value, so the writer is written by hand.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct SystemServerOptionUpdateResponse : IResponseHotspotMessage, IWireWritable
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.ServerOptionUpdate;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required ServerContent Contents { get; init; }

#endregion Message: Body

#region Interface: IWireWritable

    public void Write(ref PacketWriter writer)
    {
        Contents.Write(ref writer);
    }

#endregion Interface: IWireWritable
}
