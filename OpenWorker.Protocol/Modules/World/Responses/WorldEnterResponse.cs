using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct WorldEnterResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.EnterRes;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public EnterMapResultValue Map { get; init; }

    /// <summary>
    /// The map is a scene of the class story; never sent as one.
    /// </summary>
    public bool IsClassScene { get; init; }

    /// <summary>
    /// Players may fight each other on the map; never sent as such.
    /// </summary>
    public bool IsPvpEnabled { get; init; }

#endregion Message: Body
}
