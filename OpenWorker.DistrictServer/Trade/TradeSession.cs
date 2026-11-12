using Arch.Core;
using OpenWorker.Domain.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.DistrictServer.Trade;

/// <summary>
/// An item one side puts on the table: where it lies and what it was when it was put there. The item stays in the bag of
/// its owner until the exchange, which takes it only if it is still the same (<see cref="TradeGoods.StillHolds"/>).
/// </summary>
/// <param name="Look">What the item was when it was offered, beyond its number: the other side agreed to exactly that.</param>
public readonly record struct TradeOffer(StorageGroup Storage, short Slot, long Serial, int Item, short Count, string Look);

/// <summary>
/// What one player of a trade offers and how far it has agreed.
/// </summary>
public sealed class TradeSide(Entity player, ActorValue actor)
{
    public Entity Player { get; } = player;

    public ActorValue Actor { get; } = actor;

    public List<TradeOffer> Items { get; } = [];

    public long Money { get; set; }

    /// <summary>
    /// The player has locked its offer: it cannot change it, and the other side may confirm.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// The player has pressed the final button. The exchange happens when both have.
    /// </summary>
    public bool Confirmed { get; set; }

    public int IndexOf(StorageGroup storage, short slot)
    {
        return Items.FindIndex(e => e.Storage == storage && e.Slot == slot);
    }
}

/// <summary>
/// A trade between two players of one channel. It starts as an invitation (<see cref="IsOpen"/> false) that the invited
/// player answers or that expires, then both sides offer items and gold, lock, and confirm. It lives on the world loop only.
/// </summary>
public sealed class TradeSession(TradeSide inviter, TradeSide invitee, long invitedAt)
{
    public TradeSide Inviter { get; } = inviter;

    public TradeSide Invitee { get; } = invitee;

    /// <summary>
    /// When the invitation was sent, as a timestamp of the <see cref="TimeProvider"/> of the server.
    /// </summary>
    public long InvitedAt { get; } = invitedAt;

    /// <summary>
    /// The invited player has accepted: the trade window is open on both sides.
    /// </summary>
    public bool IsOpen { get; set; }

    public TradeSide SideOf(Entity player)
    {
        return Inviter.Player == player ? Inviter : Invitee;
    }

    public TradeSide OtherOf(Entity player)
    {
        return Inviter.Player == player ? Invitee : Inviter;
    }
}
