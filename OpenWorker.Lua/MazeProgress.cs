using OpenWorker.Batch;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// What has changed in a maze since it started: which sectors are cleared and what is open.
/// </summary>
public sealed class MazeProgress(VBatchFile batch, IReadOnlyCollection<InteractionObjectRow> interactions)
{
    private Dictionary<int, bool> Portals { get; } = batch.EventBox.Portals
        .DistinctBy(e => e.Id)
        .ToDictionary(e => e.Id, e => e.PortalState == PortalState.Enable);

    private Dictionary<int, bool> Gates { get; } = batch.EventBox.ServerGates
        .DistinctBy(e => e.Id)
        .ToDictionary(e => e.Id, e => e.Type == default);

    private Dictionary<int, MazeInteractionValue> Interactions { get; } = batch.EventBox.InterActions
        .DistinctBy(e => e.Id)
        .Join(
            interactions,
            box => box.Interaction,
            row => row.Id,
            (box, row) => new MazeInteractionValue(row.ShowSight == 1, row.EnableState == 1, box.Id, row.CountMax))
        .ToDictionary(e => e.Box);

    /// <summary>
    /// Some sectors are cleared from the start, they have nothing to clear.
    /// </summary>
    private HashSet<int> ClearedSectors { get; } = batch.EventBox.Sectors
        .Where(e => e.ClearType == ClearType.Always)
        .Select(e => e.Id)
        .ToHashSet();

    private HashSet<int> UsedBoxes { get; } = [];

    public IReadOnlyCollection<int> Cleared => ClearedSectors;
    public IReadOnlyCollection<int> OpenGates => Gates.Where(e => e.Value).Select(e => e.Key).ToArray();
    public IReadOnlyCollection<int> OpenPortals => Portals.Where(e => e.Value).Select(e => e.Key).ToArray();
    public IReadOnlyCollection<int> ClosedPortals => Portals.Where(e => !e.Value).Select(e => e.Key).ToArray();
    public IReadOnlyList<MazeInteractionValue> InteractionList => Interactions.Values.ToArray();

    public bool IsSectorCleared(int sector)
    {
        return ClearedSectors.Contains(sector);
    }

    public bool ClearSector(int sector)
    {
        return ClearedSectors.Add(sector);
    }

    public bool IsPortalOpen(int portal)
    {
        return Portals.GetValueOrDefault(portal);
    }

    /// <summary>
    /// False when there is no such portal or it is already in this state.
    /// </summary>
    public bool SetPortal(int portal, bool open)
    {
        return Set(Portals, portal, open);
    }

    public bool SetGate(int gate, bool open)
    {
        return Set(Gates, gate, open);
    }

    public bool TryGetInteraction(int box, out MazeInteractionValue value)
    {
        return Interactions.TryGetValue(box, out value);
    }

    /// <summary>
    /// The object is not there for the person: it is not shown and cannot be used.
    /// </summary>
    public void HideInteraction(int box)
    {
        if (Interactions.TryGetValue(box, out var value))
        {
            Interactions[box] = value with { Show = false, Enable = false };
        }
    }

    public MazeInteractionValue? SetInteraction(int box, bool enable)
    {
        if (!Interactions.TryGetValue(box, out var value))
        {
            return null;
        }

        return Interactions[box] = value with { Enable = enable };
    }

    /// <summary>
    /// An interaction with a limited number of uses turns off after the last one.
    /// </summary>
    public MazeInteractionValue UseInteraction(MazeInteractionValue value)
    {
        if (value.CallCount <= 0)
        {
            return value;
        }

        var left = value.CallCount - 1;

        return Interactions[value.Box] = value with { CallCount = left, Enable = left > 0 };
    }

    /// <summary>
    /// A box the client enters works once: true only the first time.
    /// </summary>
    public bool TryUseBox(int box)
    {
        return UsedBoxes.Add(box);
    }

    private static bool Set(Dictionary<int, bool> states, int key, bool value)
    {
        if (!states.TryGetValue(key, out var current) || current == value)
        {
            return false;
        }

        states[key] = value;

        return true;
    }
}
