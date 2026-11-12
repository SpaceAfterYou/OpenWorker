using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Helpers.Responses;

/// <summary>
/// The helpers of the person. The client forgets the ones it knew.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct HelperListLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.ListLoad;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Person { get; init; }

    [WireArray(WireLength.Signed16)]
    public required IReadOnlyList<HelperValue> Helpers { get; init; }

    public byte AutoSummon { get; init; }

#endregion Message: Body
}
