using Arch.Core;
using Arch.Core.Extensions;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Commands;
using OpenWorker.Hotspot.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Responses;
using OpenWorker.Hotspot.Modules.Shop.Components;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Commands;

[StuffCommand("currency")]
public sealed class CurrencyCommand(World world) : AStuffCommand(world)
{
    protected override string GetTutorialMessage() => "currency (total) (type) [bonus] [earn]";

    public override ValueTask<bool> TryExecute(Entity player, IReadOnlyList<string> tokens)
    {
        var session = World.Get<ServerSessionComponent>(player);
        
        if (!long.TryParse(tokens.ElementAtOrDefault(0), out var total) || total < 0)
        {
            SendTutorial(player, nameof(total));
            return ValueTask.FromResult(false);
        }
        
        if (!Enum.TryParse<ShopCurrency>(tokens.ElementAtOrDefault(1), out var type))
        {
            SendTutorial(player, nameof(type));
            return ValueTask.FromResult(false);
        }

        _ = int.TryParse(tokens.ElementAtOrDefault(2), out var bonus);
        _ = Enum.TryParse<MoneyEarnFlow>(tokens.ElementAtOrDefault(3), out var earn);

        var currency = player.Get<CurrencyComponent>();
        currency[(int)type] = total;

        session.Send(new ItemUpdateInvenMoneyResponse(total, bonus, earn));
        return ValueTask.FromResult(true);
    }
}