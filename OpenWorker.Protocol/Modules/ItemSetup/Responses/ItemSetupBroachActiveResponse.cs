using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.ItemSetup.Responses;

/// <summary>
/// A place for a broach opened on a piece of clothing: its serial and the new state of its fifteen places, which the client
/// puts on the item of that serial.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Response)]
public readonly partial struct ItemSetupBroachActiveResponse : IResponseHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.ItemSetup;
    private const ItemSetupOpcode Command = ItemSetupOpcode.BroachActive;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public long Serial { get; init; }

    /// <summary>
    /// One character per place: '0' none, '1' open, '2' closed. The client takes 1 to 15 characters.
    /// </summary>
    [WireString(WireStringFormat.Utf8AsciiWithoutTerminator)]
    public required string State { get; init; }
}
