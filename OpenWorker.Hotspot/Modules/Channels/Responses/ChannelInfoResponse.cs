using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Channels.Extensions;
using OpenWorker.Hotspot.Modules.Channels.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ChannelInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Info;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public short Location { get; init; }
    public IReadOnlyCollection<ChannelValue> Values { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Location);
        writer.Write((byte)Values.Count);

        foreach (var value in Values)
        {
            writer.Write(value);
        }
    }

#endregion Interface: IWritableData
}
