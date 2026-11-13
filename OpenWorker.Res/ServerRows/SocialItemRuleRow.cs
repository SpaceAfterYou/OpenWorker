using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The server's own <c>tb_Social_Item</c>: how big a social object is, how many may use it and what kind it is, which the
/// client's copy lays out differently and without the kind.
/// </summary>
[BinaryResourceTable("tb_Social_Item", Server = true, Optional = true)]
public readonly struct SocialItemRuleRow
{
    /// <summary>An object a player puts down for everybody to use.</summary>
    public const byte Normal = 0;

    public SocialItemRuleRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();
        CollisionRange = reader.ReadInt32();
        UseRange = reader.ReadInt32();
        OffsetDistance = reader.ReadInt32();
        UseMax = reader.ReadByte();
        Duration = reader.ReadInt32();
        Aura = reader.ReadUInt16();
        CodeName = reader.ReadUtf16UnicodeStringWithoutTerminator();
        Furniture = reader.ReadInt32();
        Kind = reader.ReadByte();
    }

    public ushort Id { get; }

    /// <summary>The width of the ground the object takes: half of it is its radius.</summary>
    public int CollisionRange { get; }

    /// <summary>How near a player stands to use it.</summary>
    public int UseRange { get; }

    /// <summary>How far in front of the player the client puts it.</summary>
    public int OffsetDistance { get; }

    /// <summary>How many players besides its owner may use it at once.</summary>
    public byte UseMax { get; }

    /// <summary>A time of the row, in milliseconds; the object lasts as long as its item says instead.</summary>
    public int Duration { get; }

    /// <summary>The aura a player using the object carries.</summary>
    public ushort Aura { get; }

    public string CodeName { get; }

    /// <summary>The row of the room furniture table an object of the furniture kind takes its seats from.</summary>
    public int Furniture { get; }

    /// <summary>0 an object a player puts down, 1 one the map puts down, 2 furniture, 3 a game for two.</summary>
    public byte Kind { get; }
}
