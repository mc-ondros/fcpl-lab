using SharpQuest.Contracts;

namespace Capstone.Core.Collections;

/// <summary>
/// LINQ extension methods for querying item sequences (Lab 07).
/// </summary>
public static class ItemQueryExtensions
{
    public static IReadOnlyList<IItem> Longest(this IEnumerable<IItem> items, int n)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        if (n == 0)
            return [];

        return items
            .OrderByDescending(i => i.DisplayName.Length)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .Take(n)
            .ToList();
    }
}

