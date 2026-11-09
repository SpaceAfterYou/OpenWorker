using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Helpers.Types;

namespace OpenWorker.Hotspot.Modules.Helpers.Responses;

[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct HelperSupportInfoResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Helper;
    private const HelperOpcode Command = HelperOpcode.SupportInfo;

    public MessageOpcode Opcode => new(Group, (byte)Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required HelperSupportInfoValue Info { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        Info.Write(writer);
    }

#endregion Interface: IWritableData
}
