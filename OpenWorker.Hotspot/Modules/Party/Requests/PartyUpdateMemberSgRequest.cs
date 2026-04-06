using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PartyUpdateMemberSgRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateMemberSg;
    
    public MessageOpcode Opcode => new(Group, Command);
}