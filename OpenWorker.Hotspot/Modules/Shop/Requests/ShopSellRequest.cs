using OpenWorker.Domain.Types;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Requests;

[HotspotMessage(Group, Command)]
public readonly struct ShopSellRequest(BinaryReader reader) : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Shop;
    private const ShopOpcode Command = ShopOpcode.Sell;

    public ActorValue Npc { get; } = new(reader);

    public IReadOnlyList<ShopSellEntry> Entries { get; } = Enumerable
        .Range(0, reader.ReadByte())
        .Select(_ => new ShopSellEntry(reader))
        .ToArray();

    public MessageOpcode Opcode => new(Group, Command);
}