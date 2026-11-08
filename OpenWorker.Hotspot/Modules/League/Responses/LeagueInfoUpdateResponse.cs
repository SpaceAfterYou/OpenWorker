using System.Diagnostics;
using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.League.DataTypes;
using OpenWorker.Hotspot.Modules.League.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The numbers of the league changed.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueInfoUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InfoUpdate;

    public int League { get; init; }
    public int Rank { get; init; }
    public long Money { get; init; }
    public short Members { get; init; }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(League);
        writer.Write(Rank);
        writer.Write(Money);
        writer.Write(Members);
    }
}
