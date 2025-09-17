using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Persons.Extensions;

namespace OpenWorker.Hotspot.Modules.Friends.Responses;

public readonly struct FriendRecommendValue
{
    public string Name { get; init; }
    public ActorValue Actor { get; init; }
    public byte Level { get; init; }
    public Hero Hero { get; init; }
    public byte Channel { get; init; }
    public short World { get; init; }
    // public bool IsLoggedIn { get; init; }
    public byte IsLoggedIn { get; init; }
}

/// <summary>
/// Friends will not show
/// </summary>
[HotspotMessage(Group, Command)]
public readonly struct FriendRecommendListResponse(Arch.Core.World world, Entity player, IReadOnlyCollection<FriendRecommendValue> list) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Friend;
    private const FriendOpcode Command = FriendOpcode.RecommendList;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
    {
        var actor = world.Get<ActorComponent>(player);
        
        writer.WriteActor(actor);

        writer.Write((byte)list.Count);
        
        foreach (var value in list)
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
}