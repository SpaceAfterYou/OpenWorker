using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyMatchingExitResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingExit;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    /// <summary>
    /// TODO: ta fuck. Actor?
    /// </summary>
    public int Character { get; init; }
    public PartyMatchingExit Reason { get; init; }
}
