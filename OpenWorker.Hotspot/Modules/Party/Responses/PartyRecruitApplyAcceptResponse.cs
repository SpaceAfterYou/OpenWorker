using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

/// <summary>
/// Client is not using the content of this message.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyRecruitApplyAcceptResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyAccept;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; } = new(reader);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
    }
}
