using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Channels.Requests;

[HotspotMessage(Group, Command, HotspotMessageDirection.Request)]
public readonly struct ChannelChangeRequest(BinaryReader reader) : IRequestHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Change;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public short Channel { get; } = reader.ReadInt16();

#endregion Message: Body
}
