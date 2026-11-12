using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Quests;
using OpenWorker.Hotspot.Modules.Quests.Enums;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Makes items at an NPC by a recipe of the client's <c>tb_Make</c>: every item made costs the gold, the battle points and
/// the four materials of the recipe, and gives one of its product.
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

    /// <summary>The table has no such recipe, or the count is not one the recipe can be made in.</summary>
    public const ushort WrongRecipe = 52257;

    /// <summary>The person has less gold or fewer battle points than the recipe costs.</summary>
    public const ushort NotEnoughMoney = 52310;

    /// <summary>The most items one request may make.</summary>
    public const int MaxCount = 100;

    /// <summary>The most of one material one request may use.</summary>
    public const int MaxMaterials = 32_000;

    private readonly RowIndex<int, MakeRow> _recipes = recipes.IndexBy(e => e.Id);

    /// <summary>
    /// The group of a recipe: an NPC makes the recipes of the group its production names. 0 for a recipe the table lacks.
    /// </summary>
    public int GroupOf(int index) => _recipes.TryGet(index, out var row) ? row.Group : 0;

    /// <summary>
    /// Makes <paramref name="count"/> items of a recipe at the NPC <paramref name="npc"/>: the credit with its group takes a
    /// part off the gold of an item and off the materials past the first, and the making raises that credit.
    /// </summary>
    public void Make(Entity player, int index, byte count, ServiceNpc? npc = null)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Make;

        if (!_recipes.TryGet(index, out var row) || count is 0 or > MaxCount || !inventory.Catalog.Exists(row.Product))
        {
            answers.Refuse(player, command, WrongRecipe);
            return;
        }

        var price = credit is not null && npc is { } at ? credit.Discount(player, at, CreditBenefit.MakeMoney, row.Gold) : row.Gold;
        var saved = credit is not null && npc is { } near ? credit.Benefit(player, near, CreditBenefit.MakeMaterials) : 0;
        var gold = price * count;
        var battlePoints = (long)row.BattlePoint * count;

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

            var needed = each * count;

            if (needed > MaxMaterials)
            {
                answers.Refuse(player, command, WrongRecipe);
                return;
            }

            // The credit spares a part of every material but the first, one being left at the least.
            if (place > 0 && saved > 0)
            {
                needed = Math.Max(1, needed - (int)(needed * (double)saved / NpcCreditManager.Parts));
            }

            materials[material] = materials.GetValueOrDefault(material) + needed;
        }

        if (materials.Any(e => inventory.CountOf(player, e.Key) < e.Value))
        {
            answers.Refuse(player, command, NotEnoughMaterials);
            return;
        }

        // The product needs no slot the materials leave, so it is given first: a refused gift takes nothing.
        if (!inventory.TryGive(player, row.Product, count))
        {
            answers.Refuse(player, command, NoRoom);
            return;
        }

        foreach (var (material, needed) in materials)
        {
            inventory.TryTake(player, material, needed);
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

        // Every item made counts for the quests that ask to make it, and the making raises the credit.
        quests?.OnEvent(player, QuestConditionType.Make, QuestConditionTarget.Item, row.Product, count);

        // One request counts once for the achievements, whatever the count.
        achievements?.Count(player, AchievementType.ItemMade);

        if (credit is not null && npc is { } served)
        {
            credit.Raise(player, served, CreditRaise.Make);
        }
    }
}
