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
/// A member got a position or lost it.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct LeagueMemberPositionChangeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.MemberPositionChange;

    public int Actor { get; init; }
    public byte Position { get; init; }
    public byte State { get; init; }
    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, Command);

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Position);
        writer.Write(State);
        writer.Write(Result);
    }
}
