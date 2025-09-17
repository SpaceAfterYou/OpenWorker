using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Responses;

[HotspotMessage(Group, Command)]
public readonly struct LeagueSearchResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Search;
    
    public byte State { get; init; }
    public string Name { get; init; }
    public string Master { get; init; }
    public IReadOnlyList<LeagueInfo> Leagues { get; init; }
    public ActorValue[] Actors { get; init; }
    
    public MessageOpcode Opcode => new(Group, Command);
    
    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(State);
        writer.Write(Name);
        writer.Write(Master);
        
        writer.Write((byte)Leagues.Count);

        foreach (var league in Leagues)
        {
            writer.Write(league);
        }
        
        writer.Write(Actors);
    }
}