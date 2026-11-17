using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Makes items at an NPC by a recipe of the client's <c>tb_Make</c>: one request makes one item, which costs the gold, the
/// battle points and the four materials of the recipe.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request and changes nothing.</remarks>
public sealed class ItemMakeService(
    InventoryService inventory,
    ReadOnlyCollection<MakeRow> recipes,
    ItemSetupAnswers answers,
    QuestManager? quests = null,
    NpcCreditManager? credit = null,
    AchievementManager? achievements = null)
{
    /// <summary>The person lacks the materials.</summary>
    public const ushort NotEnoughMaterials = 52254;

    /// <summary>The bag has no room for what is made.</summary>
    public const ushort NoRoom = 52256;

    /// <summary>The table has no such recipe.</summary>
    public const ushort WrongRecipe = 52257;

    /// <summary>The person has less gold or fewer battle points than the recipe costs.</summary>
    public const ushort NotEnoughMoney = 52310;

    private readonly RowIndex<int, MakeRow> _recipes = recipes.IndexBy(e => e.Id);

    /// <summary>
    /// The group of a recipe: an NPC makes the recipes of the group its production names. 0 for a recipe the table lacks.
    /// </summary>
    public int GroupOf(int index) => _recipes.TryGet(index, out var row) ? row.Group : 0;

    /// <summary>
    /// Makes one item of a recipe at the NPC <paramref name="npc"/>. The first material is taken from the slot
    /// <paramref name="picked"/> the person chose in the dialog first, then from the other stacks of the bags. The credit
    /// with the group of the NPC takes a part off the gold and off the materials past the first, and the making raises that
    /// credit.
    /// </summary>
    public void Make(Entity player, int index, StorageSlot picked, ServiceNpc? npc = null)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Make;

        if (!_recipes.TryGet(index, out var row) || !inventory.Catalog.Exists(row.Product))
        {
            answers.Refuse(player, command, WrongRecipe);
            return;
        }

        var gold = credit is not null && npc is { } at ? credit.Discount(player, at, CreditBenefit.MakeMoney, row.Gold) : row.Gold;
        var saved = credit is not null && npc is { } near ? credit.Benefit(player, near, CreditBenefit.MakeMaterials) : 0;
        var battlePoints = (long)row.BattlePoint;

        if (gold < 0 || battlePoints < 0 || inventory.Gold(player) < gold || inventory.Balance(player, ShopCurrency.Bp) < battlePoints)
        {
            answers.Refuse(player, command, NotEnoughMoney);
            return;
        }

        // A material may come back in several places of a recipe: what one request takes of it is the sum.
        var materials = new Dictionary<int, int>();

        var recipe = row.Materials;

        for (var place = 0; place < recipe.Length; place++)
        {
            var (material, each) = recipe[place];

            if (material == 0 || each <= 0)
            {
                continue;
            }

            int needed = each;

            // The credit spares a part of every material but the first, one being left at the least.
            if (place > 0 && saved > 0)
            {
                needed = Math.Max(1, needed - (int)(needed * (double)saved / NpcCreditManager.Parts));
            }

            materials[material] = materials.GetValueOrDefault(material) + needed;
        }

        // The picked slot must hold the first material in a bag a cost is paid from.
        var first = row.Material1 != 0 && row.MaterialCount1 > 0
            ? inventory.InventoryOf(player)?.At(picked.Storage, picked.Slot)
            : null;
        var pickedWrong = row.Material1 != 0 && row.MaterialCount1 > 0 &&
            (first is not { Count: > 0 } || first.Item != row.Material1 || !InventoryService.PaysFrom(first.Storage));

        if (pickedWrong || materials.Any(e => inventory.CountOf(player, e.Key) < e.Value))
        {
            answers.Refuse(player, command, NotEnoughMaterials);
            return;
        }

        // The product needs no slot the materials leave, so it is given first: a refused gift takes nothing.
        if (!inventory.TryGive(player, row.Product, 1))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        foreach (var (material, needed) in materials)
        {
            var rest = needed;

            if (first is not null && material == first.Item)
            {
                var fromPicked = Math.Min(rest, (int)first.Count);

                inventory.Spend(player, first, fromPicked);
                rest -= fromPicked;
            }

            if (rest > 0)
            {
                inventory.TryTake(player, material, rest);
            }
        }

        if (gold > 0)
        {
            inventory.TryAddGold(player, -gold);
        }

        if (battlePoints > 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, -battlePoints);
        }

        answers.Send(player, new ItemSetupMakeResponse());

        quests?.OnEvent(player, QuestConditionType.Make, QuestConditionTarget.Item, row.Product, 1);
        achievements?.Count(player, AchievementType.ItemMade);

        if (credit is not null && npc is { } served)
        {
            credit.Raise(player, served, CreditRaise.Make);
        }
    }
}
