using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryAddResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Add;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
    }

#endregion Interface: IWritableData
}
