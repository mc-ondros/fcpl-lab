using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab06Wiring : ILab06Wiring
{
    public IItemStore CreateStore() => new SpinHistoryStore();

    public IItem CreateSample(int seed) => new Lab02Wiring().CreateSample(seed);

    public Type PublisherType => typeof(SpinHistoryStore);

    public Action Subscribe(IItemStore store, Action<IItem> handler)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(handler);

        var historyStore = (SpinHistoryStore)store;
        historyStore.ItemAdded += handler;
        return () => historyStore.ItemAdded -= handler;
    }

    public IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(keep);

        var result = new List<IItem>();
        foreach (var item in (SpinHistoryStore)store)
        {
            if (keep(item))
                result.Add(item);
        }

        return result;
    }

    public IReadOnlyList<string> Map(IItemStore store, Func<IItem, string> describe)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(describe);

        var result = new List<string>();
        foreach (var item in (SpinHistoryStore)store)
            result.Add(describe(item));

        return result;
    }

    public void ForEach(IItemStore store, Action<IItem> action)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in (SpinHistoryStore)store)
            action(item);
    }
}

