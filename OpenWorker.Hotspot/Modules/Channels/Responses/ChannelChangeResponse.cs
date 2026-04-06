using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ChannelChangeResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Change;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public EnterMapResultValue Value { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Value.Result);
    }

#endregion Interface: IWritableData

    public static ChannelChangeResponse Error => new() { Value = EnterMapResultValue.Error };
}
