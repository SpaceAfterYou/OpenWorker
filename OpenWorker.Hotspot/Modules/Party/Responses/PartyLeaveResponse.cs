using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyLeaveResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.Leave;

    public MessageOpcode Opcode => new(Group, Command);

    public int Identifier { get; init; }
    public ActorValue Member { get; init; }

    public bool IsKickOut { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Identifier);
        writer.Write(Member);
        writer.Write(IsKickOut);
    }
}
