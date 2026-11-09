using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Persons.Responses;

/// <summary>
/// The reward of a district or a maze has been given.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct PersonGetRewardSharePointResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Character;
    private const CharacterOpcode Command = CharacterOpcode.GetRewardSharePoint;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Identifier { get; init; }
    public byte RewardStep { get; init; }
    public byte Result { get; init; }
    public IReadOnlyCollection<RewardItemValue> Items { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Identifier);
        writer.Write(RewardStep);
        writer.Write(Result);
        writer.Write((byte)Items.Count);

        foreach (var item in Items)
        {
            item.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
