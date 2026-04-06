using OpenWorker.Extensions;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Boosters.Enums;
using OpenWorker.Hotspot.Modules.Boosters.Extensions;

namespace OpenWorker.Hotspot.Modules.Boosters.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct BoosterAddResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Booster;
    private const BoosterOpcode Command = BoosterOpcode.Add;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public short Identifier { get; init; }
    public TimeSpan Remaining { get; init; }
    public BoosterConsumeArea Area { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write(Area);
        writer.Write(Identifier);
        writer.Write(Remaining);
    }

#endregion Interface: IWritableData
}
