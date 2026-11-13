using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The items a person has put up elsewhere: offered in a trade or in its private shop. Such an item stays in the bag but
/// cannot be used.
/// </summary>
public interface IItemLocks
{
    /// <summary>
    /// Whether the item in a slot of the person's bags is put up.
    /// </summary>
    bool IsLocked(Entity player, StorageGroup storage, short slot);
}
