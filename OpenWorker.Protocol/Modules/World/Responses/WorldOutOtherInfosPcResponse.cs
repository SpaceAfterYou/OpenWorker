using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct WorldOutOtherInfosPcResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.OutOtherInfosPc;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    [WireArray(WireLength.Byte)]
    public required ActorValue[] Actors { get; init; }
}
