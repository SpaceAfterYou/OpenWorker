using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Rewards.Types;

namespace OpenWorker.Hotspot.Modules.SoulMetry.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct SoulMetryCompleteResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.SoulMetry;
    private const SoulMetryOpcode Command = SoulMetryOpcode.Complete;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Identifier { get; init; }

    public IReadOnlyCollection<RewardItemValue> Rewards { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Identifier);

        RewardItemValue.WriteList(writer, Rewards ?? []);
    }

#endregion Interface: IWritableData
}
