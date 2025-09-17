namespace OpenWorker.Hotspot.Modules.Shop.Types;

public readonly struct CashItemEntry(BinaryReader reader)
{
    public int Index { get; } = reader.ReadInt32();
    public byte Select { get; } = reader.ReadByte();
}