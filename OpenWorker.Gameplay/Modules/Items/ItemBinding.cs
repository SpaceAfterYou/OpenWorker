using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// When an item becomes bound to its owner and what a bound item may no longer do. The one place of these rules: the
/// inventory, the item setup, the post, the trade and the private shop all ask here.
/// </summary>
/// <remarks>
/// The table says <em>when</em> an item binds (<see cref="ItemBindType"/>); the item itself carries <em>whether</em> it has
/// (<see cref="ItemBindState"/>). An item made with the rule "on pick-up" is bound from the start; one with the rule "on
/// equip" is free until it is worn first; a gem set into an item binds it; a bound broach set into clothing binds it for as
/// long as the broach stays.
/// </remarks>
public static class ItemBinding
{
    /// <summary>
    /// What the characters are given when they are made: bound.
    /// </summary>
    public const ItemBindState StartingItem = ItemBindState.Bound;

    /// <summary>
    /// Whether the item can no longer change hands (a trade, a shop of a player, a letter).
    /// </summary>
    public static bool IsBound(ItemBindState state) => state != ItemBindState.Free;

    /// <summary>
    /// Why an item may not go to another player, if it may not: a broken item (it can only be sold to a shop of the game) is
    /// asked first, then a bound one. A trade, a shop of a player and a letter all hold items back by this rule.
    /// </summary>
    public static ItemHeldBack HeldBack(ItemInstance item)
    {
        if (item.Flag == ItemRestoreService.BrokenFlag)
        {
            return ItemHeldBack.Broken;
        }

        return IsBound(item.BindState) ? ItemHeldBack.Bound : ItemHeldBack.Not;
    }

    /// <summary>
    /// The state of an item that has just been made from a row of the table.
    /// </summary>
    public static ItemBindState OnMade(ItemBindType rule)
    {
        return rule == ItemBindType.OnPickUp ? ItemBindState.Bound : ItemBindState.Free;
    }

    /// <summary>
    /// The state of an item that is worn: one with the rule "on equip" binds if it is not bound already.
    /// </summary>
    public static ItemBindState OnWorn(ItemBindState state, ItemBindType rule)
    {
        if (rule != ItemBindType.OnEquip)
        {
            return state;
        }

        return state switch
        {
            ItemBindState.Free => ItemBindState.Bound,
            ItemBindState.BoundByBroach => ItemBindState.BoundAndBroach,
            _ => state
        };
    }

    /// <summary>
    /// The state of an item that a gem is set into: a free item is bound, a bound one stays as it is.
    /// </summary>
    public static ItemBindState OnGemSet(ItemBindState state)
    {
        return state == ItemBindState.Free ? ItemBindState.Bound : state;
    }

    /// <summary>
    /// The state of clothing that a bound broach is set into: a free piece is bound by the broach, a bound one by both; one
    /// the broach binds already stays as it is. Nothing changes when the state does not change (<c>null</c>).
    /// </summary>
    public static ItemBindState? OnBoundBroachSet(ItemBindState state)
    {
        return state switch
        {
            ItemBindState.Free => ItemBindState.BoundByBroach,
            ItemBindState.Bound => ItemBindState.BoundAndBroach,
            _ => null
        };
    }

    /// <summary>
    /// The state of an item that an unsealing item has been used on: bound for good becomes free; the other states are not
    /// the ones an unsealing item frees (<c>null</c>).
    /// </summary>
    public static ItemBindState? OnUnsealed(ItemBindState state)
    {
        return state == ItemBindState.Bound ? ItemBindState.Free : null;
    }

    /// <summary>
    /// Whether a stack of the item can take items that are made now: stacks of one state only, unless the item is bound from
    /// the start, when all of its stacks are bound alike.
    /// </summary>
    public static bool CanStack(ItemBindState stack, ItemBindState made, ItemBindType rule)
    {
        return IsBound(stack) == IsBound(made) || rule == ItemBindType.OnPickUp;
    }
}

/// <summary>
/// Why an item may not change hands (<see cref="ItemBinding.HeldBack"/>).
/// </summary>
public enum ItemHeldBack
{
    /// <summary>
    /// The item may go to another player.
    /// </summary>
    Not,

    /// <summary>
    /// The item is broken: only a shop of the game takes it.
    /// </summary>
    Broken,

    /// <summary>
    /// The item is bound to its owner.
    /// </summary>
    Bound
}
