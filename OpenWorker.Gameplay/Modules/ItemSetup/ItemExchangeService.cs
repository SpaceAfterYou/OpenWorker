using System.Collections.ObjectModel;
using Arch.Core;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Npcs;
using OpenWorker.Hotspot.Modules.ItemSetup.Responses;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.ItemSetup;

/// <summary>
/// Exchanges materials for another item at an NPC, by a row of <c>tb_Reinforce_Exchange</c>: so many of one item give so
/// many of another. The request names the row and how many items the person wants, which must be a whole number of
/// exchanges.
/// </summary>
/// <remarks>Runs on the world loop. Every refusal answers the request and changes nothing.</remarks>
public sealed class ItemExchangeService(
    InventoryService inventory,
    ReadOnlyCollection<ReinforceExchangeRow> exchanges,
    ItemSetupAnswers answers,
    NpcCreditManager? credit = null)
{
    /// <summary>The table has no such exchange.</summary>
    public const ushort NoExchange = 52261;

    /// <summary>The item the exchange gives does not exist.</summary>
    public const ushort NoProduct = 52262;

    /// <summary>The person lacks the materials, or the bag has no room for what the exchange gives.</summary>
    public const ushort Failed = 52264;

    /// <summary>The count is not a whole number of exchanges.</summary>
    public const ushort WrongCount = 52265;

    private readonly RowIndex<int, ReinforceExchangeRow> _exchanges = exchanges.IndexBy(e => e.Id);

    /// <summary>
    /// The kind of NPC that offers an exchange of the table, besides offering exchanges at all; none for a row the table lacks.
    /// </summary>
    public NpcFunction FunctionOf(int index) => _exchanges.TryGet(index, out var row) ? (NpcFunction)row.NpcFunction : NpcFunction.None;

    /// <summary>
    /// Exchanges at the NPC <paramref name="npc"/>, which raises the credit with its group.
    /// </summary>
    public void Exchange(Entity player, int index, byte count, ServiceNpc? npc = null)
    {
        const ItemSetupOpcode command = ItemSetupOpcode.Exchange;

        if (!_exchanges.TryGet(index, out var row))
        {
            answers.Refuse(player, command, NoExchange);
            return;
        }

        if (count == 0 || row.ProductCount == 0 || count % row.ProductCount != 0)
        {
            answers.Refuse(player, command, WrongCount);
            return;
        }

        if (!inventory.Catalog.Exists(row.Product))
        {
            answers.Refuse(player, command, NoProduct);
            return;
        }

        var materials = count / row.ProductCount * row.MaterialCount;

        if (row.Material == 0 || materials <= 0 || inventory.CountOf(player, row.Material) < materials ||
            !inventory.CanGiveAll(player, [(row.Product, count)]))
        {
            answers.Refuse(player, command, Failed);
            return;
        }

        inventory.TryTake(player, row.Material, materials);
        inventory.TryGive(player, row.Product, count);

        answers.Send(player, new ItemSetupExchangeResponse());

        if (credit is not null && npc is { } at)
        {
            credit.Raise(player, at, CreditRaise.Exchange);
        }
    }
}
