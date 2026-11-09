using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Gameplay.Modules.Items;

/// <summary>
/// A gem in a socket of an item: the gem item, and the options it gives to the item.
/// </summary>
public readonly record struct ItemSocket(int Gem, ItemOption[] Options)
{
    public bool IsEmpty => Gem == 0;
}

/// <summary>
/// One item of a person: what the client shows in a slot. The place of the item is kept next to it
/// so that it can be saved without looking for it.
/// </summary>
public sealed class ItemInstance
{
    public const string NoBroaches = "000000000000000";

    public required long Serial { get; init; }
    public required int Item { get; init; }
    public required short Count { get; set; }

    public StorageGroup Storage { get; internal set; } = StorageGroup.None;
    public short Slot { get; internal set; } = -1;

    public ItemBindType BindType { get; set; }
    public byte Upgrade { get; set; }
    public byte Endurance { get; set; } = 100;
    public byte SocketActive { get; set; }
    public DateTimeOffset? DueTo { get; set; }
    public byte UpgradeCount { get; set; }
    public byte UpgradeLimit { get; set; }
    public byte Flag { get; set; }
    public int Exp { get; set; }
    public string BroachState { get; set; } = NoBroaches;
    public byte RestoreCount { get; set; }
    public byte SealCount { get; set; }
    public ItemOption[] Options { get; set; } = new ItemOption[ItemModuleDefines.OptionCount];

    /// <summary>
    /// The gems of the item by the place of their socket; a place without a gem is empty.
    /// </summary>
    public ItemSocket[] Sockets { get; set; } = [];

    /// <summary>
    /// The broaches set into the item, <see cref="ST_ITEM_BROACH.ItemCount"/> of them, or none at all.
    /// </summary>
    public int[] Broaches { get; set; } = [];

    /// <summary>
    /// The gem in a socket, or an empty socket when there is none at that place.
    /// </summary>
    public ItemSocket SocketAt(int place)
    {
        return place >= 0 && place < Sockets.Length ? Sockets[place] : default;
    }

    /// <summary>
    /// Puts a gem into a socket, making room for the place when the item had fewer sockets filled.
    /// </summary>
    public void SetSocket(int place, ItemSocket socket)
    {
        if (place >= Sockets.Length)
        {
            var sockets = new ItemSocket[place + 1];

            Sockets.CopyTo(sockets, 0);
            Sockets = sockets;
        }

        Sockets[place] = socket;
    }

    /// <summary>
    /// The gems of the item as the client reads them, one entry per filled socket.
    /// </summary>
    public IEnumerable<ST_SOCKET_DATA> SocketValues()
    {
        for (var place = 0; place < Sockets.Length; place++)
        {
            if (!Sockets[place].IsEmpty)
            {
                yield return new ST_SOCKET_DATA
                {
                    biEquipSerial = new SerialValue(Serial),
                    dwSocketID = Sockets[place].Gem,
                    bySocketPos = (byte)place,
                    stExtendOption = Sockets[place].Options.ToList()
                };
            }
        }
    }

    /// <summary>
    /// The broaches of the item as the client reads them, or <c>null</c> when it has none.
    /// </summary>
    public ST_ITEM_BROACH? BroachValue()
    {
        return Broaches.Length == ST_ITEM_BROACH.ItemCount
            ? new ST_ITEM_BROACH { biSerial = new SerialValue(Serial), dwItemID = Broaches.ToArray() }
            : null;
    }

    /// <summary>
    /// The item has never been saved.
    /// </summary>
    internal bool IsNew { get; set; } = true;

    /// <summary>
    /// How the client sees the item in its packets.
    /// </summary>
    public ItemValue ToValue()
    {
        return new ItemValue
        {
            Item = Item,
            Serial = new SerialValue(Serial),
            Count = Count,
            BindType = BindType,
            OptionList = Options.ToList(),
            Upgrade = Upgrade,
            Endurance = Endurance,
            SocketActiveCount = SocketActive,
            DueTo = DueTo ?? DateTimeOffset.UnixEpoch,
            UpgradeCount = UpgradeCount,
            UpgradeLimit = UpgradeLimit,
            Flag = Flag,
            Exp = Exp,
            BroachState = BroachState,
            RestoreCount = RestoreCount,
            SealCount = SealCount
        };
    }

    public ItemPersistent ToPersistent(int person)
    {
        return new ItemPersistent
        {
            Id = Serial,
            PersonId = person,
            Storage = (byte)Storage,
            Slot = Slot,
            Item = Item,
            Count = Count,
            BindType = (byte)BindType,
            Upgrade = Upgrade,
            Endurance = Endurance,
            SocketActive = SocketActive,
            DueTo = DueTo?.UtcDateTime,
            UpgradeCount = UpgradeCount,
            UpgradeLimit = UpgradeLimit,
            Flag = Flag,
            Exp = Exp,
            BroachState = BroachState,
            RestoreCount = RestoreCount,
            SealCount = SealCount,
            OptionType = Options.Select(e => e.Type).ToArray(),
            OptionValue = Options.Select(e => e.Option).ToArray(),
            Sockets = Sockets.Select(e => e.Gem).ToArray(),
            SocketOptionType = Sockets.SelectMany(e => Fill(e.Options)).Select(e => e.Type).ToArray(),
            SocketOptionValue = Sockets.SelectMany(e => Fill(e.Options)).Select(e => e.Option).ToArray(),
            Broaches = Broaches.ToArray()
        };
    }

    public static ItemInstance From(ItemPersistent row)
    {
        var options = new ItemOption[ItemModuleDefines.OptionCount];

        for (var i = 0; i < options.Length && i < row.OptionType.Length && i < row.OptionValue.Length; i++)
        {
            options[i] = new ItemOption(row.OptionType[i], row.OptionValue[i]);
        }

        var sockets = new ItemSocket[row.Sockets.Length];

        for (var place = 0; place < sockets.Length; place++)
        {
            var gem = new ItemOption[ItemModuleDefines.OptionCount];

            for (var i = 0; i < gem.Length; i++)
            {
                var at = place * ItemModuleDefines.OptionCount + i;

                if (at < row.SocketOptionType.Length && at < row.SocketOptionValue.Length)
                {
                    gem[i] = new ItemOption(row.SocketOptionType[at], row.SocketOptionValue[at]);
                }
            }

            sockets[place] = new ItemSocket(row.Sockets[place], gem);
        }

        return new ItemInstance
        {
            Serial = row.Id,
            Item = row.Item,
            Count = row.Count,
            Storage = (StorageGroup)row.Storage,
            Slot = row.Slot,
            BindType = (ItemBindType)row.BindType,
            Upgrade = row.Upgrade,
            Endurance = row.Endurance,
            SocketActive = row.SocketActive,
            DueTo = row.DueTo is { } due ? new DateTimeOffset(DateTime.SpecifyKind(due, DateTimeKind.Utc)) : null,
            UpgradeCount = row.UpgradeCount,
            UpgradeLimit = row.UpgradeLimit,
            Flag = row.Flag,
            Exp = row.Exp,
            BroachState = row.BroachState,
            RestoreCount = row.RestoreCount,
            SealCount = row.SealCount,
            Options = options,
            Sockets = sockets,
            Broaches = row.Broaches.Length == ST_ITEM_BROACH.ItemCount ? row.Broaches.ToArray() : [],
            IsNew = false
        };
    }

    /// <summary>
    /// The options of a gem as they are saved: always <see cref="ItemModuleDefines.OptionCount"/>, an empty socket included.
    /// </summary>
    private static IEnumerable<ItemOption> Fill(ItemOption[]? options)
    {
        for (var i = 0; i < ItemModuleDefines.OptionCount; i++)
        {
            yield return options is not null && i < options.Length ? options[i] : default;
        }
    }
}
