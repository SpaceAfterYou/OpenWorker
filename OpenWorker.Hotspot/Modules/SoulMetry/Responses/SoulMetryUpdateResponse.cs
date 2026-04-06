using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryUpdateResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Update;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public SoulMetryValue Data { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Data.Write(writer);
    }

#endregion Interface: IWritableData
}
