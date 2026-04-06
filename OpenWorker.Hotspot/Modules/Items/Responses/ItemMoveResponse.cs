using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Requests;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly struct ItemMoveResponse : IResponseHotspotMessage
{
#region Interface: IHotspotMessage

    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Move;

    public MessageOpcode Opcode => new(Group, Command);

#endregion Interface: IHotspotMessage

#region Message: Body

    public required ItemMoveInfoValue[] Info { get; init; }

#endregion Message: Body

#region Interface: IWritableData

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Info.Length);

        foreach (var item in Info)
        {
            Write(writer, item.Src);
            Write(writer, item.Dest);

            writer.Write(item.SrcBind);
            writer.Write(item.DestBind);
        }
    }

    private static void Write(BinaryWriter writer, MoveItemValue value)
    {
        writer.Write(value.Storage);
        writer.Write(value.Item);
        writer.Write(value.Index);
    }

#endregion Interface: IWritableData
}
