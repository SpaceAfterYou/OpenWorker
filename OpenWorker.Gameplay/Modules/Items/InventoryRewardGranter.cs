using System.Globalization;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.Gameplay.Modules.Post;
using OpenWorker.Gameplay.Modules.Rewards;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Modules.Post;
using OpenWorker.Hotspot.Modules.Post.Responses;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// Gives the rewards of the features that hand out items: into the bags of the person, with its gold, ether and battle
/// points. Experience is not money: the feature that gives it raises the level through the experience of the person.
/// <para>
/// What the bags cannot hold depends on where the reward comes from. A quest, a soul metry, a cube and a helper refuse the
/// whole reward and keep it for later, as the original does. The reward of a daily mission always goes by letter; the
/// reward of a day of attendance and of the time played goes into the bags, and what does not fit goes by letter, with gold
/// the purse cannot hold. The letter is one of the game, to the account of the person (<see cref="IPostStore.SendToAccountAsync"/>),
/// written after the reward counts as given, the way the original sends it, so the feature that gives it waits for nothing:
/// a letter the database does not take is tried again for a while and then given up with an error that names it.
/// </para>
/// </summary>
public sealed partial class InventoryRewardGranter(
    InventoryService inventory,
    World world,
    IPostStore? post = null,
    TimeProvider? time = null,
    ILogger<InventoryRewardGranter>? logger = null) : IRewardGranter
{
    private const string Sender = "SoulWorker";

    /// <summary>
    /// How long a letter the database did not take waits before it is tried again; after the last wait it is given up.
    /// </summary>
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)];

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly ILogger _logger = logger ?? NullLogger<InventoryRewardGranter>.Instance;

    private enum Delivery : byte
    {
        /// <summary>All of it into the bags, or none of it.</summary>
        Bag,

        /// <summary>Into the bags what fits, the rest by letter.</summary>
        BagThenLetter,

        /// <summary>The items by letter, the money into the purse.</summary>
        Letter
    }

    private static Delivery DeliveryOf(RewardSource source) => source switch
    {
        RewardSource.DailyMission => Delivery.Letter,
        RewardSource.Attendance or RewardSource.PlayTime => Delivery.BagThenLetter,
        _ => Delivery.Bag
    };

    public ValueTask<bool> TryGrantAsync(Entity player, Reward reward, RewardSource source, CancellationToken cancellationToken)
    {
        var delivery = DeliveryOf(source);

        // Without a post, or for a person that has no account here, nothing can go by letter.
        if (delivery == Delivery.Bag || post is null || !world.IsAlive(player) || !world.TryGet<ClaimsComponent>(player, out var claims))
        {
            return ValueTask.FromResult(TryGrant(player, reward));
        }

        return ValueTask.FromResult(TryGrantOrMail(player, claims.Account, reward, source, delivery));
    }

    /// <summary>
    /// Gives all of the reward or none of it.
    /// </summary>
    public bool TryGrant(Entity player, Reward reward)
    {
        if (inventory.InventoryOf(player) is null ||
            !inventory.CanReceive(player, ShopCurrency.Gold, reward.Gold) ||
            !inventory.CanReceive(player, ShopCurrency.Ether, reward.Ether) ||
            !inventory.CanReceive(player, ShopCurrency.Bp, reward.BattlePoints))
        {
            return false;
        }

        var items = ItemsOf(reward);

        if (!inventory.CanGiveAll(player, items))
        {
            return false;
        }

        foreach (var (item, count) in items)
        {
            inventory.TryGive(player, item, count);
        }

        AddCurrencies(player, reward.Gold, reward);

        return true;
    }

    /// <summary>
    /// Gives what goes into the bags and sends the rest by letter. Everything is settled before anything is given, on the
    /// loop and without waiting, so the reward is given once: what the bags take is in them when this returns, and the
    /// letter is on its way. Refused, with nothing given, only for what no letter carries: an item the tables do not know,
    /// ether or battle points the purse cannot hold.
    /// </summary>
    private bool TryGrantOrMail(Entity player, int account, Reward reward, RewardSource source, Delivery delivery)
    {
        var items = ItemsOf(reward);

        if (inventory.InventoryOf(player) is null ||
            items.Any(e => !inventory.Catalog.Exists(e.Item)) ||
            !inventory.CanReceive(player, ShopCurrency.Ether, reward.Ether) ||
            !inventory.CanReceive(player, ShopCurrency.Bp, reward.BattlePoints))
        {
            return false;
        }

        var bag = new List<(int Item, int Count)>();
        var mail = new List<(int Item, int Count)>();

        foreach (var item in items)
        {
            // An item goes whole: into the bags if it fits after those before it, else into the letter.
            if (delivery == Delivery.BagThenLetter && inventory.CanGiveAll(player, [.. bag, item]))
            {
                bag.Add(item);
            }
            else
            {
                mail.Add(item);
            }
        }

        var gold = inventory.CanReceive(player, ShopCurrency.Gold, reward.Gold) ? reward.Gold : 0;
        var mailGold = reward.Gold - gold;

        foreach (var (item, count) in bag)
        {
            // It was counted with the others, so it fits; if the bags say otherwise, it goes by letter all the same.
            if (!inventory.TryGive(player, item, count))
            {
                mail.Add((item, count));
            }
        }

        AddCurrencies(player, gold, reward);

        if (mail.Count > 0 || mailGold > 0)
        {
            // A letter carries five items at most; the gold goes with the first.
            (int Item, int Count)[][] letters = mail.Count == 0 ? [[]] : mail.Chunk(PostModuleDefines.ItemCount).ToArray();

            for (var i = 0; i < letters.Length; i++)
            {
                _ = MailAsync(player, account, source, letters[i], i == 0 ? mailGold : 0);
            }
        }

        return true;
    }

    private static List<(int Item, int Count)> ItemsOf(Reward reward)
    {
        return reward.Items
            .Where(e => e.Item > 0 && e.Count > 0)
            .Select(e => (e.Item, (int)e.Count))
            .ToList();
    }

    private void AddCurrencies(Entity player, long gold, Reward reward)
    {
        if (gold != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Gold, gold);
        }

        if (reward.Ether != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Ether, reward.Ether);
        }

        if (reward.BattlePoints != 0)
        {
            inventory.TryAddCurrency(player, ShopCurrency.Bp, reward.BattlePoints);
        }
    }

    /// <summary>
    /// Writes one letter of the reward to the account, trying again while the database does not take it, and tells the
    /// person when it is in, if the person is still here. The items are made when the letter is written, with what the
    /// tables give a new item, and are taken out of the letter under serials of their own.
    /// </summary>
    private async Task MailAsync(Entity player, int account, RewardSource source, (int Item, int Count)[] items, long gold)
    {
        // The client has a row for the reward of a daily mission and none for attendance: that letter shows its own text.
        var (title, body, row) = source switch
        {
            RewardSource.DailyMission => ("[System] Daily Mission Completion Reward",
                "[Daily Mission] Your completion reward has arrived. Please clear some space in your inventory to claim the attachment.",
                GameLetters.DailyMissionReward),
            RewardSource.PlayTime => ("[System] Play Time Reward",
                "Your inventory had no room for all of your play time reward, so the rest is kept here. Please clear some space in your inventory to claim the attachment.",
                GameLetters.OwnText),
            _ => ("[System] Attendance Reward",
                "Your inventory had no room for all of your attendance reward, so the rest is kept here. Please clear some space in your inventory to claim the attachment.",
                GameLetters.OwnText)
        };

        for (var attempt = 0; ; attempt++)
        {
            PostPersistent letter;

            try
            {
                await inventory.EnsureSerialsAsync(items.Select(e => (e.Item, 1)));

                if (!inventory.CanMake(items.Length))
                {
                    throw new ItemSerialsExhaustedException();
                }

                var attached = items.Select(e => PostLetters.Attach(inventory.Create(e.Item, e.Count), (short)e.Count)).ToList();

                letter = await post!.SendToAccountAsync(
                    new PostAccountDraft(account, Sender, title, body, gold, attached, GameLetters.AccountKind, row),
                    CancellationToken.None);
            }

            catch (Exception e) when (attempt < RetryDelays.Length)
            {
                LogLetterRetried(e, account, source, attempt + 1);

                await Task.Delay(RetryDelays[attempt], _time);

                continue;
            }

            catch (Exception e)
            {
                LogLetterLost(e, account, source, gold, string.Join(", ", items.Select(x => string.Create(CultureInfo.InvariantCulture, $"{x.Item} x {x.Count}"))));

                return;
            }

            await TellAsync(player, account, letter);

            return;
        }
    }

    private async Task TellAsync(Entity player, int account, PostPersistent letter)
    {
        int unread;

        try
        {
            (_, unread) = await post!.CountUnreadAsync(0, account);
        }

        catch (Exception e)
        {
            // The letter is in; the person sees it when the post is opened.
            LogNotTold(e, account, letter.Id);

            return;
        }

        if (world.IsAlive(player) && world.Has<ServerSessionComponent>(player))
        {
            world.Get<ServerSessionComponent>(player).Send(new PostAccountRecvResponse
            {
                PostCount = (short)Math.Min(unread, short.MaxValue),
                Value = PostLetters.Account(letter)
            });
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The letter with a reward from {Source} to account {Account} was not written (attempt {Attempt}); it is tried again")]
    private partial void LogLetterRetried(Exception exception, int account, RewardSource source, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "The letter with a reward from {Source} to account {Account} is lost: {Gold} gold, items {Items}")]
    private partial void LogLetterLost(Exception exception, int account, RewardSource source, long gold, string items);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Account {Account} was not told of the letter {Letter} with a reward")]
    private partial void LogNotTold(Exception exception, int account, long letter);
}
