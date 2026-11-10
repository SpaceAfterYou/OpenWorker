using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Gameplay.Modules.Items;

namespace OpenWorker.Server.Common;

/// <summary>
/// The titles an item gives go to the titles of the person, which keeps them and tells the client.
/// </summary>
public sealed class ItemTitles(TitleBook titles) : IItemTitles
{
    public bool CannotGrant(Entity player, int title)
    {
        return !titles.Catalog.TryGet(title, out _) || titles.Owns(player, title);
    }

    public ValueTask<bool> GrantAsync(Entity player, int title)
    {
        return titles.GrantAsync(player, title, TitleSource.Item);
    }
}
