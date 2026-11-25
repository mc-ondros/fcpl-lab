using Capstone.Core.Collections;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab07Wiring : ILab07Wiring
{
    public IItemStore CreateStore() => new SpinHistoryStore();

    public IItem CreateSample(int seed) => new Lab02Wiring().CreateSample(seed);

    public Type ExtensionHost => typeof(ItemQueryExtensions);

    public IEnumerable<string> Names(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return ((SpinHistoryStore)store).Select(i => i.DisplayName);
    }

    public IReadOnlyList<IItem> Sorted(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<IItem> Page(IEnumerable<IItem> items, int skip, int take)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegative(take);

        return items
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<IItem> Longest(IEnumerable<IItem> items, int n) => items.Longest(n);

    public StoreStats Summary(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var list = items.ToList();
        if (list.Count == 0)
            return new StoreStats(0, 0, null, 0);

        int distinctInitials = list
            .Where(i => !string.IsNullOrEmpty(i.DisplayName))
            .Select(i => char.ToUpperInvariant(i.DisplayName[0]))
            .Distinct()
            .Count();

        string? longestName = list.Longest(1).FirstOrDefault()?.DisplayName;
        double averageLength = list.Average(i => i.DisplayName.Length);

        return new StoreStats(list.Count, distinctInitials, longestName, averageLength);
    }
}

