using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Helpers.Responses;

/// <summary>
/// A helper was given the orb of a friend: the result (0 when it was done), the helper, and the helper as it is now. The
/// client refreshes the window when the helper is the one it shows.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct HelperSupportEquipResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.SupportEquip;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Result { get; init; }

    public int Helper { get; init; }

    public required HelperValue Record { get; init; }

#endregion Message: Body
}
