using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Channels.Extensions;
using OpenWorker.Hotspot.Modules.Channels.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Responses;

[HotspotMessage(Group, Command)]
public readonly struct ChannelInfoResponse(short location, IReadOnlyCollection<ChannelValue> values) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Info;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        writer.Write(location);
        writer.Write((byte)values.Count);

        foreach (var value in values)
        {
            writer.Write(value);
        }
    }
}