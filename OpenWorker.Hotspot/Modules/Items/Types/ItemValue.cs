using Arch.Core;
using OpenWorker.Extensions;
using OpenWorker.Hotspot.Modules.Items.Components;
using OpenWorker.Hotspot.Modules.Items.Enums;
using OpenWorker.Hotspot.Modules.Items.Extensions;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct ItemValue
{
    public int Item { get; init; }
    public SerialValue Serial { get; init; }
    public short Count { get; init; }
    public ItemBindType BindType { get; init; }
    public byte Endurance { get; init; }

    public List<ItemOption> OptionList { get; init; }

    public byte Upgrade { get; init; }
    public byte Flag { get; init; }
    public byte SocketActiveCount { get; init; }
    public DateTimeOffset DueTo { get; init; }
    public byte UpgradeCount { get; init; }
    public byte UpgradeLimit { get; init; }
    public int Exp { get; init; }
    public string BroachState { get; init; }
    public byte RestoreCount { get; init; }
    public byte SealCount { get; init; }

    public ItemValue(Arch.Core.World world, Entity item)
    {
        if (Entity.Null == item)
        {
            Item = -1;
            Serial = new SerialValue(-1);

            OptionList = Enumerable
                .Range(0, ItemModuleDefines.OptionCount)
                .Select(_ => new ItemOption())
                .ToList();

            BroachState = string.Empty;
        }

        else
        {
            Item = world.Get<StorageItemComponent>(item).Identifier;
            Serial = new SerialValue(world, item);
            Count = world.Get<StorageSlotComponent>(item).Count;

            if (world.Has<StorageItemOptionComponent>(item))
            {
                var options = world.Get<StorageItemOptionComponent>(item);
                
                OptionList = options.Collection
                    .Select(x => new ItemOption
                    {
                        Type = x.Type,
                        Option = x.Value,
                    })
                    .ToList();
            }

            else
            {
                OptionList = Enumerable
                    .Range(0, ItemModuleDefines.OptionCount)
                    .Select(_ => new ItemOption())
                    .ToList();
            }

            Endurance = 100;
            BroachState = string.Empty;
            DueTo = DateTimeOffset.Now.AddDays(1);
        }
    }

    public ItemValue(BinaryReader reader)
    {
        Item = reader.ReadInt32();
        Serial = new SerialValue(reader);
        Count = reader.ReadInt16();
        BindType = reader.ReadItemBindType();
        Endurance = reader.ReadByte();
        OptionList = Enumerable
            .Range(0, ItemModuleDefines.OptionCount)
            .Select(_ => new ItemOption(reader))
            .ToList();
        Upgrade = reader.ReadByte();
        Flag = reader.ReadByte();
        SocketActiveCount = reader.ReadByte();
        DueTo = DateTimeOffset.FromUnixTimeSeconds(reader.ReadInt64());
        UpgradeCount = reader.ReadByte();
        UpgradeLimit = reader.ReadByte();
        Exp = reader.ReadInt32();
        BroachState = reader.ReadUtf8AsciiStringWithoutTerminator();
        RestoreCount = reader.ReadByte();
        SealCount = reader.ReadByte();
    }
}