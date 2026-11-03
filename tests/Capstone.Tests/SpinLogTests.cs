using Capstone.Core.Collections;
using Capstone.Core.Model;

namespace Capstone.Tests;

/// <summary>
/// Lab S5: the custom collection — indexer, iterator and the generic filter.
/// </summary>
public class SpinLogTests
{
    private static Spin Spin(string id, int payout) => new()
    {
        Id = id,
        Timestamp = DateTimeOffset.UnixEpoch,
        Bet = 1,
        Reels = new Reels(Symbol.Lemon, Symbol.Lemon, Symbol.Lemon),
        Payout = payout,
        CreditsAfter = 100,
    };

    [Fact]
    public void Indexer_ReturnsSpinByPosition()
    {
        var log = new SpinLog();
        log.Add(Spin("a", 0));
        log.Add(Spin("b", 5));

        Assert.Equal("b", log[1].Id);
    }

    [Fact]
    public void Iterator_WalksEverySpinInOrder()
    {
        var log = new SpinLog();
        log.Add(Spin("a", 0));
        log.Add(Spin("b", 5));
        log.Add(Spin("c", 0));

        var seen = new List<string>();
        foreach (var spin in log)
            seen.Add(spin.Id);

        Assert.Equal(["a", "b", "c"], seen);
    }

    [Fact]
    public void Where_PassesAFunctionAndKeepsOnlyMatches()
    {
        var log = new SpinLog();
        log.Add(Spin("loss", 0));
        log.Add(Spin("win", 8));
        log.Add(Spin("jackpot", 100));

        // The predicate is a function handed to the generic method.
        var winners = log.Where(spin => spin.Payout > 0);

        Assert.Equal(2, winners.Count);
        Assert.Equal("win", winners[0].Id);
    }
}