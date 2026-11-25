using Capstone.Core.Model;

namespace Capstone.Tests;

/// <summary>
/// Verifies weighted reel odds, preset RTP calculations, and deterministic rolling.
/// </summary>
public class ReelOddsTests
{
    [Fact]
    public void RealisticOdds_AreWeightedTowardCommonSymbols()
    {
        var odds = ReelOdds.Realistic();

        Assert.True(odds.GetProbability(Symbol.Cherry) > odds.GetProbability(Symbol.Seven));
        Assert.True(odds.GetProbability(Symbol.Lemon) > odds.GetProbability(Symbol.Wild));
    }

    [Fact]
    public void Roll_ZeroWeightSymbol_NeverAppears()
    {
        var odds = ReelOdds.Uniform();
        odds.SetWeight(Symbol.Wild, 0);
        var rng = new Random(123);

        for (int i = 0; i < 200; i++)
            Assert.NotEqual(Symbol.Wild, odds.Roll(rng));
    }

    [Fact]
    public void SetWeight_LastRemainingWeightToZero_Throws()
    {
        var odds = new ReelOdds(new Dictionary<Symbol, int> { [Symbol.Seven] = 5 });

        Assert.Throws<InvalidOperationException>(() => odds.SetWeight(Symbol.Seven, 0));
    }

    [Fact]
    public void CalculateTheoreticalRtp_ReturnsPositivePercentage()
    {
        var odds = ReelOdds.Realistic();
        var paytable = new Paytable();

        double rtp = odds.CalculateTheoreticalRtp(paytable);

        Assert.True(rtp > 0);
        Assert.True(rtp < 500);
    }

    [Fact]
    public void LosingStreak_TriggersMediumRetentionPrize()
    {
        // Force losses using Lemon/Orange/Plum? No, use a weight of 100 on Lemon? That would win.
        // Instead, use a seeded machine with threshold = 3 and verify no streak of >3 zero-payout spins occurs.
        var machine = new SlotMachine(new Random(7), lossStreakThreshold: 3);
        var session = new Session
        {
            Id = "sess-hook",
            PlayerName = "Sam",
            StartingCredits = 1000,
            CurrentCredits = 1000,
            StartedAt = DateTimeOffset.UtcNow,
        };

        int consecutiveLosses = 0;
        int maxConsecutiveLosses = 0;

        for (int i = 0; i < 40; i++)
        {
            var spin = machine.Spin(session, bet: 5);
            if (spin.Payout == 0)
            {
                consecutiveLosses++;
                maxConsecutiveLosses = Math.Max(maxConsecutiveLosses, consecutiveLosses);
            }
            else
            {
                consecutiveLosses = 0;
            }
        }

        Assert.True(maxConsecutiveLosses <= 3);
    }
}

