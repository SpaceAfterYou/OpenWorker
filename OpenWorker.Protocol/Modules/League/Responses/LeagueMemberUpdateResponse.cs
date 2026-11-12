using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.League.Responses;

/// <summary>
/// A member came, went or moved.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct LeagueMemberUpdateResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.MemberUpdate;

    public LeagueMemberUpdateResponse()
    {
    }

    public int League { get; init; }
    public int Actor { get; init; }
    public byte Login { get; init; }
    public byte Level { get; init; }
    public short World { get; init; }
    public long PlayDate { get; init; }

    [WireString(WireStringFormat.Utf16Unicode)]
    public string Name { get; init; } = string.Empty;

    public byte Channel { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
