using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

/// <summary>
/// This packet doesn't have any content.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct PartyRecruitApplyListRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.RecruitApplyList;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
