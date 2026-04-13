using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Party.Enums;
using OpenWorker.Hotspot.Modules.Party.Extensions;

namespace OpenWorker.Hotspot.Modules.Party.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct PartyMatchingExitResponse(BinaryReader reader) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingExit;

    public MessageOpcode Opcode => new(Group, Command);

    /// <summary>
    /// TODO: ta fuck. Actor?
    /// </summary>
    public int Character { get; init; }
    public PartyMatchingExit Reason { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Character);
        writer.Write(Reason);
    }
}
