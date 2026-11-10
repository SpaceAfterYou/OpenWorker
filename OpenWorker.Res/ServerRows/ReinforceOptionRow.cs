using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The options an item gets from its upgrade, from the server's own <c>tb_Reinforce_Option</c>: up to five, each from the level of
/// upgrade it begins at, as a class (1 a stat effect, 2 the effect of a skill), the effect and its value.
/// </summary>
[BinaryResourceTable("tb_Reinforce_Option", Optional = true)]
public readonly struct ReinforceOptionRow
{
    public const int Options = 5;

    private readonly byte[] _phases;
    private readonly byte[] _classes;
    private readonly ushort[] _effects;
    private readonly uint[] _values;

    public ReinforceOptionRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();

        // How many levels of upgrade the table is for.
        reader.ReadByte();

        _phases = reader.ReadBytes(Options);
        _classes = reader.ReadBytes(Options);
        _effects = new ushort[Options];
        _values = new uint[Options];

        for (var i = 0; i < Options; i++)
        {
            _effects[i] = reader.ReadUInt16();
        }

        for (var i = 0; i < Options; i++)
        {
            _values[i] = reader.ReadUInt32();
        }
    }

    public ReinforceOptionRow(int id, (byte Phase, byte Class, ushort Effect, uint Value)[] options)
    {
        Id = id;
        _phases = new byte[Options];
        _classes = new byte[Options];
        _effects = new ushort[Options];
        _values = new uint[Options];

        for (var i = 0; i < Math.Min(options.Length, Options); i++)
        {
            (_phases[i], _classes[i], _effects[i], _values[i]) = options[i];
        }
    }

    public int Id { get; }

    /// <summary>
    /// The option of a place: the level of upgrade it begins at (zero for no option), its class, the effect it names and its value.
    /// </summary>
    public (byte Phase, byte Class, ushort Effect, uint Value) Option(int index)
    {
        return index is >= 0 and < Options && _phases is not null ? (_phases[index], _classes[index], _effects[index], _values[index]) : default;
    }
}
