using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

/// <summary>
/// This packet doesn't have any content.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct PartyMazeClearResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MazeClear;

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
