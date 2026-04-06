using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.League.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct LeagueOverlapNameRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.League;
    private const LeagueOpcode Command = LeagueOpcode.OverlapName;

    public string Name { get; } = reader.ReadUtf8UnicodeString();
    
    public MessageOpcode Opcode => new(Group, Command);
}