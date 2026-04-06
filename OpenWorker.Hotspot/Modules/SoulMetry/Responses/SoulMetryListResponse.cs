using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SoulMetry.Types;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.List;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public IReadOnlyList<SoulMetryValue> Values { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((short)Values.Count);

        foreach (var value in Values)
        {
            value.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
