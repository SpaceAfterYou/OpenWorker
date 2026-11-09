using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.Domain.Components;
using OpenWorker.Gameplay.Combat;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.ItemSetup.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// The wear of the gear of a player in a fight, by the table of its hero: the weapon may lose endurance each time its blow
/// lands and when its carrier falls, the armour (head, shoulders, body, legs) each time a blow lands on its carrier and when its
/// carrier falls. Each piece rolls its own chance. A piece whose endurance is spent, that has none to lose, or that carries the
/// option that fixes it, does not wear. The player is told the new endurance of what wore (18/12), and the stats it loses
/// with it. Without the server's table nothing wears.
/// </summary>
public sealed class EnduranceWear
{
    private const int Chance = 10_000;

    /// <summary>The option of an item that keeps its endurance from falling.</summary>
    private const int FixedEndurance = 118;

    private const int StaticOptions = 5;

    private static readonly AbilityEquipSlot[] Weapon = [AbilityEquipSlot.SoulWeapon];

    private static readonly AbilityEquipSlot[] Armour =
        [AbilityEquipSlot.HeadGear, AbilityEquipSlot.ShoulderGear, AbilityEquipSlot.BodyGear, AbilityEquipSlot.LegGear];

    private readonly World _world;
    private readonly ItemCatalog _catalog;
    private readonly PlayerStatsService? _stats;
    private readonly RowIndex<int, ItemEnduranceRow> _rows;
    private readonly Func<int, int, int> _roll;

    /// <param name="roll">A number from the first bound to the second, both included; random unless a test fixes it.</param>
    public EnduranceWear(
        World world,
        ItemCatalog catalog,
        ReadOnlyCollection<ItemEnduranceRow>? rows = null,
        PlayerStatsService? stats = null,
        Func<int, int, int>? roll = null)
    {
        _world = world;
        _catalog = catalog;
        _stats = stats;
        _rows = (rows ?? new ReadOnlyCollection<ItemEnduranceRow>([])).IndexBy(e => e.Hero);
        _roll = roll ?? ((low, high) => Random.Shared.Next(low, high + 1));
    }

    /// <summary>
    /// A blow of the player has landed: its weapon may wear.
    /// </summary>
    public void Attacked(Entity player)
    {
        if (RowOf(player) is { } row)
        {
            Wear(player, (Weapon, row.WeaponAttackChance, row.WeaponAttackLoss));
        }
    }

    /// <summary>
    /// A blow has landed on the player: its armour may wear.
    /// </summary>
    public void Hit(Entity player)
    {
        if (RowOf(player) is { } row)
        {
            Wear(player, (Armour, row.GearDefenceChance, row.GearDefenceLoss));
        }
    }

    /// <summary>
    /// The player has fallen: its weapon and its armour may wear, by the rates of a death.
    /// </summary>
    public void Died(Entity player)
    {
        if (RowOf(player) is { } row)
        {
            Wear(player, (Weapon, row.WeaponDeathChance, row.WeaponDeathLoss), (Armour, row.GearDeathChance, row.GearDeathLoss));
        }
    }

    private ItemEnduranceRow? RowOf(Entity player)
    {
        return _world.IsAlive(player) && _world.TryGet<PersonInfoComponent>(player, out var person) && _rows.TryGet((int)person.Hero, out var row)
            ? row
            : null;
    }

    private void Wear(Entity player, params (AbilityEquipSlot[] Slots, ushort Chance, float Loss)[] rules)
    {
        if (!_world.TryGet<InventoryComponent>(player, out var component) || component.Value is not { } inventory)
        {
            return;
        }

        List<ItemSetupEnduranceEntry>? worn = null;

        foreach (var (slots, chance, loss) in rules)
        {
            foreach (var slot in slots)
            {
                var item = inventory.At(StorageGroup.AbilityEquip, (int)slot);

                if (item is null || item.Endurance == 0 || !_catalog.TryGet(item.Item, out var row) || row.Endurance == 0 || IsFixed(item, row))
                {
                    continue;
                }

                if (_roll(1, Chance) > chance)
                {
                    continue;
                }

                item.Endurance = (byte)Math.Clamp((int)(item.Endurance - loss), 0, byte.MaxValue);
                inventory.Touch(item);

                (worn ??= []).Add(new ItemSetupEnduranceEntry(item.Serial, item.Endurance));
            }
        }

        if (worn is null)
        {
            return;
        }

        if (_world.TryGet<ServerSessionComponent>(player, out var session))
        {
            session.Send(new ItemSetupEnduranceResponse { Items = worn });
        }

        _stats?.Send(player);
    }

    private static bool IsFixed(ItemInstance item, in ItemRow row)
    {
        for (var index = 0; index < StaticOptions; index++)
        {
            if (row.StatOption(index).Type == FixedEndurance)
            {
                return true;
            }
        }

        foreach (var option in item.Options)
        {
            if (option.Type == FixedEndurance)
            {
                return true;
            }
        }

        return false;
    }
}
