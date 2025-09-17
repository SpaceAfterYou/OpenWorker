using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Party.Requests;

[HotspotMessage(Group, Command)]
public readonly struct PartyMatchingEnterRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Party;
    private const PartyOpcode Command = PartyOpcode.MatchingEnter;

    // unsigned __int16 wModeID;
    // unsigned __int16 wModeMazeID;
    // - or -
    public int Location { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}