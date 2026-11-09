using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Vaccum.Types;

namespace OpenWorker.Hotspot.Modules.Vaccum.Responses;

/// <summary>
/// All the cubes that can be taken now; a byte tells how many.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct VaccumInfosResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Vaccum;
    private const VaccumOpcode Command = VaccumOpcode.Infos;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required IReadOnlyList<VaccumCubeValue> Cubes { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(checked((byte)Cubes.Count));

        foreach (var cube in Cubes)
        {
            cube.Write(writer);
        }
    }

#endregion Interface: IWritableData
}
