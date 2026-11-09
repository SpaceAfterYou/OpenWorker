using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Extensions;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;

namespace OpenWorker.Hotspot.Modules.Items.Responses;

/// <summary>
/// The battle points of the player after they changed.
/// </summary>
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly struct ItemUpdateBpResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Item;
    private const ItemOpcode Command = ItemOpcode.UpdateBp;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Actor { get; init; }
    public long Total { get; init; }
    public byte Type { get; init; }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Actor);
        writer.Write(Total);
        writer.WriteUtf16UnicodeString(string.Empty);
        writer.Write(Type);
    }
}
