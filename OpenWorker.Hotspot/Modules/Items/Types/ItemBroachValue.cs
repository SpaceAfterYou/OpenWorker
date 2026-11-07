using OpenWorker.Hotspot.Messages.Abstractions;
using System.Diagnostics;

namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct ItemBroachValue : IWritableData
{
    public const int ItemCount = 15;

    public long Serial { get; init; }
    public int[] Items { get; init; }

    public ItemBroachValue(BinaryReader reader)
    {
        Serial = reader.ReadInt64();
        Items = Enumerable
            .Range(0, ItemCount)
            .Select(_ => reader.ReadInt32())
            .ToArray();
    }

    public void Write(BinaryWriter writer)
    {
        Debug.Assert(Items.Length is ItemCount);

        writer.Write(Serial);

        foreach (var item in Items)
        {
            writer.Write(item);
        }
    }
}
