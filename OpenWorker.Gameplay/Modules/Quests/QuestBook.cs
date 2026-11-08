using System.Text.Json.Serialization;
using OpenWorker.Hotspot.Modules.Quests;

namespace OpenWorker.Gameplay.Modules.Quests;

/// <summary>
/// An episode the person has taken and where it stands.
/// </summary>
public sealed class QuestEpisodeState
{
    public int Episode { get; set; }

    /// <summary>
    /// Whether the helper of the person shows the episode.
    /// </summary>
    public bool Helper { get; set; }

    /// <summary>
    /// Whether the episode has failed. It stays until the client asks to remove it.
    /// </summary>
    public bool Failed { get; set; }

    /// <summary>
    /// A bit for each condition of the episode that is done.
    /// </summary>
    public short Done { get; set; }

    /// <summary>
    /// How many times each condition of the episode has been done so far.
    /// </summary>
    public byte[] Steps { get; set; } = new byte[QuestModuleDefines.ConditionsPerEpisode];

    public bool IsDone(int slot) => (Done & (1 << slot)) != 0;
}

/// <summary>
/// The quests of a person. This is what is stored.
/// </summary>
public sealed class QuestBook
{
    /// <summary>
    /// The episodes the person has taken and not finished.
    /// </summary>
    public List<QuestEpisodeState> Episodes { get; set; } = [];

    /// <summary>
    /// A bit for each episode the person has finished.
    /// </summary>
    public byte[] Finished { get; set; } = new byte[QuestModuleDefines.CompleteListByteLength];

    /// <summary>
    /// Writes of the book to the database go one after another, so the last one has the latest state.
    /// </summary>
    [JsonIgnore]
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public QuestEpisodeState? Find(int episode) => Episodes.Find(e => e.Episode == episode);

    /// <summary>
    /// How many episodes show in the helper of the person.
    /// </summary>
    public int HelperCount => Episodes.Count(e => e.Helper);

    public bool IsFinished(int bit)
    {
        var index = bit >> 3;

        return bit >= 0 && index < Finished.Length && (Finished[index] & (1 << (bit & 7))) != 0;
    }

    /// <summary>
    /// Marks an episode as finished or as not finished. False if the bit does not fit the list.
    /// </summary>
    public bool SetFinished(int bit, bool finished)
    {
        var index = bit >> 3;

        if (bit < 0 || index >= Finished.Length)
        {
            return false;
        }

        if (finished)
        {
            Finished[index] |= (byte)(1 << (bit & 7));
        }

        else
        {
            Finished[index] &= (byte)~(1 << (bit & 7));
        }

        return true;
    }

    public bool Remove(int episode) => Episodes.RemoveAll(e => e.Episode == episode) > 0;

    /// <summary>
    /// Makes a book that was stored fit the tables as they are now: episodes and conditions that no longer exist
    /// go, and the steps of an episode get the length the client expects.
    /// </summary>
    public void Normalize(QuestCatalog catalog)
    {
        if (Finished.Length != QuestModuleDefines.CompleteListByteLength)
        {
            var finished = new byte[QuestModuleDefines.CompleteListByteLength];
            Array.Copy(Finished, finished, Math.Min(Finished.Length, finished.Length));
            Finished = finished;
        }

        var seen = new HashSet<int>();

        Episodes.RemoveAll(e => !catalog.TryGetEpisode(e.Episode, out _) || !seen.Add(e.Episode));

        foreach (var episode in Episodes)
        {
            if (episode.Steps.Length != QuestModuleDefines.ConditionsPerEpisode)
            {
                var steps = new byte[QuestModuleDefines.ConditionsPerEpisode];
                Array.Copy(episode.Steps, steps, Math.Min(episode.Steps.Length, steps.Length));
                episode.Steps = steps;
            }

            if (catalog.TryGetEpisode(episode.Episode, out var definition))
            {
                // Bits of conditions the episode does not have can not be done.
                episode.Done &= definition.AllDone;
            }
        }
    }
}
