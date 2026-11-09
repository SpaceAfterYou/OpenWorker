using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Res.Types;
using Arch.Core;
using OpenWorker.Hotspot.Modules.Items.Enums;

namespace OpenWorker.Channel.Community;

/// <summary>
/// A party as the rest of the server sees it: its identifier, its master and everybody in it, the master included.
/// </summary>
public sealed record PartyView(int Id, int Master, IReadOnlyList<int> Members);

/// <summary>
/// Who stands with a person in a party. A person alone gets an empty list and no party.
/// </summary>
public interface IPartyRoster
{
    ValueTask<IReadOnlyList<int>> MembersAsync(int person);

    /// <summary>
    /// The party of the person, or null when they are alone.
    /// </summary>
    ValueTask<PartyView?> PartyAsync(int person);
}

/// <summary>
/// Everybody who belongs to a league.
/// </summary>
public interface ILeagueRoster
{
    ValueTask<IReadOnlyList<int>> MembersAsync(int league);
}

/// <summary>
/// Who has the other in their friend list or block list.
/// </summary>
public interface IPersonRelations
{
    ValueTask<bool> IsFriendAsync(int person, int other);

    ValueTask<bool> IsBlockedAsync(int person, int other);
}

/// <summary>
/// The use of a megaphone: the item has to be taken from the bag before the words go out.
/// </summary>
public interface IMegaphoneSource
{
    /// <summary>
    /// Returns zero when the item was taken, otherwise the code of the message the client shows.
    /// </summary>
    ValueTask<ushort> TakeAsync(Entity player, StorageGroup storage, short slot);
}

/// <summary>
/// A megaphone is one item of the bag, of the use kind of a megaphone, which a use takes away, as the original does.
/// </summary>
public sealed class InventoryMegaphone(InventoryService inventory) : IMegaphoneSource
{
    /// <summary>The client's line for a slot without a usable item.</summary>
    public const ushort NoItem = 52011;

    /// <summary>The client's line for an item that is not a megaphone.</summary>
    public const ushort NotAMegaphone = 52002;

    public ValueTask<ushort> TakeAsync(Entity player, StorageGroup storage, short slot)
    {
        var item = inventory.InventoryOf(player)?.At(storage, slot);

        if (item is null || item.Count <= 0)
        {
            return ValueTask.FromResult(NoItem);
        }

        if (inventory.Catalog.Classify(item.Item) is not { } classify)
        {
            return ValueTask.FromResult(NoItem);
        }

        if (classify.ItemUseType != ItemUseType.MEGAPHONE)
        {
            return ValueTask.FromResult(NotAMegaphone);
        }

        inventory.Spend(player, item, 1);

        return ValueTask.FromResult<ushort>(0);
    }
}
