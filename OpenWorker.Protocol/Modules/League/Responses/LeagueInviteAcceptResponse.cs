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
/// The one you asked joined.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct LeagueInviteAcceptResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.InviteAccept;

    public int Requester { get; init; }
    public int Target { get; init; }
    public string TargetName { get; init; } = string.Empty;
    public int League { get; init; }
    public int Result { get; init; }

    public LeagueInviteAcceptResponse()
    {
    }

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Requester);
        writer.Write(Target);
        writer.WritePersonName(TargetName);
        writer.Write(League);
        writer.Write(Result);
    }
}
