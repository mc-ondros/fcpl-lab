using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab04Wiring : ILab04Wiring
{
    public IItemStore CreateStore() => new SpinHistoryStore();

    public Type BaseType => typeof(Bonus);

    public IReadOnlyList<Type> KindTypes { get; } = [typeof(FreeSpinBonus), typeof(CreditBonus)];

    public IItem CreateOfKind(int kindIndex, int seed) => kindIndex switch
    {
        0 => new FreeSpinBonus
        {
            Id = $"bonus-free-{seed}",
            Reason = $"Lucky Spin #{seed}",
            Spins = 1 + (Math.Abs(seed) % 5),
        },
        1 => new CreditBonus
        {
            Id = $"bonus-credit-{seed}",
            Reason = $"Lucky Credit #{seed}",
            Credits = 10 + (Math.Abs(seed) % 10) * 5,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(kindIndex), kindIndex, "Unknown bonus kind."),
    };

    public string Describe(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item is Bonus bonus
            ? bonus.Describe()
            : item.DisplayName;
    }

    public IReadOnlyList<string> DescribeAll(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var result = new List<string>();
        foreach (var item in store.All())
        {
            if (item is Bonus bonus)
                result.Add(bonus.Describe());
            else
                result.Add(item.DisplayName);
        }

        return result;
    }

    public string Classify(IItem item) => item switch
    {
        null => throw new ArgumentNullException(nameof(item)),
        FreeSpinBonus => "Free Spin Bonus",
        CreditBonus => "Credit Bonus",
        _ => "Foreign Item",
    };

    public bool TryFindByName(IItemStore store, string name, out IItem? found)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(name))
        {
            found = null;
            return false;
        }

        foreach (var item in store.All())
        {
            if (string.Equals(item.DisplayName, name, StringComparison.Ordinal))
            {
                found = item;
                return true;
            }
        }

        found = null;
        return false;
    }

    public int AddAll(IItemStore store, params IItem[] items)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(items);

        int count = 0;
        foreach (var item in items)
        {
            store.Add(item);
            count++;
        }

        return count;
    }
}

