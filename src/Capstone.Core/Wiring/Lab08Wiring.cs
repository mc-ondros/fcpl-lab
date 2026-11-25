using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public sealed class Lab08Wiring : ILab08Wiring
{
    private readonly Lab04Wiring _lab04 = new();

    public IItemStore CreateStore() => new SpinHistoryStore();

    public IItem CreateSample(int seed) => new Lab02Wiring().CreateSample(seed);

    public IReadOnlyList<Type> KindTypes => _lab04.KindTypes;

    public IItem CreateOfKind(int kindIndex, int seed) => _lab04.CreateOfKind(kindIndex, seed);

    public Task SaveAsync(IItemStore store, string path) => StoreJsonStorage.SaveAsync(store, path);

    public Task<IItemStore> LoadAsync(string path) => StoreJsonStorage.LoadAsync(path);
}

