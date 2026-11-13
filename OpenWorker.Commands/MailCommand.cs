using System.Globalization;
using Arch.Core;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Post.Responses;

namespace OpenWorker.Commands;

/// <summary>
/// A letter of the game to the account of the player, as a game master sends one: <c>/mail item [count] [gold]</c>.
/// </summary>
[CommandTrigger("mail")]
[CommandDescription("Send a letter of the game with an item and gold to your account")]
internal sealed partial class MailCommand : ICommand
{
    [ExternalDependency]
    private World World { get; }

    [ExternalDependency]
    private InventoryService Inventory { get; }

    [ExternalDependency]
    private IPostStore Post { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        if (!int.TryParse(tokens.ElementAtOrDefault(0), NumberStyles.Integer, CultureInfo.InvariantCulture, out var item) ||
            !Inventory.Catalog.Exists(item))
        {
            return false;
        }

        var count = short.TryParse(tokens.ElementAtOrDefault(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var given) ? given : (short)1;
        var gold = long.TryParse(tokens.ElementAtOrDefault(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var money) ? money : 0;

        if (count is < 1 or > 999 || gold < 0)
        {
            return false;
        }

        var account = World.Get<ClaimsComponent>(player).Account;

        var letter = await Post.SendToAccountAsync(new PostAccountDraft(
            account,
            "GM",
            "Mail",
            "A letter of the game master.",
            gold,
            [new PostItemPersistent { Item = item, Count = count, Endurance = 100 }]));

        var (_, unread) = await Post.CountUnreadAsync(0, account);

        if (World.IsAlive(player))
        {
            World.Get<ServerSessionComponent>(player).Send(new PostAccountRecvResponse
            {
                PostCount = (short)Math.Min(unread, short.MaxValue),
                Value = PostLetters.Account(letter)
            });
        }

        return true;
    }
}
