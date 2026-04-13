using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyChangeMasterResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.ChangeMaster;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Master { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Master);
    }
}
