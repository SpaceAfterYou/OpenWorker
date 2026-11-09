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
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// The league has another master.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueMasterChangeResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.Delegate;

    public int League { get; init; }
    public int Previous { get; init; }
    public int Master { get; init; }
    public int Result { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
