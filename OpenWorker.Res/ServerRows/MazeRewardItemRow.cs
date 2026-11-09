using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// The items a maze rewards at its end, by maze. Not in the client files, the server tables have it.
/// </summary>
/// <remarks>
/// Only the key reward is used: a random box rolled once when the maze is finished, opened with keys from the window of
/// the maze. The reward by rank and the second key reward are read to reach the end of the row.
/// </remarks>
[BinaryResourceTable("tb_MazeReward_Item", Optional = true)]
public readonly struct MazeRewardItemRow(BinaryReader reader)
{
    public const int RankCount = 6;

    public uint Maze { get; } = reader.ReadUInt32();

    private readonly ushort _monsterRate = reader.ReadUInt16();
    private readonly uint _box = reader.ReadUInt32();
    private readonly uint[] _boxByRank = Enumerable.Range(0, RankCount).Select(_ => reader.ReadUInt32()).ToArray();
    private readonly ushort[] _rateByRank = Enumerable.Range(0, RankCount).Select(_ => reader.ReadUInt16()).ToArray();
    private readonly uint[] _minByRank = Enumerable.Range(0, RankCount).Select(_ => reader.ReadUInt32()).ToArray();
    private readonly uint[] _maxByRank = Enumerable.Range(0, RankCount).Select(_ => reader.ReadUInt32()).ToArray();

    /// <summary>
    /// The random box of <c>tb_Item_RandomBox</c> the key reward is rolled from; 0 for none.
    /// </summary>
    public uint KeyBox { get; } = reader.ReadUInt32();

    private readonly uint _secondKeyBox = reader.ReadUInt32();

    /// <summary>
    /// How many keys open the key reward.
    /// </summary>
    public byte Keys { get; } = reader.ReadByte();

    private readonly byte _secondKeys = reader.ReadByte();
}
