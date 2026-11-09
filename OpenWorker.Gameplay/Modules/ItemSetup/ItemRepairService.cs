using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Gameplay.Combat;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Messages.Abstractions;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Repairs worn-down items for gold, at an NPC or with a repair kit. The price of one item is the product of three rows of
/// <c>tb_Item_Repair</c>: the row of the level of the item, the row of its endurance and the row of its rank, each read in the
/// columns of its kind (weapon, sub weapon, gear) and of the way it is repaired (at an NPC or with a kit).
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request with the error of the client, so its dialog never waits.</remarks>
public sealed class ItemRepairService(
    World world,
    InventoryService inventory,
    ReadOnlyCollection<ItemRepairRow> repairs,
    NpcCreditManager? credit = null,
    PlayerStatsService? stats = null)
{
    /// <summary>The repair failed: no such item, nothing to repair, a table without a row for it.</summary>
    public const ushort Failed = 52450;

    /// <summary>The item is not worn down.</summary>
    public const ushort AlreadyRepaired = 52452;

    /// <summary>The person has less gold than the repair costs.</summary>
    public const ushort NotEnoughGold = 52453;

    /// <summary>A slot that cannot hold what the request says.</summary>
    public const ushort WrongSlot = 52012;

    private const byte WeaponSlot = 1;
    private const byte SubWeaponSlot = 2;

    private readonly RowIndex<int, ItemRepairRow> _repairs = repairs.IndexBy(e => e.Id);

    /// <summary>
    /// How much repairing the item costs, or <c>null</c> when it cannot or need not be repaired. A repair one item at a time
    /// is refused at less than one gold; among several, such an item is repaired with the others.
    /// </summary>
    public long? Price(ItemInstance item, bool withKit)
    {
        if (item.Count <= 0 || !inventory.Catalog.TryGet(item.Item, out var row) || row.Endurance == 0 || item.Endurance >= row.Endurance)
        {
            return null;
        }

        if (inventory.Catalog.Classify(item.Item) is not { RepairType: not 0 } kind)
        {
            return null;
        }

        if (!_repairs.TryGet(row.Level, out var byLevel) || !_repairs.TryGet(item.Endurance, out var byEndurance) ||
            !_repairs.TryGet(row.Rank, out var byRank))
        {
            return null;
        }

        var price = (kind.SlotType, withKit) switch
        {
            (WeaponSlot, false) => byLevel.WeaponByLevel * byEndurance.WeaponByEndurance * byRank.WeaponByRank,
            (WeaponSlot, true) => byLevel.WeaponKitByLevel * byEndurance.WeaponKitByEndurance * byRank.WeaponKitByRank,
            (SubWeaponSlot, false) => byLevel.SubWeaponByLevel * byEndurance.SubWeaponByEndurance * byRank.SubWeaponByRank,
            (SubWeaponSlot, true) => byLevel.SubWeaponKitByLevel * byEndurance.SubWeaponKitByEndurance * byRank.SubWeaponKitByRank,
            (_, false) => byLevel.GearByLevel * byEndurance.GearByEndurance * byRank.GearByRank,
            (_, true) => byLevel.GearKitByLevel * byEndurance.GearKitByEndurance * byRank.GearKitByRank
        };

        return float.IsFinite(price) && price > 0 ? (long)price : 0;
    }

    /// <summary>
    /// Repairs the item in one slot at an NPC, for less with credit at its group.
    /// </summary>
    public void RepairOne(Entity player, StorageSlot target, ServiceNpc? npc = null)
    {
        var opcode = Opcode(ItemSetupOpcode.RepairNpc);

        if (inventory.InventoryOf(player) is not { } bag || bag.At(target.Storage, target.Slot) is not { } item)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        if (Price(item, withKit: false) is not { } price)
        {
            Refuse(player, opcode, IsWornDown(item) ? Failed : AlreadyRepaired);
            return;
        }

        if (price < 1)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        price = Discount(player, npc, price);

        if (!inventory.TryAddGold(player, -price))
        {
            Refuse(player, opcode, NotEnoughGold);
            return;
        }

        Restore(bag, item);
        Send(player, new ItemSetupRepairNpcResponse { Item = InventoryService.ToStorageValue(item), Spent = price });
        Raise(player, npc);
    }

    /// <summary>
    /// Repairs at an NPC every worn item, and with <paramref name="carried"/> every item of the bag as well.
    /// </summary>
    public void RepairMany(Entity player, bool carried, ServiceNpc? npc = null)
    {
        var opcode = Opcode(carried ? ItemSetupOpcode.RepairAll : ItemSetupOpcode.RepairEquip);

        if (inventory.InventoryOf(player) is not { } bag)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        var storages = carried ? new[] { StorageGroup.Common, StorageGroup.AbilityEquip } : [StorageGroup.AbilityEquip];
        var items = new List<ItemInstance>();
        var total = 0L;

        foreach (var storage in storages)
        {
            if (bag.Find(storage) is not { } found)
            {
                continue;
            }

            foreach (var item in found.Slots)
            {
                if (item is not null && Price(item, withKit: false) is { } price)
                {
                    items.Add(item);
                    total += price;
                }
            }
        }

        if (items.Count == 0 || total < 1)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        total = Discount(player, npc, total);

        if (!inventory.TryAddGold(player, -total))
        {
            Refuse(player, opcode, NotEnoughGold);
            return;
        }

        var values = new List<StorageItemValue>(items.Count);

        foreach (var item in items)
        {
            Restore(bag, item);
            values.Add(InventoryService.ToStorageValue(item));
        }

        if (carried)
        {
            Send(player, new ItemSetupRepairAllResponse { Items = values, Spent = total });
        }
        else
        {
            Send(player, new ItemSetupRepairEquipResponse { Items = values, Spent = total });
        }

        Raise(player, npc);
    }

    /// <summary>
    /// The price of a repair at an NPC, less what the credit with its group and the boosters of the person take off.
    /// </summary>
    private long Discount(Entity player, ServiceNpc? npc, long price)
    {
        return credit is not null && npc is { } at ? credit.Discount(player, at, CreditBenefit.RepairMoney, price) : price;
    }

    /// <summary>
    /// A repair at an NPC raises the credit with its group.
    /// </summary>
    private void Raise(Entity player, ServiceNpc? npc)
    {
        if (credit is not null && npc is { } at)
        {
            credit.Raise(player, at, CreditRaise.Repair);
        }
    }

    /// <summary>
    /// Repairs with a kit of the bag: one item, or every worn item for a kit that repairs the equipment. One kit is used up and
    /// the repair costs gold as well, at the price of a kit.
    /// </summary>
    public void RepairWithKit(Entity player, StorageSlot kitSlot, StorageSlot target)
    {
        var opcode = Opcode(ItemSetupOpcode.Repair);

        if (inventory.InventoryOf(player) is not { } bag)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        if (kitSlot.Storage != StorageGroup.Common || bag.At(kitSlot.Storage, kitSlot.Slot) is not { Count: > 0 } kit)
        {
            Refuse(player, opcode, WrongSlot);
            return;
        }

        var use = inventory.Catalog.Classify(kit.Item)?.ItemUseType;

        if (use == ItemUseType.REPAIR_EQUIP)
        {
            RepairEquippedWithKit(player, bag, kit, opcode);
            return;
        }

        if (use != ItemUseType.REPAIR)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        if (target.Storage is not (StorageGroup.Common or StorageGroup.AbilityEquip) || bag.At(target.Storage, target.Slot) is not { } item)
        {
            Refuse(player, opcode, WrongSlot);
            return;
        }

        if (Price(item, withKit: true) is not { } price)
        {
            Refuse(player, opcode, IsWornDown(item) ? Failed : AlreadyRepaired);
            return;
        }

        if (price < 1)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        if (!inventory.TryAddGold(player, -price))
        {
            Refuse(player, opcode, NotEnoughGold);
            return;
        }

        var kitItem = kit.Item;

        inventory.Spend(player, kit, 1);
        Restore(bag, item);
        Send(player, new ItemSetupRepairResponse { Item = InventoryService.ToStorageValue(item), Kit = kitItem, Spent = price });
    }

    private void RepairEquippedWithKit(Entity player, Inventory bag, ItemInstance kit, MessageOpcode opcode)
    {
        if (bag.Find(StorageGroup.AbilityEquip) is not { } worn)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        var items = new List<ItemInstance>();
        var total = 0L;

        foreach (var item in worn.Slots)
        {
            if (item is not null && Price(item, withKit: true) is { } price)
            {
                items.Add(item);
                total += price;
            }
        }

        if (items.Count == 0 || total < 1)
        {
            Refuse(player, opcode, Failed);
            return;
        }

        if (!inventory.TryAddGold(player, -total))
        {
            Refuse(player, opcode, NotEnoughGold);
            return;
        }

        inventory.Spend(player, kit, 1);

        var values = new List<StorageItemValue>(items.Count);

        foreach (var item in items)
        {
            Restore(bag, item);
            values.Add(InventoryService.ToStorageValue(item));
        }

        // The client hears a kit that repairs the equipment as a repair of the equipment.
        Send(player, new ItemSetupRepairEquipResponse { Items = values, Spent = total });
    }

    /// <summary>
    /// The item would need a repair but the tables price it at nothing: refused as a failure, not as an item in full repair.
    /// </summary>
    private bool IsWornDown(ItemInstance item)
    {
        return inventory.Catalog.TryGet(item.Item, out var row) && item.Endurance < row.Endurance;
    }

    private void Restore(Inventory bag, ItemInstance item)
    {
        inventory.Catalog.TryGet(item.Item, out var row);

        item.Endurance = row.Endurance;
        bag.Touch(item);
    }

    private static MessageOpcode Opcode(ItemSetupOpcode command) => MessageOpcode.From(GroupOpcode.ItemSetup, command);

    private void Refuse(Entity player, MessageOpcode opcode, ushort code)
    {
        Send(player, new ErrorMessageResponse(opcode, code));
    }

    private void Send<T>(Entity player, T message) where T : IResponseHotspotMessage
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(message);
        }

        // What a repair gives back to worn gear shows in the stats.
        stats?.Send(player);
    }
}
