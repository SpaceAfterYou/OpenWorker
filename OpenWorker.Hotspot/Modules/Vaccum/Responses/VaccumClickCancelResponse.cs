using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Vaccum.Types;

namespace OpenWorker.Hotspot.Modules.Vaccum.Responses;

/// <summary>
/// A person has stopped taking a cube.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct VaccumClickCancelResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Vaccum;
    private const VaccumOpcode Command = VaccumOpcode.ClickCancel;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public int Actor { get; init; }

    public int Cube { get; init; }

    public int Result { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Cube);
        writer.Write(Result);
    }

#endregion Interface: IWritableData
}
