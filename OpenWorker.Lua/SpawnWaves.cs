using OpenWorker.Batch;
using OpenWorker.Batch.Entities;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Lua.Managers;

namespace OpenWorker.Lua;

/// <summary>
/// The spawn boxes that wait for a signal and then send their monsters in waves: the first wave after the delay of the box,
/// the others one sequence time apart, as many waves as the box says (one when it says none). A mode or a script starts a box;
/// what comes of a wave is made by <see cref="LuaCreatureManager.SpawnWave"/>.
/// </summary>
public sealed class SpawnWaves(LuaCreatureManager creatures, VBatchFile batch)
{
    /// <summary>The slots a box holds monsters in.</summary>
    private const int Slots = 10;

    /// <summary>A roll against the chance of a slot goes from one to this.</summary>
    private const int Chances = 10_000;

    /// <summary>A mode counts the waves done a second after the last one has appeared, so that the last monsters are on the field.</summary>
    private const float Settling = 1f;

    private sealed class Run(MonsterSpawnBox box, int waves, float wait)
    {
        public MonsterSpawnBox Box { get; } = box;
        public int Left { get; set; } = waves;
        public float Wait { get; set; } = wait;

        /// <summary>Where the next wave of a box that sends one slot at a time starts looking.</summary>
        public int Order { get; set; }
    }

    private readonly List<Run> _running = [];
    private readonly HashSet<int> _started = [];

    /// <summary>
    /// Whether a box is waiting to send a wave.
    /// </summary>
    public bool IsRunning => _running.Count > 0;

    /// <summary>
    /// The box that waits for a signal, or nothing for a box the map lacks and for one that appears by itself.
    /// </summary>
    private MonsterSpawnBox? Find(int box)
    {
        return batch.EventBox.MonsterSpawns.Find(box) is { CreationCondition: CreationConditionType.WaitSignal } found ? found : null;
    }

    /// <summary>
    /// How long the waves of a box take from its signal to the appearance of its last monsters, a second more. Zero for a box
    /// that sends none.
    /// </summary>
    public float Duration(int box)
    {
        return Find(box) is { } spawn ? Duration(spawn) : 0f;
    }

    private static float Duration(MonsterSpawnBox spawn)
    {
        var waves = Math.Max(1, (int)spawn.WaitCreationMaxWave);

        return spawn.WaitCreationDelayTime + spawn.WaitCreationSequenceTime * (waves - 1) + Settling;
    }

    /// <summary>
    /// The signal of a box. A box the map has not is ignored. A box that has been started is started again only by a script
    /// (<paramref name="byScript"/>); a mode starts each box once.
    /// </summary>
    public bool Start(int box, bool byScript)
    {
        if (Find(box) is not { } spawn || (!byScript && !_started.Add(box)))
        {
            return false;
        }

        _started.Add(box);
        _running.RemoveAll(e => e.Box.Id == box);

        var wait = spawn.WaitCreationDelayTime > 0f ? spawn.WaitCreationDelayTime : spawn.WaitCreationSequenceTime;

        _running.Add(new Run(spawn, Math.Max(1, (int)spawn.WaitCreationMaxWave), Sane(wait)));

        return true;
    }

    /// <summary>
    /// Counts the time down for every box that has started and sends the waves that are due.
    /// </summary>
    public void Step(float seconds)
    {
        if (_running.Count == 0)
        {
            return;
        }

        foreach (var run in _running.ToArray())
        {
            run.Wait -= seconds;

            if (run.Wait > 0f)
            {
                continue;
            }

            // A box that was told to stop sends nothing more.
            if (creatures.HasAppeared(run.Box.Id))
            {
                _running.Remove(run);

                continue;
            }

            run.Left--;

            creatures.SpawnWave(run.Box, Pick(run), last: run.Left <= 0);

            if (run.Left <= 0)
            {
                _running.Remove(run);
            }
            else
            {
                run.Wait = Sane(run.Box.WaitCreationSequenceTime);
            }
        }
    }

    private static float Sane(float seconds) => float.IsFinite(seconds) ? Math.Clamp(seconds, 0f, 86_400f) : 0f;

    private int Roll() => creatures.Random.Next(1, Chances + 1);

    /// <summary>
    /// The slots a wave makes monsters of. A box that sends everything rolls the chance of every slot; one that sends one slot at a
    /// time takes the first that rolls well, starting after the one it took last; one that sends only one takes the slot a single
    /// roll falls in.
    /// </summary>
    private List<int> Pick(Run run)
    {
        var box = run.Box;
        var picked = new List<int>();
        var oneByOne = box.WaitCreationSequenceType == WaitCreationSequenceType.OneByOne;
        var onlyOne = box.WaitCreationSequenceType == WaitCreationSequenceType.OnlyOne;
        var roll = onlyOne ? Roll() : 0;
        var found = false;

        for (var slot = oneByOne ? run.Order : 0; slot < Slots && slot < box.MonsterList.Count; slot++)
        {
            var monster = box.MonsterList[slot];

            if (monster.Id is 0)
            {
                continue;
            }

            if (onlyOne)
            {
                if (found)
                {
                    break;
                }

                roll -= monster.Chance;
                found = roll <= 0;
            }
            else
            {
                found = Roll() <= monster.Chance;
            }

            if (!found)
            {
                continue;
            }

            picked.Add(slot);

            if (oneByOne)
            {
                run.Order = slot + 1 >= Slots ? 0 : slot + 1;

                break;
            }
        }

        return picked;
    }
}
