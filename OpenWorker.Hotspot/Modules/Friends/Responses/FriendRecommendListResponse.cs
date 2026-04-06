using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

/// <summary>
/// Friends will not show
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct FriendRecommendListResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.RecommendList;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorValue Actor { get; init; }
    public required IReadOnlyCollection<FriendRecommendValue> List { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);

        writer.Write((byte)List.Count);

        foreach (var value in List)
        {
            writer.WritePersonName(value.Name);
            writer.WriteActor(value.Actor);
            writer.Write(value.Level);
            writer.Write(value.Hero);
            writer.Write(value.World);
            writer.Write(value.Channel);
            writer.Write(value.IsLoggedIn);
        }
    }

#endregion Interface: IWritableData
}
