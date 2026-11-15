using System.Globalization;
using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Domain.Components;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Shop.Enums;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Hotspot.Modules.Chat.Enums;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.Commands;

/// <summary>
/// Sets a currency of the player: <c>/currency total kind [bonus] [flow]</c>. The client is told in the message of the kind;
/// a kind it has no message for is told at its next entry. The cash of the shop belongs to the account and is given with
/// <c>/cash</c>; this command refuses that kind.
/// </summary>
[CommandTrigger("currency")]
[CommandDescription("Set player currency")]
internal sealed partial class CurrencyCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!long.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.None, CultureInfo.InvariantCulture, out var total))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        if (!Enum.TryParse<ShopCurrency>(tokens.ElementAtOrDefault(1), out var type) || !Enum.IsDefined(type) ||
            type is ShopCurrency.None or ShopCurrency.Max || total > InventoryService.LimitOf(type))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        if (type == ShopCurrency.Cash)
        {
            World.Get<ServerSessionComponent>(player).Send(new ChatNormalResponse
            {
                Actor = World.Get<ActorComponent>(player),
                Appearance = ChatMessageAppearance.System,
                Message = "The cash of the shop belongs to the account: give it with /cash <amount>."
            });

            return ValueTask.FromResult(false);
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(2), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bonus);
        _ = Enum.TryParse<MoneyEarnFlow>(tokens.ElementAtOrDefault(3), out var earn);
        
        PrivateExecute(player, total, type, bonus, earn);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, long total, ShopCurrency type, int bonus, MoneyEarnFlow earn)
    {
        var currency = player.Get<CurrencyComponent>();

        currency[type] = total;
        World.MarkDirty(player, PersonSections.Currencies);

        if (!InventoryService.TellBalance(World, player, type, earn, bonus))
        {
            World.Get<ServerSessionComponent>(player).Send(new ChatNormalResponse
            {
                Actor = World.Get<ActorComponent>(player),
                Appearance = ChatMessageAppearance.System,
                Message = "The client has no message for this currency: it shows the new amount at the next entry."
            });
        }
    }
}
