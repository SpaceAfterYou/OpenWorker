using OpenWorker.Wire;
using OpenWorker.Hotspot.Modules.Items.Types;
using System.Runtime.CompilerServices;

namespace OpenWorker.Hotspot.Modules.Items.Extensions;

public static class ItemAttachmentExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSocketValue[] ReadItemSocketList(this BinaryReader reader)
    {
        return Enumerable
            .Range(0, reader.ReadByte())
            .Select(_ => new ItemSocketValue(reader))
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemBroachValue[] ReadItemBroachList(this BinaryReader reader)
    {
        return Enumerable
            .Range(0, reader.ReadByte())
            .Select(_ => new ItemBroachValue(reader))
            .ToArray();
    }

    public static void Write(this BinaryWriter writer, ItemSocketValue[] values)
    {
        writer.Write(checked((byte)values.Length));

        foreach (var value in values)
        {
            WireBridge.Write(writer, value);
        }
    }

    public static void Write(this BinaryWriter writer, ItemBroachValue[] values)
    {
        writer.Write(checked((byte)values.Length));

        foreach (var value in values)
        {
            WireBridge.Write(writer, value);
        }
    }
}
