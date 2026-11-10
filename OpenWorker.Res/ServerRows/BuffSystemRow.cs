using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What the server's own <c>tb_Buff</c> says of a buff that the client's does not: its system type. 0 is an ordinary buff, 1 one
/// that is not given to a boss, 2 and 3 are not held back by resistances and always stick, 3 also gets through immunity. The
/// rest of the row is read to get to the end of it; the kind, effect and group are kept so that a row that has changed between
/// the two versions of the table is not taken for the same buff.
/// </summary>
[BinaryResourceTable("tb_Buff", Server = true, Optional = true)]
public readonly struct BuffSystemRow
{
    public BuffSystemRow(BinaryReader reader)
    {
        Id = reader.ReadUInt16();

        // Where it may be given.
        reader.ReadByte();

        SystemType = reader.ReadByte();
        Kind = reader.ReadByte();
        Effect = reader.ReadUInt16();
        Group = reader.ReadUInt16();

        // The chance, the grade, the duration, the stand-by time, the tick, how a second one stacks, how many, and what takes it away.
        reader.ReadBytes(sizeof(int) + sizeof(byte) + 3 * sizeof(uint) + 3 * sizeof(byte));

        // The animation it plays.
        reader.ReadBytes(reader.ReadUInt16() * sizeof(char));

        // The buff and the skill that follow it, the buff a hit calls and the buff a full stack turns into.
        reader.ReadBytes(sizeof(ushort) + sizeof(uint) + 2 * sizeof(ushort));

        // Three stats and their values, then how the buff is sent.
        reader.ReadBytes(3 * (sizeof(byte) + sizeof(int)) + sizeof(byte));
    }

    public ushort Id { get; }

    public byte SystemType { get; }

    public byte Kind { get; }

    public ushort Effect { get; }

    public ushort Group { get; }
}
