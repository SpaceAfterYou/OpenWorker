using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.ItemSetup.Types;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// The sockets of an item: opening the next one and setting a gem into an open one. What an item may have is its row of
/// <c>tb_Socket</c> (through the socket column of <c>tb_Item</c>): how many sockets, and what opening one costs.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request, so the dialog of the client never waits.</remarks>
public sealed class ItemSocketService(InventoryService inventory, ReadOnlyCollection<SocketRow> sockets, ItemSetupAnswers answers)
{
    /// <summary>The gem cannot go there: wrong item, wrong storage, a socket that is not open or not empty.</summary>
    public const ushort EquipFailed = 52280;

    /// <summary>The gem is of a higher level than the item.</summary>
    public const ushort GemLevelTooHigh = 52282;

    /// <summary>The socket is not open, or already holds a gem.</summary>
    public const ushort SocketNotOpen = 52283;

    /// <summary>The person has less gold than setting the gem costs.</summary>
    public const ushort EquipGoldShort = 52284;

    /// <summary>The item has no sockets to open.</summary>
    public const ushort ActiveFailed = 52286;

    /// <summary>The person has less gold than opening the socket costs.</summary>
    public const ushort ActiveGoldShort = 52288;

    /// <summary>The socket cannot be opened: not the next one, or past the last.</summary>
    public const ushort SocketNotNext = 52289;

    /// <summary>The person lacks the item that opens a socket.</summary>
    public const ushort ActiveItemShort = 52290;

    private readonly RowIndex<int, SocketRow> _sockets = sockets.IndexBy(e => e.Id);

    /// <summary>
    /// Opens the next socket of an item, for the gold and the item of its row.
    /// </summary>
    public void Activate(Entity player, StorageSlot targetSlot, byte place)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.SocketActive;

        if (inventory.InventoryOf(player) is not { } bag || bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (RowOf(target) is not { } row)
        {
            answers.Refuse(player, command, ActiveFailed);
            return;
        }

        if (place != target.SocketActive || place >= row.MaxSockets)
        {
            answers.Refuse(player, command, SocketNotNext);
            return;
        }

        if (inventory.Gold(player) < row.AddCost)
        {
            answers.Refuse(player, command, ActiveGoldShort);
            return;
        }

        if (row.AddItem != 0 && row.AddCount > 0 && inventory.CountOf(player, row.AddItem) < row.AddCount)
        {
            answers.Refuse(player, command, ActiveItemShort);
            return;
        }

        if (row.AddItem != 0 && row.AddCount > 0)
        {
            inventory.TryTake(player, row.AddItem, row.AddCount);
        }

        if (row.AddCost > 0)
        {
            inventory.TryAddGold(player, -row.AddCost);
        }

        target.SocketActive++;
        bag.Touch(target);

        answers.Send(player, new ItemSetupSocketActiveResponse { Item = InventoryService.ToStorageValue(target), Socket = place });
    }

    /// <summary>
    /// Sets a gem of the bag into an open, empty socket of an item. The gem is used up, gives the item its options, and binds it.
    /// </summary>
    public void Equip(Entity player, StorageSlot targetSlot, IReadOnlyList<ItemSetupSocketGem> gems)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.SocketEquip;

        // The client sets one gem at a time.
        if (gems is not { Count: 1 } || inventory.InventoryOf(player) is not { } bag ||
            bag.At(targetSlot.Storage, targetSlot.Slot) is not { } target ||
            target.Flag == ItemRestoreService.BrokenFlag ||
            !inventory.Catalog.TryGet(target.Item, out var targetRow) ||
            RowOf(target) is not { } row)
        {
            answers.Refuse(player, command, EquipFailed);
            return;
        }

        var request = gems[0];

        if (request.Gem.Storage != StorageGroup.Common || bag.At(request.Gem.Storage, request.Gem.Slot) is not { Count: > 0 } gem ||
            inventory.Catalog.Classify(gem.Item)?.ItemUseType != ItemUseType.ITEM_SOCKET_EQUIP ||
            !inventory.Catalog.TryGet(gem.Item, out var gemRow))
        {
            answers.Refuse(player, command, EquipFailed);
            return;
        }

        if (gemRow.Level > targetRow.Level)
        {
            answers.Refuse(player, command, GemLevelTooHigh);
            return;
        }

        if (request.Socket >= target.SocketActive || !target.SocketAt(request.Socket).IsEmpty)
        {
            answers.Refuse(player, command, SocketNotOpen);
            return;
        }

        if (inventory.Gold(player) < row.EquipCost)
        {
            answers.Refuse(player, command, EquipGoldShort);
            return;
        }

        if (row.EquipCost > 0)
        {
            inventory.TryAddGold(player, -row.EquipCost);
        }

        target.SetSocket(request.Socket, new ItemSocket(gem.Item, (ItemOption[])gem.Options.Clone()));

        // A gem binds an item that is free; a bound one stays as it is.
        var before = target.BindState;

        target.BindState = ItemBinding.OnGemSet(before);

        var bound = target.BindState != before;

        bag.Touch(target);
        inventory.Spend(player, gem, 1);

        answers.Send(player, new ItemSetupSocketEquipResponse
        {
            Sockets = target.SocketValues().Where(e => e.bySocketPos == request.Socket).ToArray()
        });

        if (bound)
        {
            answers.Send(player, new ItemUpdateResponse { List = [InventoryService.ToStorageValue(target)] });
        }
    }

    private SocketRow? RowOf(ItemInstance item)
    {
        return inventory.Catalog.TryGet(item.Item, out var row) && row.Socket != 0 && _sockets.TryGet(row.Socket, out var socket)
            ? socket
            : null;
    }
}
