using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Modules.Persons.Types;

namespace OpenWorker.Hotspot.Modules.Channels.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ChannelChangeResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Channel;
    private const ChannelOpcode Command = ChannelOpcode.Change;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public EnterMapResultValue Value { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    /// <summary>
    /// The same enter-map record as the move to another world: the client takes its map as the one it is in, and moves to
    /// another server only when the flag says so.
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(Value);
    }

#endregion Interface: IWritableData

    public static ChannelChangeResponse Error => new() { Value = EnterMapResultValue.Error };
}
