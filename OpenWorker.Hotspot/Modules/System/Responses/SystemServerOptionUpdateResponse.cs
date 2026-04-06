using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.System.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SystemServerOptionUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.System;
    private const SystemOpcode Command = SystemOpcode.ServerOptionUpdate;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
    }

#endregion Interface: IWritableData
}
