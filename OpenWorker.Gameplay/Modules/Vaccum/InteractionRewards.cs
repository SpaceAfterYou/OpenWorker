using System.Collections.ObjectModel;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Gameplay.Modules.Vaccum;

/// <summary>
/// What cubes ask and give, from the tables of the server: the object of a cube says how long its click is held, how many picks
/// it has and which row of the reward table a pick rolls on. Without the tables a cube gives nothing.
/// </summary>
public sealed class InteractionRewards(ReadOnlyCollection<CubeObjectRow> objects, ReadOnlyCollection<CubeRewardRow> rewards) : IVaccumRewards
{
    /// <summary>
    /// A roll goes from one to this.
    /// </summary>
    public const int Chances = 10_000;

    /// <summary>
    /// The place of the row has a chance but names no item.
    /// </summary>
    public const ushort NoItemInPlace = 52004;

    private readonly RowIndex<uint, CubeObjectRow> _objects = objects.IndexBy(e => e.Id);
    private readonly RowIndex<uint, CubeRewardRow> _rewards = rewards.IndexBy(e => e.Id);

    public VaccumCubeRule? RuleOf(int interaction)
    {
        if (interaction < 0 || !_objects.TryGet((uint)interaction, out var row) || row.Kind != CubeObjectRow.CubeKind)
        {
            return null;
        }

        return new VaccumCubeRule(TimeSpan.FromMilliseconds(row.HoldMilliseconds), row.MinPicks, row.MaxPicks);
    }

    public VaccumPick Roll(int interaction) => Roll(interaction, Random.Shared);

    /// <summary>
    /// One roll on the row of the cube: the chances of the places are taken off a number from one to ten thousand in turn, and the
    /// place that brings it to zero gives its item, as many as a number between its least and its most. A roll past the last
    /// place gives nothing, and so does an object or a row the tables lack.
    /// </summary>
    public VaccumPick Roll(int interaction, Random random)
    {
        if (interaction < 0 || !_objects.TryGet((uint)interaction, out var row) || !_rewards.TryGet(row.RewardTable, out var table))
        {
            return default;
        }

        var roll = random.Next(1, Chances + 1);

        for (var place = 0; place < CubeRewardRow.Places; place++)
        {
            roll -= table.Chances[place];

            if (roll > 0)
            {
                continue;
            }

            if (table.Items[place] == 0)
            {
                return new VaccumPick(0, 0, NoItemInPlace);
            }

            var least = table.Least[place];
            var most = Math.Max(least, table.Most[place]);

            return new VaccumPick((int)table.Items[place], (short)Math.Min(random.Next(least, most + 1), short.MaxValue), 0);
        }

        return default;
    }
}
