using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct SoulCompleteRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Complete;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Identifier { get; } = reader.ReadInt32();

#endregion Message: Body
}
