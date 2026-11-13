using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Responses;

namespace OpenWorker.Commands;

/// <summary>
/// Gives the account of the player the currency of the cash shop, or takes it with a negative amount: <c>/cash amount</c>.
/// The only way the balance grows.
/// </summary>
[CommandTrigger("cash")]
[CommandDescription("Give your account the currency of the cash shop")]
internal sealed partial class CashCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private ICashShopStore Store { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!long.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount) ||
            Math.Abs(amount) > InventoryService.CashLimit)
        {
            return false;
        }

        var balance = await Store.GiveAsync(World.Get<ClaimsComponent>(player).Account, amount, InventoryService.CashLimit);

        if (balance is not { } total)
        {
            return false;
        }

        if (World.IsAlive(player))
        {
            World.Get<ServerSessionComponent>(player).Send(new ItemUpdateCashResponse { Total = (int)total });
        }

        return true;
    }
}
