using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Extensions;
using OpenWorker.Hotspot.Modules.Party.Types;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyMatchingEnterResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingEnter;

    public MessageOpcode Opcode => new(Group, Command);

    public int Identifier { get; init; } = reader.ReadInt32();
    public PartyMemberInfoValue[] MemberList { get; init; } = reader.ReadPartyMemberInfoListFixed();

    /// <summary>
    /// TODO: Remaining time for search.
    /// </summary>
    public int RemainTick { get; init; } = reader.ReadInt32();

    public void Write(BinaryWriter writer)
    {
        writer.Write(Identifier);
        writer.WriteFixed(MemberList);
        writer.Write(RemainTick);
    }
}
