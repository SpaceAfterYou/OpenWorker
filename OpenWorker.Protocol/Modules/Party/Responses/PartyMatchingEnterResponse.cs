using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyMatchingEnterResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingEnter;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Identifier { get; init; }
    [WireArray(Count = PartyModuleDefines.MaxMemberCount)]
    public PartyMemberInfoValue[] MemberList { get; init; }

    /// <summary>
    /// TODO: Remaining time for search.
    /// </summary>
    public int RemainTick { get; init; }
}
