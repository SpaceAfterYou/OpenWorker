namespace OpenWorker.Hotspot;

/// <summary>
/// A list that is walked in place while its items step, without a copy: an item that leaves during a step is skipped
/// and dropped after the step, and an item that comes during a step starts with the next one. For the world loop only.
/// </summary>
/// <example>
/// <code>
/// var count = list.BeginStep();
///
/// try
/// {
///     for (var index = 0; index &lt; count; index++)
///     {
///         if (list.At(index) is { } item) item.Step();
///     }
/// }
/// finally
/// {
///     list.EndStep();
/// }
/// </code>
/// </example>
public sealed class StepList<T> where T : class
{
    private readonly List<T?> _items = [];
    private int _stepping;
    private int _holes;

    /// <summary>
    /// The items in the list, the ones that left during the step not counted.
    /// </summary>
    public int Count { get; private set; }

    public void Add(T item)
    {
        _items.Add(item);
        Count++;
    }

    public bool Contains(T item)
    {
        return _items.IndexOf(item) >= 0;
    }

    public bool Remove(T item)
    {
        var index = _items.IndexOf(item);

        if (index < 0)
        {
            return false;
        }

        if (_stepping > 0)
        {
            _items[index] = null;
            _holes++;
        }
        else
        {
            _items.RemoveAt(index);
        }

        Count--;

        return true;
    }

    public void Clear()
    {
        if (_stepping > 0)
        {
            for (var index = 0; index < _items.Count; index++)
            {
                if (_items[index] is not null)
                {
                    _items[index] = null;
                    _holes++;
                }
            }
        }
        else
        {
            _items.Clear();
        }

        Count = 0;
    }

    /// <summary>
    /// Starts a step and says how many places it walks: the items that come from now on are past them.
    /// </summary>
    public int BeginStep()
    {
        _stepping++;

        return _items.Count;
    }

    /// <summary>
    /// The item at a place of the step, or nothing when it has left.
    /// </summary>
    public T? At(int index)
    {
        return _items[index];
    }

    /// <summary>
    /// Ends the step, and drops the places of the items that left during it.
    /// </summary>
    public void EndStep()
    {
        if (--_stepping > 0 || _holes == 0)
        {
            return;
        }

        _items.RemoveAll(static e => e is null);
        _holes = 0;
    }
}
