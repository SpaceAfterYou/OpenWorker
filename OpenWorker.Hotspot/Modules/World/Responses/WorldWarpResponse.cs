using System.Numerics;
using OpenWorker.Domain.Components;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.World.Enums;
using OpenWorker.Hotspot.Modules.World.Extensions;

namespace OpenWorker.Hotspot.Modules.World.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct WorldWarpResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.World;
    private const WorldOpcode Command = WorldOpcode.WarpRes;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public ActorComponent Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Rotation { get; init; }
    public WorldWrapResult Result { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.WriteActor(Actor);
        writer.Write(Result);
        writer.Write(Position);
        writer.Write(Rotation);
    }

#endregion Interface: IWritableData
}
