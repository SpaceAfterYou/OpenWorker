using OpenWorker.Extensions;
using OpenWorker.Res.Attributes;

namespace OpenWorker.UpdateContent.Res.Rows;

/// <summary>
/// What an achievement counts, from the server's own <c>tb_Achievement</c>: the kind of event and its target (a monster, a
/// faction, a maze, a helper). The client's table has no target, so achievements of one kind with different targets cannot
/// be told apart without this one. The rest of the row (its place, its name, its count and its reward) is read from the
/// client's table, which says what the window shows.
/// </summary>
[BinaryResourceTable("tb_Achievement", Server = true)]
public readonly struct AchievementTargetRow
{
    public AchievementTargetRow(BinaryReader reader)
    {
        Id = reader.ReadInt32();

        // Category, group, order and the bit of the reward.
        reader.ReadBytes(sizeof(byte) + sizeof(short) + sizeof(byte) + sizeof(short));

        Type = reader.ReadUInt16();
        Target = reader.ReadInt32();

        reader.ReadUtf16UnicodeStringWithoutTerminator();

        // The count, gold, ether, battle points, two titles, two kinds of item, two items and two counts.
        reader.ReadBytes(6 * sizeof(int) + 2 * sizeof(byte) + 2 * sizeof(int) + 2 * sizeof(byte));
    }

    public int Id { get; }

    /// <summary>
    /// The kind of event the achievement counts.
    /// </summary>
    public ushort Type { get; }

    /// <summary>
    /// What the event is about, 0 for any.
    /// </summary>
    public int Target { get; }
}
