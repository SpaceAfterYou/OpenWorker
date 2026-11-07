using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Helpers.Requests;

/// <summary>
/// Swaps two helpers in the list. The orders are filled by the server.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct HelperChangeOrderRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.ChangeOrder;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Person { get; } = reader.ReadInt32();

    public int First { get; } = reader.ReadInt32();

    public byte FirstOrder { get; } = reader.ReadByte();

    public int Second { get; } = reader.ReadInt32();

    public byte SecondOrder { get; } = reader.ReadByte();

    public int Error { get; } = reader.ReadInt32();

#endregion Message: Body
}
