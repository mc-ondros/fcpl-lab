using System.Collections;
using Capstone.Core.Model;

namespace Capstone.Core.Collections;

/// <summary>
/// The machine's record of every spin in one session.
/// A real collection: an indexer to reach a spin by position,
/// an iterator so it can be walked with foreach, and a generic
/// method that filters it by a predicate the caller supplies.
/// </summary>
public sealed class SpinLog : IEnumerable<Spin>
{
    private readonly List<Spin> _spins = [];

    public int Count => _spins.Count;

    /// <summary>The spin at <paramref name="index"/>, in the order it was played.</summary>
    public Spin this[int index] => _spins[index];

    public void Add(Spin spin)
    {
        ArgumentNullException.ThrowIfNull(spin);
        _spins.Add(spin);
    }

    /// <summary>
    /// A generic method: the predicate is a function passed as a parameter,
    /// and the result is a new log holding only the spins it accepts.
    /// </summary>
    public SpinLog Where(Func<Spin, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var filtered = new SpinLog();
        foreach (var spin in _spins)
        {
            if (predicate(spin))
                filtered.Add(spin);
        }

        return filtered;
    }

    /// <summary>The iterator: foreach and LINQ both walk the log through this.</summary>
    public IEnumerator<Spin> GetEnumerator()
    {
        foreach (var spin in _spins)
            yield return spin;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
