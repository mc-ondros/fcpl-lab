using System.Collections;
using SharpQuest.Contracts;

namespace Capstone.Core.Storage;

/// <summary>
/// In-memory repository of recorded spins and slot items.
/// Preserves insertion order, enforces unique case-sensitive Ids,
/// acts as an enumerable collection with indexers, and publishes add/remove events.
/// </summary>
public sealed class SpinHistoryStore : IItemStore, IEnumerable<IItem>
{
    private readonly List<IItem> _ordered = [];
    private readonly Dictionary<string, IItem> _byId = new(StringComparer.Ordinal);

    public event Action<IItem>? ItemAdded;

    public event Action<IItem>? ItemRemoved;

    public int Count => _ordered.Count;

    public IItem this[int index] => _ordered[index];

    public IItem? this[string id] => _byId.GetValueOrDefault(id);

    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!_byId.TryAdd(item.Id, item))
            throw new ArgumentException($"A spin with Id '{item.Id}' already exists in the store.", nameof(item));

        _ordered.Add(item);
        ItemAdded?.Invoke(item);
    }

    public IItem? FindById(string id) => _byId.GetValueOrDefault(id);

    public IReadOnlyList<IItem> All() => _ordered.ToArray();

    public bool Remove(string id)
    {
        if (!_byId.Remove(id, out var item))
            return false;

        _ordered.Remove(item);
        ItemRemoved?.Invoke(item);
        return true;
    }

    public IEnumerable<IItem> Matching(string text)
    {
        foreach (var item in _ordered)
        {
            if (string.IsNullOrEmpty(text) ||
                item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                yield return item;
            }
        }
    }

    public IEnumerator<IItem> GetEnumerator()
    {
        foreach (var item in _ordered)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
