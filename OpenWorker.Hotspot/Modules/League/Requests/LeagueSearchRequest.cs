using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueSearchRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Search;

    public byte State { get; } = reader.ReadByte();
    public string Name { get; } = reader.ReadLeagueName();
    public string Master { get; } = reader.ReadPersonName();
    
    public IReadOnlyList<LeagueInfo> Leagues { get; } = Enumerable
        .Range(0, reader.ReadByte())
        .Select(_ => new LeagueInfo(reader))
        .ToArray();
    
    public MessageOpcode Opcode => new(Group, Command);
}