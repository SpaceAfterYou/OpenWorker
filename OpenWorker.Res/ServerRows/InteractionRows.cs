using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

// What the server knows of the objects a person clicks and of what they give. The client's tables of the same names keep the
// columns the client shows and leave these out.

/// <summary>
/// An object of a map as the server's own <c>tb_Interaction_Object</c> has it: what a cube asks of the person who takes it, how
/// often it can be taken and what it gives. The rest of the row is read to reach its end.
/// </summary>
[BinaryResourceTable("tb_Interaction_Object", Server = true)]
public readonly struct CubeObjectRow
{
    /// <summary>The kind of object a cube is.</summary>
    public const byte CubeKind = 2;

    public CubeObjectRow(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        Kind = reader.ReadByte();

        // Whether it changes for the person alone.
        reader.ReadByte();

        HoldMilliseconds = reader.ReadUInt32();

        // The kind of resource and the file of the model.
        reader.ReadByte();
        reader.ReadUtf16UnicodeStringWithoutTerminator();

        // The state it starts in, whether it is in sight and the sights it changes.
        reader.ReadBytes(3);

        // The quest and the class it shows for, the item, the quest and the condition it asks for.
        reader.ReadBytes(7 * sizeof(uint));

        MinPicks = reader.ReadInt16();
        MaxPicks = reader.ReadInt16();

        // The time before it can be used again and the range it can be used from.
        reader.ReadBytes(2 * sizeof(uint));

        // Whether it goes after use, and the items it takes and gives.
        reader.ReadByte();
        reader.ReadBytes(4 * sizeof(uint));

        AliveAtOnce = reader.ReadUInt16();
        RespawnMilliseconds = reader.ReadUInt32();
        RewardTable = reader.ReadUInt32();

        // The aura it links to, whether its use counts for the quests of everyone, and the script.
        reader.ReadBytes(sizeof(uint) + sizeof(byte));
        reader.ReadUtf16UnicodeStringWithoutTerminator();
    }

    public uint Id { get; }

    /// <summary>The kind of object: <see cref="CubeKind"/> for a cube.</summary>
    public byte Kind { get; }

    /// <summary>How long the person has to hold the click, in milliseconds.</summary>
    public uint HoldMilliseconds { get; }

    /// <summary>The fewest times a cube can be taken, and the most.</summary>
    public short MinPicks { get; }

    public short MaxPicks { get; }

    /// <summary>How many cubes of the kind stand at once.</summary>
    public ushort AliveAtOnce { get; }

    /// <summary>How long a cube that was taken away is waited for, in milliseconds.</summary>
    public uint RespawnMilliseconds { get; }

    /// <summary>The row of <see cref="CubeRewardRow"/> that tells what a take gives.</summary>
    public uint RewardTable { get; }
}

/// <summary>
/// What a take of a cube gives, from the server's own <c>tb_Interaction_Item</c>: ten places, each with its chance (in ten
/// thousandths), the item or kind of money, and how much of it at least and at most.
/// </summary>
[BinaryResourceTable("tb_Interaction_Item", Server = true)]
public readonly struct CubeRewardRow
{
    /// <summary>The places of a row.</summary>
    public const int Places = 10;

    public CubeRewardRow(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        Chances = ReadAll(reader, r => r.ReadUInt16());
        Items = ReadAll(reader, r => r.ReadUInt32());
        Least = ReadAll(reader, r => r.ReadUInt16());
        Most = ReadAll(reader, r => r.ReadUInt16());
    }

    private static T[] ReadAll<T>(BinaryReader reader, Func<BinaryReader, T> read)
    {
        var values = new T[Places];

        for (var i = 0; i < Places; i++)
        {
            values[i] = read(reader);
        }

        return values;
    }

    public uint Id { get; }

    public ushort[] Chances { get; }

    /// <summary>The item of each place; the number of a kind of money for a place that gives money, zero for a place that gives nothing.</summary>
    public uint[] Items { get; }

    public ushort[] Least { get; }

    public ushort[] Most { get; }
}
