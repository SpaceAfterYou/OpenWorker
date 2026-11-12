using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Hotspot.Handler.Attributes;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Modules.Exchange.Requests;

/// <summary>
/// A page of the listings that match what the search window asks. Every filter is -1 when it is not set; the page counts
/// from 1.
/// </summary>
[WireMessage]
[HotspotMessage(Group, (byte)Command, HotspotMessageDirection.Request)]
public readonly partial struct ExchangeSearchRequest : IRequestHotspotMessage
{
    private const GroupOpcode Group = GroupOpcode.Exchange;
    private const ExchangeOpcode Command = ExchangeOpcode.Search;

    public MessageOpcode Opcode => new(Group, (byte)Command);

    public int Page { get; init; }

    /// <summary>
    /// One item by its number, or 0 for any.
    /// </summary>
    public int Item { get; init; }

    public int Category { get; init; }
    public int SubCategory { get; init; }

    /// <summary>
    /// The character the item is made for.
    /// </summary>
    public int UseClass { get; init; }

    public int LevelMin { get; init; }
    public int LevelMax { get; init; }
    public int GradeMin { get; init; }
    public int GradeMax { get; init; }

    /// <summary>
    /// The highest price of one.
    /// </summary>
    public long Price { get; init; }

    /// <summary>
    /// The lowest upgrade.
    /// </summary>
    public int Upgrade { get; init; }
}
