using System.Collections;

namespace Capstone.Core.Collections;

/// <summary>
/// Bounded, duplicate-free MRU (most-recently-used) generic collection.
/// </summary>
public sealed class RecentlyUsed<T> : IReadOnlyList<T>
{
    private readonly int _capacity;
    private readonly List<T> _items = [];

    public RecentlyUsed(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public int Count => _items.Count;

    public T this[int index] => _items[index];

    public void Push(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (comparer.Equals(_items[i], item))
                _items.RemoveAt(i);
        }

        _items.Insert(0, item);

        if (_items.Count > _capacity)
            _items.RemoveAt(_items.Count - 1);
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

