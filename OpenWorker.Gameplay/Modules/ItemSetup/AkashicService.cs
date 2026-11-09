using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Akashic record cards. A card is an item of the bag (use type <c>AKASHIC_RECORD</c>) with a row of <c>tb_Akashic_Records</c>
/// under the same id, and it carries experience. Cards fed to a card give it their experience up to what making the next card
/// needs; a full card, or a material item, makes the card of a row of <c>tb_Akashic_Make</c> at an NPC; using a card puts it
/// into the akashic record book of the person, one card per place of the book; and a card taken out of the book comes back
/// into the bag, as the row of <c>tb_Akashic_Disassemble</c> says and for its price, with the experience it went in with.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request, so the dialog of the client never waits.</remarks>
public sealed class AkashicService(
    InventoryService inventory,
    ReadOnlyCollection<AkashicRecordsRow> records,
    ReadOnlyCollection<AkashicMakeRow> makes,
    ReadOnlyCollection<AkashicDisassembleRow> disassembles,
    ItemSetupAnswers answers)
{
    /// <summary>The item does not fit the recipe: not its material, not full, wrong cards, nothing to gain.</summary>
    public const ushort WrongItem = 52002;

    /// <summary>The bag has no free slot for what comes out.</summary>
    public const ushort NoRoom = 52010;

    /// <summary>The person has less money than the work costs.</summary>
    public const ushort MoneyShort = 52437;

    /// <summary>The book already holds a card at the place of this one.</summary>
    public const ushort AlreadyRecorded = 6028;

    /// <summary>The kind of material of a recipe that is a card, which must be full; any other kind is a plain item.</summary>
    public const byte CardMaterial = 1;

    private static readonly ShopCurrency[] Costs = [ShopCurrency.Gold, ShopCurrency.Bp, ShopCurrency.Ether];

    private readonly RowIndex<int, AkashicRecordsRow> _records = records.IndexBy(e => e.Id);
    private readonly RowIndex<int, AkashicMakeRow> _makes = makes.IndexBy(e => e.Id);
    private readonly RowIndex<int, AkashicDisassembleRow> _disassembles = disassembles.IndexBy(e => e.Id);

    /// <summary>
    /// Makes the card of a recipe: its material (a full card, or a plain item), the card item of the recipe when it names one,
    /// the extra cards of the rarity of the material when it asks for them, and its gold. The client sends <c>AkashicMake</c>
    /// for a recipe without extra cards and <c>AkashicMakeEx</c> for one with them; both are answered by an empty
    /// <c>AkashicMake</c>.
    /// </summary>
    public void Make(Entity player, ItemSetupOpcode command, int index, StorageSlot materialSlot, IReadOnlyList<StorageSlot> extras)
    {
        if (inventory.InventoryOf(player) is not { } bag || CardAt(bag, materialSlot) is not { } material)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (!_makes.TryGet(index, out var recipe) || material.Item != recipe.Material || extras.Count != recipe.ExtraCount ||
            !_records.TryGet(recipe.Result, out _) || !inventory.Catalog.Exists(recipe.Result))
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        var isCard = _records.TryGet(material.Item, out var materialRow);

        if (recipe.MaterialKind == CardMaterial && (!isCard || materialRow.MakeExp <= 0 || material.Exp < materialRow.MakeExp))
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        var uses = new Dictionary<ItemInstance, int> { [material] = 1 };

        foreach (var slot in extras)
        {
            if (CardAt(bag, slot) is not { } extra)
            {
                answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
                return;
            }

            if (!_records.TryGet(extra.Item, out var extraRow) || (isCard && extraRow.RarePoint != materialRow.RarePoint))
            {
                answers.Refuse(player, command, WrongItem);
                return;
            }

            uses[extra] = uses.GetValueOrDefault(extra) + 1;
        }

        if (uses.Any(e => e.Key.Count < e.Value) || (recipe.Card != 0 && inventory.CountOf(player, recipe.Card) < 1))
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        if (inventory.Gold(player) < recipe.Gold)
        {
            answers.Refuse(player, command, MoneyShort);
            return;
        }

        if (!inventory.CanGiveAll(player, [(recipe.Result, 1)], ignoring: material))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        foreach (var (item, count) in uses)
        {
            inventory.Spend(player, item, count);
        }

        if (recipe.Card != 0)
        {
            inventory.TryTake(player, recipe.Card, 1);
        }

        inventory.TryAddGold(player, -recipe.Gold);
        inventory.TryGive(player, recipe.Result, 1);

        answers.Send(player, new ItemSetupAkashicMakeResponse());
    }

    /// <summary>
    /// Feeds cards to a card: each gives the experience of its row, and the card grows up to what making the next card needs.
    /// </summary>
    public void Compose(Entity player, StorageSlot targetSlot, IReadOnlyList<StorageSlot> materials)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.AkashicCompose;

        if (inventory.InventoryOf(player) is not { } bag || CardAt(bag, targetSlot) is not { Count: 1 } target)
        {
            answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
            return;
        }

        if (!_records.TryGet(target.Item, out var row) || row.MakeExp <= 0 || target.Exp >= row.MakeExp || materials.Count == 0)
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        var uses = new Dictionary<ItemInstance, int>();
        var gained = 0L;

        foreach (var slot in materials)
        {
            if (CardAt(bag, slot) is not { } material || material == target)
            {
                answers.Refuse(player, command, ItemSetupAnswers.WrongSlot);
                return;
            }

            if (!_records.TryGet(material.Item, out var materialRow))
            {
                answers.Refuse(player, command, WrongItem);
                return;
            }

            uses[material] = uses.GetValueOrDefault(material) + 1;
            gained += materialRow.ExpGiven;
        }

        if (gained <= 0 || uses.Any(e => e.Key.Count < e.Value))
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        foreach (var (item, count) in uses)
        {
            inventory.Spend(player, item, count);
        }

        target.Exp = (int)Math.Min(target.Exp + gained, row.MakeExp);
        bag.Touch(target);

        answers.Send(player, new ItemSetupAkashicComposeResponse { Serial = target.Serial, Exp = target.Exp });
    }

    /// <summary>
    /// Takes a card out of the akashic record book: the item of its row of <c>tb_Akashic_Disassemble</c> comes into the bag with
    /// the experience the card had, for the price of the row in gold, battle points or ether.
    /// </summary>
    public void Disassemble(Entity player, int card)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.AkashicDisassemble;

        if (inventory.InventoryOf(player) is not { } bag || !_records.TryGet(card, out var row) ||
            bag.AkashicRecords.At(row.Position) is not { } record || record.Card != card ||
            !_disassembles.TryGet(row.Disassemble, out var back) || !inventory.Catalog.Exists(back.Item) ||
            back.CostKind >= Costs.Length)
        {
            answers.Refuse(player, command, WrongItem);
            return;
        }

        var currency = Costs[back.CostKind];

        if (inventory.Balance(player, currency) < back.Cost)
        {
            answers.Refuse(player, command, MoneyShort);
            return;
        }

        // The card comes back with its own experience, so it never joins a stack.
        var storage = bag.Find(inventory.Catalog.HomeOf(back.Item));
        var free = storage?.FirstFree() ?? -1;

        if (storage is null || free < 0)
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        var item = inventory.Create(back.Item, 1);

        item.Exp = record.Exp;
        bag.Place(storage.Group, free, item);

        inventory.TryAddCurrency(player, currency, -back.Cost);

        bag.AkashicRecords.Records.Remove(record);
        bag.Dirty?.Mark(PersonSections.AkashicRecords);

        answers.Send(player, new ItemCreateResponse { List = [InventoryService.ToStorageValue(item)] });
        answers.Send(player, new ItemSetupAkashicDisassembleResponse
        {
            Result = 1,
            Record = new AkashicRecordValue { Card = record.Card, Position = record.Position }
        });
    }

    /// <summary>
    /// Puts a card the person uses into its akashic record book. A card whose place is taken stays in the bag, and the client
    /// is told so; false when the item is no card.
    /// </summary>
    public bool Register(Entity player, ItemInstance card)
    {
        if (inventory.InventoryOf(player) is not { } bag || card.Count < 1 || !_records.TryGet(card.Item, out var row))
        {
            return false;
        }

        if (bag.AkashicRecords.At(row.Position) is not null)
        {
            answers.Send(player, new ErrorMessageResponse(MessageOpcode.From(GroupOpcode.Item, ItemOpcode.Use), AlreadyRecorded));
            return true;
        }

        var record = new AkashicRecord { Card = card.Item, Position = row.Position, Exp = card.Exp };

        inventory.Spend(player, card, 1);

        bag.AkashicRecords.Records.Add(record);
        bag.Dirty?.Mark(PersonSections.AkashicRecords);

        answers.Send(player, new ItemAkashicRecordAddResponse
        {
            Record = new AkashicRecordValue { Card = record.Card, Position = record.Position }
        });

        return true;
    }

    /// <summary>
    /// The item at a slot of the bags a card may lie in.
    /// </summary>
    private static ItemInstance? CardAt(Inventory bag, StorageSlot slot)
    {
        return slot.Storage is StorageGroup.Common or StorageGroup.Cash ? bag.At(slot.Storage, slot.Slot) : null;
    }
}
