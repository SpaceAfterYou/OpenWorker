using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Boosters;
using OpenWorker.Gameplay.Modules.ItemSetup;
using OpenWorker.Gameplay.Modules.Maze;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Res.Types;
using OpenWorker.UpdateContent.Res.Rows;
using OpenWorker.Gameplay.Modules.Tower;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The answer to the use of an item: it was used, or the code of the error the client is told. A used item that has a motion
/// (a potion) is shown to everybody who sees the person.
/// </summary>
/// <param name="Error">Zero when the item was used.</param>
/// <param name="Item">The item that was used.</param>
/// <param name="Slot">The slot of the bag it was in.</param>
/// <param name="Motion">The use plays a motion and starts the cooldown in the bag of the client.</param>
public readonly record struct ItemUseResult(ushort Error, int Item = 0, short Slot = 0, bool Motion = false)
{
    public bool Ok => Error == 0;

    public static ItemUseResult Used(int item = 0, short slot = 0, bool motion = false) => new(0, item, slot, motion);

    public static ItemUseResult Refused(ushort code = ItemUseCodes.NotUsable) => new(code);
}

/// <summary>
/// The codes the client is told when the use of an item is refused, as the original server gives them.
/// </summary>
public static class ItemUseCodes
{
    /// <summary>The item is not there, not usable, or not usable now: the code for everything that has no other.</summary>
    public const ushort NotUsable = InventoryService.Refused;

    /// <summary>The item is for another class.</summary>
    public const ushort OtherClass = 52009;

    /// <summary>The bags have no room for what an opened box gives.</summary>
    public const ushort BagFull = 52010;

    /// <summary>A fallen person cannot use an item.</summary>
    public const ushort Fallen = 52026;

    /// <summary>The person has not the level the item asks for.</summary>
    public const ushort LevelTooLow = 52601;

    /// <summary>The item (its group) is still cooling down.</summary>
    public const ushort Cooling = 52602;

    /// <summary>An item that only gets a person up is used where nobody can get up.</summary>
    public const ushort NoRevive = 52607;

    /// <summary>The kind of item has been used as often as a day allows.</summary>
    public const ushort DailyLimit = 52610;

    /// <summary>The infinite tower takes no item.</summary>
    public const ushort InfiniteTower = 52612;

    /// <summary>The item of a tower's ticket is not usable.</summary>
    public const ushort NoTowerTicket = 52613;

    /// <summary>The item cannot be used in this kind of place.</summary>
    public const ushort WrongPlace = 52616;
}

/// <summary>
/// What happens when an item is used (08/11): the checks every item goes through, in the order of the original server, then
/// what its kind does. Boxes and packages open into items and money, an akashic record card goes into the book, an item of
/// fatigue fills the fatigue of the day, an item of a title gives the title, a potion gives its buff in a maze, a booster its
/// booster. Every kind that is used starts the cooldown of the item's group.
/// </summary>
public sealed partial class ItemUseService(
    World world,
    InventoryService inventory,
    ItemCatalog catalog,
    ItemUseLimits limits,
    ReadOnlyCollection<ItemPackageRow> packages,
    ReadOnlyCollection<ItemRandomBoxRow> randomBoxes,
    ReadOnlyCollection<ItemClassrewardLinkRow> classRewards,
    IItemField? field = null,
    IItemTitles? titles = null,
    BoosterManager? boosters = null,
    AkashicService? akashic = null,
    FatigueService? fatigue = null,
    IReviveItems? revive = null,
    AchievementManager? achievements = null,
    InfiniteTower? tower = null,
    ReadOnlyCollection<ItemPackageUnpackRow>? unpacking = null,
    ReadOnlyCollection<ReinforceRow>? reinforces = null)
{
    // The group of the classify table that holds the items taken at a meal: potions and fatigue items.
    private const int ConsumableGroup = 17;

    // What a kind of item may do where, in the bits of the use state of its classify row.
    private const int UsableWhenFallen = 8;

    private static readonly StorageGroup[] Usable = [StorageGroup.Common, StorageGroup.Costume, StorageGroup.Cash, StorageGroup.Cube];

    private readonly IItemField _field = field ?? new DistrictItemField();

    /// <summary>
    /// Uses the item in a slot of a bag. The client names the slot and the serial of the item it believes is there.
    /// </summary>
    public async ValueTask<ItemUseResult> UseAsync(Entity player, StorageGroup group, short slot, long serial, CancellationToken cancellationToken)
    {
        var bag = inventory.InventoryOf(player);
        var item = bag?.At(group, slot);

        if (bag is null || item is null || item.Serial != serial || !Usable.Contains(group) ||
            !catalog.TryGet(item.Item, out var row) || catalog.Classify(item.Item) is not { } classify)
        {
            return ItemUseResult.Refused();
        }

        var place = _field.PlaceOf(player);

        // What every item asks, in this order.
        if (row.LevelLimit > world.Get<LevelComponent>(player).Level)
        {
            return ItemUseResult.Refused(ItemUseCodes.LevelTooLow);
        }

        if (row.ClassLimit != 0 && row.ClassLimit != (byte)world.Get<PersonInfoComponent>(player).Hero)
        {
            return ItemUseResult.Refused(ItemUseCodes.OtherClass);
        }

        if (place.Down && (classify.UseState & UsableWhenFallen) == 0)
        {
            return ItemUseResult.Refused(ItemUseCodes.Fallen);
        }

        if (limits.IsCooling(player, row))
        {
            return ItemUseResult.Refused(ItemUseCodes.Cooling);
        }

        if (item.Count < 1 || !place.ModeAllowsItems)
        {
            return ItemUseResult.Refused();
        }

        var result = classify.ItemUseType switch
        {
            ItemUseType.BOOSTER => await UseBoosterAsync(player, bag, item, row, cancellationToken),
            ItemUseType.TITLE => await UseTitleAsync(player, item, row),
            ItemUseType.AKASHIC_RECORD => UseAkashic(player, item, classify),
            ItemUseType.FP or ItemUseType.FP_FREE => UseFatigue(player, item, row, classify),
            ItemUseType.PACKAGE or ItemUseType.PACKAGE_COSTUME or ItemUseType.RANDOM_BOX or ItemUseType.BOX_CLASS =>
                UseBox(player, item, row, classify),
            ItemUseType.EFFECT => UseEffect(player, item, row, classify, place),
            ItemUseType.EQUIP_SLOT_OPEN => OpenEquipSlot(player, item, classify),
            ItemUseType.INFINITE_TOWER => UseTowerTicket(player, item),
            _ => ItemUseResult.Refused()
        };

        if (result.Ok)
        {
            limits.StartCooldown(player, row);
            achievements?.Count(player, AchievementType.ItemUsed);
        }

        return result;
    }

    /// <summary>
    /// The item in a slot that a service of its own takes: the person's request names the slot, not the use of an item (a
    /// skill reset, a new place in the deck). Null when there is no item of that use type in the slot.
    /// </summary>
    public ItemInstance? ItemOfService(Entity player, StorageGroup group, short slot, ItemUseType type)
    {
        var item = inventory.InventoryOf(player)?.At(group, slot);

        return item is { Count: >= 1 } && Usable.Contains(group) && catalog.Classify(item.Item) is { } classify && classify.ItemUseType == type
            ? item
            : null;
    }

    private void Tell(Entity player, ItemUseResponse answer)
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(answer);
        }
    }
}
