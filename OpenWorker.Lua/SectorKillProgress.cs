using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Lua;

/// <summary>
/// How far the fight in a sector has gone: a share of its monsters dead opens spawn steps and finishes the sector.
/// </summary>
public sealed class SectorKillProgress(ClearType clearType, int clearKillRatio, IReadOnlyList<byte> stepKillRatios)
{
    public SectorKillProgress(SectorBox box) : this(box.ClearType, box.ClearKillRatio, box.ConditionKillRatioSteps)
    {
    }

    private int Alive { get; set; }
    private int Total { get; set; }
    private HashSet<int> SpawnedSteps { get; } = [];

    /// <summary>
    /// Counted from zero. Spawn boxes count from one, so this one spawns the boxes of <c>Step + 1</c>.
    /// </summary>
    public int Step { get; private set; }

    public bool Stopped { get; set; }

    public bool IsCurrentStepSpawned => SpawnedSteps.Contains(Step);

    public float KilledPercent => Total == 0 ? 0f : 100f - (float)Alive / Total * 100f;

    public void Added()
    {
        Alive++;
        Total++;
    }

    public void Removed()
    {
        Alive--;
    }

    public void MarkCurrentStepSpawned()
    {
        SpawnedSteps.Add(Step);
    }

    /// <summary>
    /// A step opens once when the share of the dead reaches its ratio.
    /// </summary>
    public bool TryOpenNextStep()
    {
        if (Stopped || !IsCurrentStepSpawned || Step >= stepKillRatios.Count)
        {
            return false;
        }

        var ratio = stepKillRatios[Step];

        if (ratio == 0 || KilledPercent < ratio)
        {
            return false;
        }

        Step++;

        return true;
    }

    /// <param name="boxesToCome">Spawn boxes of this sector that have not appeared yet and have to be killed too.</param>
    public bool IsCleared(int boxesToCome)
    {
        return clearType switch
        {
            ClearType.Kill => KilledPercent >= clearKillRatio,
            ClearType.KillPerpect => boxesToCome == 0 && Alive == 0,
            _ => false
        };
    }
}
