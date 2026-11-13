using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Responses;

/// <summary>
/// A player no longer uses an object: to everybody who sees the player.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct SocialItemStopResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Stop;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; init; }

    public SocialItemValue Prop { get; init; }
}
