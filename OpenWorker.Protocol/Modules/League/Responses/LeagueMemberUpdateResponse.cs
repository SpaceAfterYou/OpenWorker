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
/// A member came, went or moved.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueMemberUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.MemberUpdate;

    public int League { get; init; }
    public int Actor { get; init; }
    public byte Login { get; init; }
    public byte Level { get; init; }
    public short World { get; init; }
    public long PlayDate { get; init; }
    public string Name { get; init; } = string.Empty;
    public byte Channel { get; init; }

    public LeagueMemberUpdateResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(League);
        writer.Write(Actor);
        writer.Write(Login);
        writer.Write(Level);
        writer.Write(World);
        writer.Write(PlayDate);
        writer.WritePersonName(Name);
        writer.Write(Channel);
    }
}
