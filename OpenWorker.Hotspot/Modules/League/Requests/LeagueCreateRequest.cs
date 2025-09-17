using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command)]
public readonly struct LeagueCreateRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Create;

    public string Name { get; } = reader.ReadLeagueName();
    public int Npc { get; } = reader.ReadInt32();
    public int League { get; } = reader.ReadInt32();
    public int Error { get; } = reader.ReadInt32();
    
    public MessageOpcode Opcode => new(Group, Command);
}