using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Helpers.Components;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Persons.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Helpers.Requests;
using OpenWorker.Hotspot.Modules.Helpers.Responses;
using OpenWorker.Hotspot.Modules.Helpers.Types;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.System.Responses;

namespace OpenWorker.Gameplay.Modules.Helpers;

/// <summary>
/// Which helpers of a person are in the maze with it now. A called helper is told to the client as called, fights with what
/// it wears at every change of its items or its orb, and gives back the orb of a friend when the maze is cleared; helpers
/// that are not called keep theirs.
/// </summary>
public interface ISummonedHelpers
{
    bool IsSummoned(Entity player, int helper);

    /// <summary>
    /// What a called helper wears has changed: the helper in the maze fights from now on with what this record says it has.
    /// Nothing for a helper that is not called.
    /// </summary>
    void Refit(Entity player, in HelperValue record);
}

/// <summary>
/// No helper is called into a maze.
/// </summary>
public sealed class NoSummonedHelpers : ISummonedHelpers
{
    public bool IsSummoned(Entity player, int helper) => false;

    public void Refit(Entity player, in HelperValue record)
    {
    }
}

/// <summary>
/// What the helpers of a person wear: three items each, kept in the inventory of the person in a storage of their own and
/// moved only through <see cref="InventoryService"/>, and the orb of a friend, kept on the helper in the book. Both add to the
/// stats of the helper (<see cref="HelperStats"/>). Items are put on and taken off anywhere, in a district or in a maze; a
/// helper that stands in the maze then fights with what it wears now, so an item taken off it is no longer in its blows.
/// </summary>
public sealed class HelperGear(
    World world,
    HelperCatalog catalog,
    InventoryService inventory,
    HelperStats stats,
    ISummonedHelpers? called = null,
    TimeProvider? time = null)
{
    /// <summary>
    /// The item cannot be put on the helper, or nothing is worn there to take off.
    /// </summary>
    public const ushort CannotEquip = 58004;

    /// <summary>
    /// The bag has no free slot for the item taken off.
    /// </summary>
    public const ushort NoRoom = 52010;

    // The slot types of the items a helper wears: a weapon, a gear and a third kind no item uses. The slot of the helper is
    // the slot type less the first.
    private const int FirstHelperSlotType = 240;

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    /// <summary>
    /// The helper as the client reads it: what it wears, the orb of a friend while it counts, whether it stands in the maze
    /// now (unless the caller says), what it has by itself at the level of its owner and what the items and the orb add.
    /// </summary>
    public HelperValue Record(Entity player, HelperEntry entry, bool? summoned = null)
    {
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : (byte)1;
        var worn = Worn(player, entry.Helper);
        var orb = OrbOf(entry);

        return new HelperValue
        {
            Helper = entry.Helper,
            Items = worn.Select(e => e?.ToValue() ?? HelperValue.NoItem).ToArray(),
            Support = orb,
            Summoned = summoned ?? called?.IsSummoned(player, entry.Helper) == true,
            Order = entry.Order,
            Origin = stats.Origin(entry.Helper, level),
            Additional = stats.Additional(worn.OfType<ItemInstance>().Select(e => e.Item), orb)
        };
    }

    /// <summary>
    /// The items a helper of the person wears, by slot.
    /// </summary>
    public ItemInstance?[] Worn(Entity player, int helper)
    {
        var worn = new ItemInstance?[Inventory.HelperItemSlots];

        if (!catalog.TryGetNumber(helper, out var number))
        {
            return worn;
        }

        for (var slot = 0; slot < worn.Length; slot++)
        {
            var place = Inventory.HelperPlace(number, slot);

            worn[slot] = place < 0 ? null : inventory.WornByHelper(player, place);
        }

        return worn;
    }

    /// <summary>
    /// Puts an item of the bag on a helper, swaps it for the item worn in its slot, or takes the worn item off, as the
    /// request names the serials: both for a swap, the bag's alone to put on, the helper's alone to take off. The checks and
    /// the codes are the original's; the level of the item is checked too, which the client does on its side.
    /// </summary>
    public void Equip(Entity player, in HelperEquipRequest request)
    {
        if (!TryGetEntry(player, request.Helper, out var entry) || !catalog.TryGetNumber(entry.Helper, out var number))
        {
            Refuse(player, CannotEquip);
            return;
        }

        var code = (request.InventorySerial != 0, request.HelperSerial != 0) switch
        {
            (true, true) => Swap(player, request, number),
            (true, false) => PutOn(player, request, number),
            (false, true) => TakeOff(player, request, number),
            _ => CannotEquip
        };

        if (code != 0)
        {
            Refuse(player, code);
            return;
        }

        world.Get<ServerSessionComponent>(player).Send(new HelperEquipResponse { Result = 0, Helper = Refit(player, entry) });
    }

    private ushort Swap(Entity player, in HelperEquipRequest request, int number)
    {
        var place = Inventory.HelperPlace(number, request.HelperSlot);

        if (place < 0 || inventory.WornByHelper(player, place) is not { } worn || worn.Serial != request.HelperSerial ||
            FindInBag(player, request) is not { } item || SlotFor(player, item, number) != request.HelperSlot)
        {
            return CannotEquip;
        }

        return inventory.PutOnHelper(player, item, place, swap: true) ? (ushort)0 : CannotEquip;
    }

    private ushort PutOn(Entity player, in HelperEquipRequest request, int number)
    {
        if (FindInBag(player, request) is not { } item || SlotFor(player, item, number) is not { } slot)
        {
            return CannotEquip;
        }

        var place = Inventory.HelperPlace(number, slot);

        return place >= 0 && inventory.WornByHelper(player, place) is null && inventory.PutOnHelper(player, item, place, swap: false)
            ? (ushort)0
            : CannotEquip;
    }

    private ushort TakeOff(Entity player, in HelperEquipRequest request, int number)
    {
        var place = Inventory.HelperPlace(number, request.HelperSlot);

        if (place < 0 || inventory.WornByHelper(player, place) is not { } worn || worn.Serial != request.HelperSerial)
        {
            return CannotEquip;
        }

        return inventory.TakeOffHelper(player, place, (StorageGroup)request.InventoryType, request.InventorySlot) ? (ushort)0 : NoRoom;
    }

    /// <summary>
    /// The item the request names in a bag of its own kind, if the slot holds the serial it names.
    /// </summary>
    private ItemInstance? FindInBag(Entity player, in HelperEquipRequest request)
    {
        var group = (StorageGroup)request.InventoryType;
        var item = inventory.InventoryOf(player)?.At(group, request.InventorySlot);

        return item is not null && item.Serial == request.InventorySerial && inventory.Catalog.HomeOf(item.Item) == group ? item : null;
    }

    /// <summary>
    /// The slot of the helper the item goes to: it must be a helper item of the helper's number, of a level the owner has
    /// reached.
    /// </summary>
    private short? SlotFor(Entity player, ItemInstance item, int number)
    {
        if (inventory.Catalog.Classify(item.Item) is not { } classify || !inventory.Catalog.TryGet(item.Item, out var row))
        {
            return null;
        }

        var slot = classify.SlotType - FirstHelperSlotType;
        var level = world.TryGet<LevelComponent>(player, out var reached) ? reached.Level : 1;

        return slot is >= 0 and < Inventory.HelperItemSlots && row.ClassLimit == number && row.LevelLimit <= level ? (short)slot : null;
    }

    /// <summary>
    /// The orb of a friend goes on a helper of the person, in place of any it wore. The book is written by the flush.
    /// </summary>
    public bool WearOrb(Entity player, int helper, HelperOrb orb, out HelperValue record)
    {
        record = default;

        if (!TryGetEntry(player, helper, out var entry))
        {
            return false;
        }

        entry.Orb = orb;
        world.MarkDirty(player, PersonSections.MiscState);
        record = Refit(player, entry);

        return true;
    }

    /// <summary>
    /// The orb of a friend leaves a helper: when the helper is sent away from the maze, and when a maze is cleared. The client
    /// is told with the helper as it is now, and says that a new orb can be worn. Nothing happens to a helper without one.
    /// </summary>
    public void ReleaseOrb(Entity player, int helper)
    {
        if (!TryGetEntry(player, helper, out var entry) || entry.Orb is null)
        {
            return;
        }

        entry.Orb = null;
        world.MarkDirty(player, PersonSections.MiscState);

        world.Get<ServerSessionComponent>(player).Send(new HelperUpdateInfoResponse { Helper = Refit(player, entry) });
    }

    /// <summary>
    /// The person has cleared a maze: the helpers it called there give back the orbs of friends they wore.
    /// </summary>
    public void OnMazeCleared(Entity player)
    {
        if (called is null || !world.IsAlive(player) || !world.TryGet<HelperComponent>(player, out var component) || component.Book is null)
        {
            return;
        }

        foreach (var entry in component.Book.Helpers.Where(e => e.Orb is not null && called.IsSummoned(player, e.Helper)).ToArray())
        {
            ReleaseOrb(player, entry.Helper);
        }
    }

    /// <summary>
    /// The helper as it is now, after a change of what it wears; a helper that stands in the maze fights with it from now on.
    /// </summary>
    private HelperValue Refit(Entity player, HelperEntry entry)
    {
        var record = Record(player, entry);

        if (record.Summoned)
        {
            called?.Refit(player, record);
        }

        return record;
    }

    /// <summary>
    /// The orb a helper wears as the record carries it, or none when it has none or its day is over.
    /// </summary>
    private HelperSupportInfoValue OrbOf(HelperEntry entry)
    {
        return entry.Orb is { } orb && orb.Friend != 0 && orb.Until > _time.GetUtcNow()
            ? new HelperSupportInfoValue(orb.Friend, orb.SupportType, orb.Value, orb.Until, 0, false)
            : default;
    }

    private bool TryGetEntry(Entity player, int helper, out HelperEntry entry)
    {
        entry = null!;

        if (!world.IsAlive(player) || !world.TryGet<HelperComponent>(player, out var component) || component.Book?.Find(helper) is not { } found)
        {
            return false;
        }

        entry = found;

        return true;
    }

    private void Refuse(Entity player, ushort code)
    {
        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player)
                .Send(new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Helper, HelperOpcode.Equip), code));
        }
    }
}
