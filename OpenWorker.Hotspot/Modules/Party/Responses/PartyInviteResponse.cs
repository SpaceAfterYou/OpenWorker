using OpenWorker.Domain.Types;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyInviteResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Invite;

    public ActorValue MasterActor { get; init; } = new(reader);
    public ActorValue RequestActor { get; init; } = new(reader);
    public string MasterName { get; init; } = reader.ReadUtf8UnicodeString(21);
    public string RequestName { get; init; } = reader.ReadUtf8UnicodeString(21);
    public int ReqServer { get; init; } = reader.ReadInt32();
    public int Result { get; init; } = reader.ReadInt32();

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(MasterActor);
        writer.Write(RequestActor);
        writer.WritePersonName(MasterName);
        writer.WritePersonName(RequestName);
        writer.Write(ReqServer);
        writer.Write(Result);
    }
}
