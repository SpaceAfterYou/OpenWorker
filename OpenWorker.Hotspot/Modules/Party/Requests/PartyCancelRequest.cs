using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct PartyCancelRequest(BinaryReader reader) : IRequestHotspotMessage, IWritableData
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Cancel;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; } = new(reader);
    public int ErrorCode { get; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(ErrorCode);
    }
}
