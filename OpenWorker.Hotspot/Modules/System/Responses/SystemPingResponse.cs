using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.System.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SystemPingResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.Ping;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public long KeepAlive { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        // In the client: GetTickCount64() - keepAlive = ping in ms
        writer.Write(KeepAlive);
    }

#endregion Interface: IWritableData
}
