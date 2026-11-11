namespace OpenWorker.Hotspot.Modules.Items.Enums;

/// <summary>
/// Where one item stands with its owner: whether it may still change hands. The client reads it as one byte of the item
/// and shows a bound item as "Bound Item, untradable". It is not the rule of the item table (<see cref="ItemBindType"/>):
/// that says when an item becomes bound, this says whether it is.
/// </summary>
public enum ItemBindState : byte
{
    /// <summary>
    /// The item has never been bound: it can be traded, sold in a shop of a player and mailed.
    /// </summary>
    Free = 0,

    /// <summary>
    /// Bound for good: by picking it up when its table says so, by wearing it when its table says so, or by a gem set into it.
    /// Only an unsealing item frees it.
    /// </summary>
    Bound = 1,

    /// <summary>
    /// Free in itself, bound for as long as a bound broach is set into it (clothing only).
    /// </summary>
    BoundByBroach = 2,

    /// <summary>
    /// Bound for good, and a bound broach is set into it as well.
    /// </summary>
    BoundAndBroach = 3
}
