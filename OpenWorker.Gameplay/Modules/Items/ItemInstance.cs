using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Types;

namespace OpenWorker.Gameplay.Modules.Items;

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
            OptionValue = Options.Select(e => e.Option).ToArray()
        };
    }

    public static ItemInstance From(ItemPersistent row)
    {
        var options = new ItemOption[ItemModuleDefines.OptionCount];

        for (var i = 0; i < options.Length && i < row.OptionType.Length && i < row.OptionValue.Length; i++)
        {
            options[i] = new ItemOption(row.OptionType[i], row.OptionValue[i]);
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
            IsNew = false
        };
    }
}
