using Capstone.Core.Collections;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab05Wiring : ILab05Wiring
{
    public IItemStore CreateStore() => new SpinHistoryStore();

    public IItem CreateSample(int seed) => new Lab02Wiring().CreateSample(seed);

    public Type CollectionType => typeof(SpinHistoryStore);

    public Type GenericDefinition => typeof(RecentlyUsed<>);

    public IItem At(IItemStore store, int index) => ((SpinHistoryStore)store)[index];

    public IItem? ById(IItemStore store, string id) => ((SpinHistoryStore)store)[id];

    public IReadOnlyList<IItem> EachInOrder(IItemStore store)
    {
        var list = new List<IItem>();
        foreach (var item in (SpinHistoryStore)store)
            list.Add(item);
        return list;
    }

    public IEnumerable<IItem> Matching(IItemStore store, string text) =>
        ((SpinHistoryStore)store).Matching(text);

    public IReadOnlyList<T> Recent<T>(int capacity, params T[] pushed)
    {
        var recent = new RecentlyUsed<T>(capacity);
        foreach (var value in pushed)
            recent.Push(value);
        return recent;
    }
}

