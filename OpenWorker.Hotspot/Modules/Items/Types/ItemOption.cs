namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct ItemOption(BinaryReader reader)
{
    public short Type { get; init; } = reader.ReadInt16();
    public int Option { get; init; } = reader.ReadInt32();
}