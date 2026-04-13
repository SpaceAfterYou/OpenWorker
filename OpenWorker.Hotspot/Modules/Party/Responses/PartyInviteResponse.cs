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

    public ActorValue MasterActor { get; } = new(reader);
    public ActorValue RequestActor { get; } = new(reader);
    public string MasterName { get; } = reader.ReadUtf8UnicodeString(21);
    public string RequestName { get; } = reader.ReadUtf8UnicodeString(21);
    public int ReqServer { get; } = reader.ReadInt32();
    public int Result { get; } = reader.ReadInt32();

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
