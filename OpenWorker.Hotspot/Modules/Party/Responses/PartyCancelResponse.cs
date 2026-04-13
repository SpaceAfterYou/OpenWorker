using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyCancelResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Cancel;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue ReqActor { get; init; } = new(reader);
    public ActorValue RejActor { get; init; } = new(reader);
    public string RejName { get; init; }
    public int ErrorCode { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(ReqActor);
        writer.Write(RejActor);
        writer.WritePersonName(RejName);
        writer.Write(ErrorCode);
    }
}
