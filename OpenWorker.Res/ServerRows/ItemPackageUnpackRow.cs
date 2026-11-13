using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The server's own <c>tb_Item_Package</c>: how each item of a package is given, which the client's copy leaves out. An item
/// given as it is comes with its count; an item given upgraded comes as one, at the level its count names.
/// </summary>
[BinaryResourceTable("tb_Item_Package", Server = true)]
public readonly struct ItemPackageUnpackRow
{
    /// <summary>The items a package holds.</summary>
    public const int Entries = 15;

    /// <summary>The kind of an entry that gives one item at the level its count names.</summary>
    public const byte Upgraded = 1;

    public ItemPackageUnpackRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();

        // The money, which the client's copy has as well.
        reader.ReadBytes(3 * sizeof(int));

        Items = new int[Entries];
        Kinds = new byte[Entries];

        for (var i = 0; i < Entries; i++)
        {
            Items[i] = reader.ReadInt32();
        }

        for (var i = 0; i < Entries; i++)
        {
            Kinds[i] = reader.ReadByte();
        }

        // The counts, which the client's copy has as well.
        reader.ReadBytes(Entries * sizeof(int));
    }

    public int Id { get; }

    /// <summary>The item of each entry.</summary>
    public int[] Items { get; }

    /// <summary>How the item of each entry is given.</summary>
    public byte[] Kinds { get; }
}
