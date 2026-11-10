using OpenWorker.Batch;
using OpenWorker.Domain.Batch.Enums;
using OpenWorker.Hotspot.Modules.Maze.Types;
using OpenWorker.Res;
using OpenWorker.UpdateContent.Res.Rows;

namespace OpenWorker.Lua;

/// <summary>
/// What has changed in a maze since it started: which sectors are cleared and what is open.
/// </summary>
public sealed class MazeProgress(VBatchFile batch, RowIndex<int, InteractionObjectRow> interactions)
{
    private Dictionary<int, bool> Portals { get; } = batch.EventBox.Portals
        .DistinctBy(e => e.Id)
        .ToDictionary(e => e.Id, e => e.PortalState == PortalState.Enable);

    private Dictionary<int, bool> Gates { get; } = batch.EventBox.ServerGates
        .DistinctBy(e => e.Id)
        .ToDictionary(e => e.Id, e => e.Type == default);

    private Dictionary<int, MazeInteractionValue> Interactions { get; } = InteractionsOf(batch, interactions);

    /// <summary>
    /// The interaction objects only the player who uses them sees change; the others are shown to everyone.
    /// </summary>
    private HashSet<int> PrivateInteractions { get; } = batch.EventBox.InterActions
        .DistinctBy(e => e.Id)
        .Where(e => interactions.TryGet(e.Interaction, out var row) && row.PrivateActivate == 1)
        .Select(e => e.Id)
        .ToHashSet();

    /// <summary>
    /// The interaction objects whose use counts for the quests of everyone in the maze, not only of the player who uses them.
    /// </summary>
    private HashSet<int> PublicQuestInteractions { get; } = batch.EventBox.InterActions
        .DistinctBy(e => e.Id)
        .Where(e => interactions.TryGet(e.Interaction, out var row) && row.PublicQuestCheck == 1)
        .Select(e => e.Id)
        .ToHashSet();

    /// <summary>
    /// Whether the use of the interaction object counts for everyone in the maze.
    /// </summary>
    public bool CountsForEveryone(int box) => PublicQuestInteractions.Contains(box);

    /// <summary>
    /// Whether using the interaction object changes it for the player who uses it alone.
    /// </summary>
    public bool IsPrivate(int box) => PrivateInteractions.Contains(box);

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

    /// <summary>
    /// The interaction objects of the map that the table knows, by their box, in the order of the map.
    /// </summary>
    private static Dictionary<int, MazeInteractionValue> InteractionsOf(VBatchFile batch, RowIndex<int, InteractionObjectRow> interactions)
    {
        var result = new Dictionary<int, MazeInteractionValue>();

        foreach (var box in batch.EventBox.InterActions.DistinctBy(e => e.Id))
        {
            if (interactions.TryGet(box.Interaction, out var row))
            {
                result.Add(box.Id, new MazeInteractionValue(row.ShowSight == 1, row.EnableState == 1, box.Id, row.CountMax));
            }
        }

        return result;
    }

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
