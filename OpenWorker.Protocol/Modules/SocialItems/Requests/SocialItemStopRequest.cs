using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Requests;

/// <summary>
/// A player stops using the social object its window shows.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct SocialItemStopRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Stop;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Prop { get; }
}
