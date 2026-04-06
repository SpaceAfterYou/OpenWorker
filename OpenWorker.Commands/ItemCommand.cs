using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Commands;

[CommandTrigger("item")]
[CommandDescription("Add item to player inventory")]
internal sealed partial class ItemCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private StorageItemFactory StorageItemFactory { get; }

    [ExternalDependency]
    private StorageManager StorageManager { get; }

    [ExternalDependency]
    private ReadOnlyCollection<ItemRow> ItemCollection { get; }

    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), out var prototype))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (ItemCollection.All(e => e.Id != prototype))
        {
            // TODO: Not found message
            return ValueTask.FromResult(false);
        }

        PrivateExecute(player, prototype);

        return ValueTask.FromResult(true);
    }

    private void PrivateExecute(Entity player, int prototype)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var item = StorageItemFactory.Create(prototype, 1);
        var result = StorageManager.TryAdd(player, item, 1);

        if (!result.State)
        {
            // TODO: No space message
            return;
        }

        session.Send(ItemDtoFactory.CreateItemCreateResponse(World, result.Storage,
            result.Info.Select(e => e.Slot).ToArray()));
    }
}
