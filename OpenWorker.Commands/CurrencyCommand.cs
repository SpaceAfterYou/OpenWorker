using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Gameplay.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Commands;

[CommandTrigger("currency")]
[CommandDescription("Set player currency")]
internal sealed partial class CurrencyCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }
    
    ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!long.TryParse(tokens.ElementAtOrDefault(0), out var total) || total < 0)
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }
        
        if (!Enum.TryParse<ShopCurrency>(tokens.ElementAtOrDefault(1), out var type))
        {
            // TODO: Failed parser message
            return ValueTask.FromResult(false);
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(2), out var bonus);
        _ = Enum.TryParse<MoneyEarnFlow>(tokens.ElementAtOrDefault(3), out var earn);
        
        PrivateExecute(player, total, type, bonus, earn);
        
        return ValueTask.FromResult(true);
    }
    
    private void PrivateExecute(Entity player, long total, ShopCurrency type, int bonus, MoneyEarnFlow earn)
    {
        var session = World.Get<ServerSessionComponent>(player);
        var currency = player.Get<CurrencyComponent>();
        
        currency[unchecked((int)type)] = total;

        session.Send(new ItemUpdateInvenMoneyResponse { Total = total, Bonus = bonus, Type = earn });
    }
}