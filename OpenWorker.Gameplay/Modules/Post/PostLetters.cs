using Arch.Core;
using OpenWorker.Domain.Persistent;
using OpenWorker.Gameplay.Modules.Items;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;
using OpenWorker.Hotspot.Modules.Post;
using OpenWorker.Hotspot.Modules.Post.Types;

namespace OpenWorker.Gameplay.Modules.Post;

/// <summary>
/// Letters as the client sees them, and what they carry as it moves between a letter and an inventory.
/// </summary>
public static class PostLetters
{
    /// <summary>
    /// What a person pays for every letter, besides the money it carries.
    /// </summary>
    public const long Postage = 100;

    /// <summary>
    /// The storages whose items may go with a letter: what is worn, kept in a bank or bought with cash stays.
    /// </summary>
    public static bool CanSendFrom(StorageGroup storage) => storage is StorageGroup.Common or StorageGroup.Costume;

    #region What a letter carries

    /// <summary>
    /// Some of an item of the inventory as it goes into a letter, with everything that was done to it.
    /// </summary>
    public static PostItemPersistent Attach(ItemInstance item, short count)
    {
        // The gems and broaches go as the item saves them.
        var saved = item.ToPersistent(0);

        return new PostItemPersistent
        {
            Item = item.Item,
            Count = count,
            BindType = (byte)item.BindState,
            Upgrade = item.Upgrade,
            Endurance = item.Endurance,
            SocketActive = item.SocketActive,
            DueTo = item.DueTo?.UtcDateTime,
            UpgradeCount = item.UpgradeCount,
            UpgradeLimit = item.UpgradeLimit,
            Flag = item.Flag,
            Exp = item.Exp,
            BroachState = item.BroachState,
            RestoreCount = item.RestoreCount,
            SealCount = item.SealCount,
            OptionType = item.Options.Select(e => e.Type).ToArray(),
            OptionValue = item.Options.Select(e => e.Option).ToArray(),
            Sockets = saved.Sockets,
            SocketOptionType = saved.SocketOptionType,
            SocketOptionValue = saved.SocketOptionValue,
            Broaches = saved.Broaches
        };
    }

    /// <summary>
    /// Whether the inventory of the person has room for all of it at once.
    /// </summary>
    public static bool Fits(InventoryService inventory, Entity player, IReadOnlyList<PostItemPersistent> items)
    {
        return items.Count == 0 || inventory.CanGiveAll(player, items.Select(e => (e.Item, (int)e.Count)).ToArray());
    }

    /// <summary>
    /// Puts what a letter carries into the inventory of the person, which marks it for the flush; check
    /// <see cref="Fits"/> first. Stacks are filled as the inventory fills them and the client is told; an item that does
    /// not stack comes back as it was sent, under a serial of its own, and is returned for the caller to tell the client.
    /// </summary>
    public static List<ItemInstance> Give(InventoryService inventory, Entity player, IReadOnlyList<PostItemPersistent> items)
    {
        var created = new List<ItemInstance>();
        var bag = inventory.InventoryOf(player);

        if (bag is null)
        {
            return created;
        }

        foreach (var item in items)
        {
            if (inventory.Catalog.StackLimit(item.Item) > 1)
            {
                inventory.TryGive(player, item.Item, item.Count);
                continue;
            }

            for (var i = 0; i < item.Count; i++)
            {
                var storage = bag.Find(inventory.Catalog.HomeOf(item.Item));
                var slot = storage?.FirstFree() ?? -1;

                if (storage is null || slot < 0)
                {
                    break;
                }

                var instance = Restore(inventory.Create(item.Item, 1), item);

                bag.Place(storage.Group, slot, instance);
                created.Add(instance);
            }
        }

        return created;
    }

    private static ItemInstance Restore(ItemInstance instance, PostItemPersistent item)
    {
        instance.BindState = (ItemBindState)item.BindType;
        instance.Upgrade = item.Upgrade;
        instance.Endurance = item.Endurance;
        instance.SocketActive = item.SocketActive;
        instance.DueTo = item.DueTo is { } due ? new DateTimeOffset(DateTime.SpecifyKind(due, DateTimeKind.Utc)) : null;
        instance.UpgradeCount = item.UpgradeCount;
        instance.UpgradeLimit = item.UpgradeLimit;
        instance.Flag = item.Flag;
        instance.Exp = item.Exp;
        instance.BroachState = item.BroachState;
        instance.RestoreCount = item.RestoreCount;
        instance.SealCount = item.SealCount;

        for (var i = 0; i < instance.Options.Length && i < item.OptionType.Length && i < item.OptionValue.Length; i++)
        {
            instance.Options[i] = new ItemOption(item.OptionType[i], item.OptionValue[i]);
        }

        var attached = ItemInstance.From(new ItemPersistent
        {
            Sockets = item.Sockets ?? [],
            SocketOptionType = item.SocketOptionType ?? [],
            SocketOptionValue = item.SocketOptionValue ?? [],
            Broaches = item.Broaches ?? []
        });

        instance.Sockets = attached.Sockets;
        instance.Broaches = attached.Broaches;

        return instance;
    }

    #endregion

    #region As the client sees them

    /// <summary>
    /// A letter as its receiver sees it: who sent it.
    /// </summary>
    public static PostValue Received(PostPersistent letter)
    {
        return ToValue(letter, new PostCharacterValue { Character = letter.SenderId ?? 0, Class = letter.SenderHero, Name = letter.SenderName }, RemainTime(letter));
    }

    /// <summary>
    /// A letter as its sender sees it: whom it went to.
    /// </summary>
    public static PostValue Sent(PostPersistent letter)
    {
        // The sender lists it until its time is up, even when the receiver keeps it.
        return ToValue(letter, new PostCharacterValue { Character = letter.ReceiverId ?? 0, Class = letter.ReceiverHero, Name = letter.ReceiverName }, End(letter));
    }

    public static PostAccountValue Account(PostPersistent letter)
    {
        return new PostAccountValue
        {
            Serial = letter.Id,
            Account = letter.ReceiverAccountId ?? 0,
            Title = letter.Title,
            Message = letter.Body,
            Money = letter.HasAttachments ? letter.Money : 0,
            Items = Items(letter),
            RegisteredTime = new DateTimeOffset(DateTime.SpecifyKind(letter.CreatedAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            RemainTime = End(letter),
            Flag = Flag(letter),
            SenderName = letter.SenderName,
            MainType = letter.MainType,
            SubType = letter.SubType
        };
    }

    // An empty place for an item. It comes before the empty letter, which is made of it: static members start in order.
    private static ItemValue EmptyItem { get; } = new()
    {
        OptionList = Enumerable.Range(0, ItemModuleDefines.OptionCount).Select(_ => new ItemOption(0, 0)).ToList(),
        DueTo = DateTimeOffset.UnixEpoch,
        BroachState = ItemInstance.NoBroaches
    };

    /// <summary>
    /// The answer to a letter that was not sent: the result and an empty letter.
    /// </summary>
    public static PostValue Empty { get; } = new()
    {
        Character = new PostCharacterValue { Name = string.Empty },
        Title = string.Empty,
        Message = string.Empty,
        Items = Enumerable.Repeat(EmptyItem, PostModuleDefines.ItemCount).ToArray(),
        Sockets = [],
        Broaches = []
    };

    /// <summary>
    /// What the client is shown of the state: read, taken, sent back, kept. A letter that carried nothing has nothing taken.
    /// </summary>
    public static byte Flag(PostPersistent letter)
    {
        var state = letter.State & PostState.Shown;

        if (letter.Money == 0 && letter.Items.Count == 0)
        {
            state &= ~PostState.Received;
        }

        return (byte)state;
    }

    /// <summary>
    /// The end of the letter for its receiver, as the client reads the field it calls the remaining time: a moment in Unix
    /// seconds, which it compares with the date the server gave it (04/03), and 0 for a letter its receiver keeps, which has
    /// no end. The client takes a letter whose end has passed off its lists by itself.
    /// </summary>
    public static long RemainTime(PostPersistent letter)
    {
        return letter.State.HasFlag(PostState.Saved) ? 0 : End(letter);
    }

    private static long End(PostPersistent letter)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(letter.ExpiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }

    private static PostValue ToValue(PostPersistent letter, PostCharacterValue character, long remainTime)
    {
        return new PostValue
        {
            Serial = letter.Id,
            Character = character,
            Title = letter.Title,
            Message = letter.Body,
            Money = letter.HasAttachments ? letter.Money : 0,
            Items = Items(letter),
            RegisteredTime = new DateTimeOffset(DateTime.SpecifyKind(letter.CreatedAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
            Flag = Flag(letter),
            Type = letter.MainType,
            SubType = letter.SubType,
            RemainTime = remainTime,
            Event = 0,
            Sockets = [],
            Broaches = []
        };
    }

    /// <summary>
    /// The five places for items of a letter: what it still carries, then empty ones.
    /// </summary>
    private static ItemValue[] Items(PostPersistent letter)
    {
        var items = new ItemValue[PostModuleDefines.ItemCount];
        var carried = letter.HasAttachments ? letter.Items : [];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = i < carried.Count ? ToValue(carried[i]) : EmptyItem;
        }

        return items;
    }

    private static ItemValue ToValue(PostItemPersistent item)
    {
        return new ItemValue
        {
            Item = item.Item,
            Serial = new SerialValue(0),
            Count = item.Count,
            BindState = (ItemBindState)item.BindType,
            OptionList = Enumerable.Range(0, ItemModuleDefines.OptionCount)
                .Select(i => i < item.OptionType.Length && i < item.OptionValue.Length ? new ItemOption(item.OptionType[i], item.OptionValue[i]) : new ItemOption(0, 0))
                .ToList(),
            Upgrade = item.Upgrade,
            Endurance = item.Endurance,
            SocketActiveCount = item.SocketActive,
            DueTo = item.DueTo is { } due ? new DateTimeOffset(DateTime.SpecifyKind(due, DateTimeKind.Utc)) : DateTimeOffset.UnixEpoch,
            UpgradeCount = item.UpgradeCount,
            UpgradeLimit = item.UpgradeLimit,
            Flag = item.Flag,
            Exp = item.Exp,
            BroachState = item.BroachState,
            RestoreCount = item.RestoreCount,
            SealCount = item.SealCount
        };
    }

    #endregion
}
