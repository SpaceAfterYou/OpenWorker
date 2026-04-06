using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Friends.Extensions;
using OpenWorker.Hotspot.Modules.Friends.Types;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct FriendLoadResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.Load;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyCollection<FriendValue> List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)List.Count);

        foreach (var value in List)
        {
            writer.Write(value);
        }
    }

#endregion Interface: IWritableData
}
