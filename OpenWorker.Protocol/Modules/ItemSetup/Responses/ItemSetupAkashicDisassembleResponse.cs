using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// A card taken out of the akashic record book: the client drops it from the book, and shows the result when it is 1.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemSetupAkashicDisassembleResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.AkashicDisassemble;

    /// <summary>
    /// 1 when the card came out; the client drops the card from the book whatever it is.
    /// </summary>
    public byte Result { get; init; }

    public AkashicRecordValue Record { get; init; }

    public MessageOpcode Opcode => new(Group, (byte)Command);
}
