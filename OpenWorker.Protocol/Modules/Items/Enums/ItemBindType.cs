namespace OpenWorker.Hotspot.Modules.Items.Enums;

/// <summary>
/// The rule of an item in the item table: when the item becomes bound to its owner. What an item is at the moment is its
/// <see cref="ItemBindState"/>.
/// </summary>
public enum ItemBindType : byte
{
    /// <summary>
    /// The item never binds by itself.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// The item is bound when it is worn first.
    /// </summary>
    OnEquip = 0x1,

    /// <summary>
    /// The item is bound from the moment it is made.
    /// </summary>
    OnPickUp = 0x2
}