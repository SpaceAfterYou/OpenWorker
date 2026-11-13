using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Responses;

/// <summary>
/// An object the player saw is gone: its time ran out, its owner left or put down another.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct SocialItemOutResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Out;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public SocialItemValue Prop { get; init; }
}
