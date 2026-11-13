using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The chances of the groups of a gacha, out of ten thousand: the place of a chance is the place of the group in the
/// gacha's row of <c>tb_Random_Get_Group</c>.
/// </summary>
[BinaryResourceTable("tb_Gacha_Group", Optional = true)]
public readonly struct GachaGroupRow
{
    public const int Groups = 20;

    public GachaGroupRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();

        // What the row is, in words.
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        Group = reader.ReadUInt16();
        On = reader.ReadByte() == 1;
        Chances = new int[Groups];

        for (var index = 0; index < Groups; index++)
        {
            Chances[index] = reader.ReadUInt16();
        }
    }

    public ushort Id { get; }

    /// <summary>
    /// The number the gacha names it by.
    /// </summary>
    public ushort Group { get; }

    public bool On { get; }

    public int[] Chances { get; }
}

/// <summary>
/// A gacha, from the server's own <c>tb_Random_Get_Group</c> (the client's has its rows without the groups): the item it is
/// opened from, the days it may be opened, its chances and the draws of its groups.
/// </summary>
[BinaryResourceTable("tb_Random_Get_Group", Server = true)]
public readonly struct GachaRow
{
    public GachaRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        Item = reader.ReadInt32();

        // How the window shows it.
        reader.ReadByte();

        Period = reader.ReadByte() == 1;
        StartYear = reader.ReadUInt16();
        StartMonth = reader.ReadByte();
        StartDay = reader.ReadByte();
        EndYear = reader.ReadUInt16();
        EndMonth = reader.ReadByte();
        EndDay = reader.ReadByte();
        Chances = reader.ReadUInt16();
        Draws = new int[GachaGroupRow.Groups];

        for (var index = 0; index < GachaGroupRow.Groups; index++)
        {
            Draws[index] = reader.ReadUInt16();
        }
    }

    public ushort Id { get; }

    /// <summary>
    /// The gacha item that is used up.
    /// </summary>
    public int Item { get; }

    /// <summary>
    /// It may be opened only between the start and the end, compared as year, month and day each on its own.
    /// </summary>
    public bool Period { get; }

    public ushort StartYear { get; }
    public byte StartMonth { get; }
    public byte StartDay { get; }
    public ushort EndYear { get; }
    public byte EndMonth { get; }
    public byte EndDay { get; }

    /// <summary>
    /// The row of <c>tb_Gacha_Group</c> with the chances of its groups.
    /// </summary>
    public ushort Chances { get; }

    /// <summary>
    /// The rows of <c>tb_Random_Get</c> its groups draw from.
    /// </summary>
    public int[] Draws { get; }
}

/// <summary>
/// A draw of a gacha, from the server's own <c>tb_Random_Get</c>: up to thirty items, each with its chance out of ten
/// thousand and the kind of draw the client is told.
/// </summary>
[BinaryResourceTable("tb_Random_Get", Server = true)]
public readonly struct GachaDrawRow
{
    public const int Items = 30;

    public GachaDrawRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        ItemList = new int[Items];
        Chances = new int[Items];
        Types = new byte[Items];

        for (var index = 0; index < Items; index++)
        {
            ItemList[index] = reader.ReadInt32();
        }

        for (var index = 0; index < Items; index++)
        {
            Chances[index] = reader.ReadUInt16();
        }

        for (var index = 0; index < Items; index++)
        {
            Types[index] = reader.ReadByte();
        }
    }

    public ushort Id { get; }

    public int[] ItemList { get; }

    public int[] Chances { get; }

    public byte[] Types { get; }
}
