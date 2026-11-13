using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Responses;

/// <summary>
/// An object has been put down where the player sees it.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct SocialItemInResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.In;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public SocialItemPlacedValue Prop { get; init; }
}
