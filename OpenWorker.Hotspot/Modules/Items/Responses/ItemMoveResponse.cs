using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Extensions;
using OpenWorker.Hotspot.Modules.Items.Requests;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

public readonly record struct ItemMoveInfoValue
{
    public required MoveItemValue Src { get; init; }
    public required MoveItemValue Dest { get; init; }

    /// <summary>
    /// TODO: What is it
    /// </summary>
    public byte SrcBind { get; init; }
    
    /// <summary>
    /// TODO: What is it
    /// </summary>
    public byte DestBind { get; init; }
}

[HotspotMessage(Group, Command)]
public readonly record struct ItemMoveResponse(ItemMoveInfoValue[] Info) : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.Move;

    public MessageOpcode Opcode => new(Group, Command);

    public void ToBinary(BinaryWriter writer)
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
}