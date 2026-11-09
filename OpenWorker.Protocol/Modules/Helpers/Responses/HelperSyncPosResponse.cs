using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Helpers.Responses;

/// <summary>
/// Where a helper stands now.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct HelperSyncPosResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.SyncPos;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Actor { get; init; }

    public float X { get; init; }

    public float Y { get; init; }

    public float Z { get; init; }

#endregion Message: Body
}
