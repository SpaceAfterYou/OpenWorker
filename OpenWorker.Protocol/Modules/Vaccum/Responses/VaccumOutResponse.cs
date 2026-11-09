using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Vaccum.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Vaccum.Responses;

/// <summary>
/// A cube is gone.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct VaccumOutResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Vaccum;
    private const VaccumOpcode Command = VaccumOpcode.Out;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Cube { get; init; }

    public byte Flag { get; init; }

#endregion Message: Body
}
