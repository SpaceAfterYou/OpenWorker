using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.SocialItems.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.SocialItems.Responses;

/// <summary>
/// The owner's object is down: the client keeps it as the player's own.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct SocialItemStartResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.SocialItem;
    private const SocialItemOpcode Command = SocialItemOpcode.Start;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public SocialItemValue Prop { get; init; }
}
