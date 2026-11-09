using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Rewards.Types;

namespace OpenWorker.Hotspot.Modules.Vaccum.Responses;

/// <summary>
/// A person has taken a cube and got the items. A result other than 0 tells why it did not work out.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct VaccumClickCompleteResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Vaccum;
    private const VaccumOpcode Command = VaccumOpcode.ClickComplete;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Actor { get; init; }

    public int Cube { get; init; }

    /// <summary>
    /// How many times the cube can still be taken.
    /// </summary>
    public short Remain { get; init; }

    public IReadOnlyCollection<RewardItemValue> Items { get; init; }

    public int Result { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Cube);
        writer.Write(Remain);

        RewardItemValue.WriteList(writer, Items ?? []);

        writer.Write(Result);
    }

#endregion Interface: IWritableData
}
