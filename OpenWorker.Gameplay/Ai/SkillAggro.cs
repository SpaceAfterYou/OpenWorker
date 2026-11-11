using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Ai;

/// <summary>
/// How much a blow angers the monster at the one who dealt it. The hate grows with the damage, more with a skill that is made
/// to draw it (the table of the server says which), and a little more with the player whose level is the lowest in the group.
/// </summary>
public sealed class SkillAggro(ReadOnlyCollection<SkillAggroRow> rows)
{
    /// <summary>
    /// The share of the damage a blow adds to the hate, for each half it counts twice.
    /// </summary>
    private const float DamageShare = 0.0002f;

    /// <summary>
    /// The share of the damage a poison or a burn adds.
    /// </summary>
    private const float OverTimeShare = 0.0001f;

    private const float Percent = 0.01f;

    /// <summary>
    /// What the hate is multiplied by more for each player, from the lowest level of the group to the highest. A group has four
    /// players at most.
    /// </summary>
    private static readonly float[] LevelRates = [1.5f, 1.4f, 1.3f, 1.2f];

    /// <summary>
    /// Without the table of the server: the hate of a blow is the damage and the level of the player alone.
    /// </summary>
    public static SkillAggro None { get; } = new(new ReadOnlyCollection<SkillAggroRow>([]));

    private readonly RowIndex<uint, SkillAggroRow> _rows = rows.IndexBy(e => e.Id);

    /// <summary>
    /// The hate of one blow of a skill. <paramref name="order"/> counts the players of the group that are of a lower level than
    /// the one who dealt it.
    /// </summary>
    public float Hate(int skill, int damage, int order)
    {
        if (order is < 0 or >= 4)
        {
            return 0f;
        }

        var extra = 0f;

        if (skill > 0 && _rows.TryGet((uint)skill, out var row))
        {
            extra = row.Kind != 0 ? damage * row.Value * Percent : row.Value;
        }

        var hate = (damage + damage + extra) * DamageShare;

        return hate + hate * LevelRates[order];
    }

    /// <summary>
    /// The hate of a blow that has no skill, such as a burn.
    /// </summary>
    public static float OverTime(int damage) => damage * OverTimeShare;
}
