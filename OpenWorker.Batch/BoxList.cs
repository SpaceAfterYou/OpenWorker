using System.Collections;
using System.Diagnostics.CodeAnalysis;
using OpenWorker.Batch.Entities.Basic;

namespace OpenWorker.Batch;

/// <summary>
/// The boxes of one kind of a map, in the order of the file, with an index by id built when the map is read. A map can
/// give two boxes one id; the first one is found, as a search through the list would find it.
/// </summary>
public sealed class BoxList<T> : IReadOnlyList<T> where T : BasicEntity
{
    private readonly T[] _boxes;
    private readonly Dictionary<int, T> _byId;

    public BoxList(IEnumerable<T> boxes)
    {
        _boxes = boxes.ToArray();
        _byId = new Dictionary<int, T>(_boxes.Length);

        foreach (var box in _boxes)
        {
            _byId.TryAdd(box.Id, box);
        }
    }

    public int Count => _boxes.Length;

    public T this[int index] => _boxes[index];

    /// <summary>
    /// The box with the id, or nothing when the map has none: an id from a client or a script may name any box.
    /// </summary>
    public T? Find(int id)
    {
        return _byId.GetValueOrDefault(id);
    }

    public bool TryGet(int id, [NotNullWhen(true)] out T? box)
    {
        return _byId.TryGetValue(id, out box);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_boxes).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
