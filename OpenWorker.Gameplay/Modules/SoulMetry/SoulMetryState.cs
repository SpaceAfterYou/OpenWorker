using System.Text.Json.Serialization;
using OpenWorker.Hotspot.Modules.SoulMetry;

namespace OpenWorker.Gameplay.Modules.SoulMetry;

/// <summary>
/// The soul metries of a person: the ones being collected with the bits of the found objects, and a
/// bitmap of the finished ones. This is what is stored.
/// </summary>
public sealed class SoulMetryState
{
    public Dictionary<short, short> Progress { get; set; } = [];

    public byte[] Complete { get; set; } = new byte[SoulMetryModuleDefines.CompleteListByteLength];

    /// <summary>
    /// Writes of the state to the database go one after another, so the last one has the latest state.
    /// </summary>
    [JsonIgnore]
    public SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>
    /// Makes a state loaded from the database safe to use: a bitmap of the wrong size is resized and the
    /// soul metries the tables do not know are dropped.
    /// </summary>
    public void Normalize(SoulMetryCatalog catalog)
    {
        if (Complete.Length != SoulMetryModuleDefines.CompleteListByteLength)
        {
            var resized = new byte[SoulMetryModuleDefines.CompleteListByteLength];

            Array.Copy(Complete, resized, Math.Min(Complete.Length, resized.Length));

            Complete = resized;
        }

        foreach (var id in Progress.Keys.Where(e => !catalog.TryGet(e, out _)).ToArray())
        {
            Progress.Remove(id);
        }
    }

    /// <summary>
    /// Whether the bit of the soul metry is in the bitmap.
    /// </summary>
    public bool Fits(SoulMetryDefinition definition) =>
        definition.CompleteBit >= 0 && definition.CompleteBit / 8 < Complete.Length;

    public bool IsComplete(SoulMetryDefinition definition) =>
        Fits(definition) && (Complete[definition.CompleteBit / 8] & (1 << (definition.CompleteBit & 7))) != 0;

    /// <summary>
    /// False when the bit does not fit the bitmap.
    /// </summary>
    public bool TrySetComplete(SoulMetryDefinition definition)
    {
        if (!Fits(definition))
        {
            return false;
        }

        Complete[definition.CompleteBit / 8] |= (byte)(1 << (definition.CompleteBit & 7));

        return true;
    }

    /// <summary>
    /// Starts collecting a soul metry. Nothing happens for one that is finished or already started.
    /// </summary>
    public bool TryAdd(SoulMetryDefinition definition, out short bits)
    {
        bits = definition.InitialBits;

        if (IsComplete(definition) || !Progress.TryAdd(definition.Id, bits))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marks an object as found. False if the soul metry is not being collected, does not have the object
    /// or the object was found before.
    /// </summary>
    public bool TryCollect(SoulMetryDefinition definition, int obj, out short bits)
    {
        bits = 0;

        if (!Progress.TryGetValue(definition.Id, out var current))
        {
            return false;
        }

        for (var order = 0; order < definition.Objects.Count; order++)
        {
            if (definition.Objects[order].ObjectId != obj)
            {
                continue;
            }

            if ((current & (1 << order)) != 0)
            {
                return false;
            }

            bits = (short)((ushort)current | (1 << order));
            Progress[definition.Id] = bits;

            return true;
        }

        return false;
    }

    /// <summary>
    /// A soul metry can be finished once every object is found.
    /// </summary>
    public bool IsReady(SoulMetryDefinition definition) =>
        Progress.TryGetValue(definition.Id, out var current) && current == SoulMetryDefinition.AllFound;
}
