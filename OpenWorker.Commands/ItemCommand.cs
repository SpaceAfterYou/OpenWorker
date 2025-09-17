using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Hotspot.Commands;

[StuffCommand("item")]
public sealed class ItemCommand(World world, StorageItemFactory storageItemFactory, StorageManager storageManager, ReadOnlyCollection<ItemRow> itemCollection) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "item (prototype)";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);

        if (int.TryParse(tokens.ElementAtOrDefault(0), out var prototype) is false)
        {
            SendTutorial(player, nameof(prototype));
            return ValueTask.FromResult(false);
        }

        if (itemCollection.Any(e => e.Id == prototype) is false)
        {
            SendTutorial(player, nameof(prototype), "Item not found");
            return ValueTask.FromResult(false);
        }

        var item = storageItemFactory.Create(prototype, 1);

        var result = storageManager.TryAdd(player, item, 1);

        if (result.State is false)
        {
            SendTutorial(player, nameof(prototype), "No space");
            return ValueTask.FromResult(false);
        }

        session.Send(ItemCreateResponse.Create(World, result.Storage, result.Info.Select(e => e.Slot).ToArray()));

        return ValueTask.FromResult(true);

    }
}