using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// How many sockets a new item has open, from the server's own <c>tb_Socket</c>: those open from the start and up to three more,
/// each by its chance out of ten thousand. The client's table of the same name has neither the extra sockets nor their chances.
/// </summary>
[BinaryResourceTable("tb_Socket", Server = true)]
public readonly struct SocketChanceRow
{
    /// <summary>The extra sockets a row can have.</summary>
    public const int Chances = 3;

    // The costs of setting, removing and opening a socket, and what taking a gem out gives: these are read from the client's
    // table, so that the player pays what the client shows.
    private const int Costs = 3 * (2 * sizeof(int) + sizeof(short)) + sizeof(int) + sizeof(short);

    public SocketChanceRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        reader.ReadByte();
        MaxSockets = reader.ReadByte();
        FixedSockets = reader.ReadByte();
        ExtraSockets = reader.ReadByte();

        ExtraChances = new ushort[Chances];

        for (var i = 0; i < Chances; i++)
        {
            ExtraChances[i] = reader.ReadUInt16();
        }

        reader.ReadBytes(Costs);
    }

    public int Id { get; }

    public byte MaxSockets { get; }

    public byte FixedSockets { get; }

    /// <summary>How many more sockets may be open, each by its own chance.</summary>
    public byte ExtraSockets { get; }

    public ushort[] ExtraChances { get; }
}
