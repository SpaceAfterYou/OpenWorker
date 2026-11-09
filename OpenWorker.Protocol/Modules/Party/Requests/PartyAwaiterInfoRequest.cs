using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PartyAwaiterInfoRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.AwaiterInfo;
    
    public MessageOpcode Opcode => new(Group, (byte)Command);

    public ActorValue Actor { get; }
    public byte Select { get; }
}