using OpenWorker.Hotspot.Modules.Shop.Types;

namespace OpenWorker.Hotspot.Modules.Shop.Extensions;

internal static class BinaryReaderExtension
{
    internal static IReadOnlyList<CashItemEntry> ReadCashItemList(this BinaryReader reader)
    {
        return Enumerable
            .Range(0, reader.ReadInt32())
            .Select(_ => new CashItemEntry(reader)).ToArray();
    }
}