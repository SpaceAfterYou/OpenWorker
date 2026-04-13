using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyUpdateMemberSgResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.UpdateMemberSg;

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        throw new NotImplementedException("unknown_libname_8. Unimplemented on the client side.");
    }
}
