namespace OpenWorker.Hotspot.Modules.Items.Types;

public readonly struct ItemOption
{
    public short Type { get; init; }
    public int Option { get; init; }

    public ItemOption(short type, int option)
    {
        Type = type;
        Option = option;
    }

    public ItemOption(BinaryReader reader)
    {
        Type = reader.ReadInt16();
        Option = reader.ReadInt32();
    }
}
