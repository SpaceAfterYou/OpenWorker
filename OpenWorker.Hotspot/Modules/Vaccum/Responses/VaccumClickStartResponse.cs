using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Vaccum.Types;

namespace OpenWorker.Hotspot.Modules.Vaccum.Responses;

/// <summary>
/// A person is taking a cube. A result other than 0 tells why it cannot.
/// </summary>
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct VaccumClickStartResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Vaccum;
    private const VaccumOpcode Command = VaccumOpcode.ClickStart;

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
