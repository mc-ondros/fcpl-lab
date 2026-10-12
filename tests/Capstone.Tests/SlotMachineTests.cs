using Capstone.Core.Model;

namespace Capstone.Tests;

/// <summary>
/// Domain logic tests for the Slot Machine Arcade (LuckySpin).
/// Verifies paytable multipliers, wild substitution, balance limits, and seeded reproducibility.
/// </summary>
public class SlotMachineTests
{
    private readonly Paytable _paytable = new();

    [Fact]
    public void Jackpot_ThreeSevens_AwardsJackpotMultiplier()
    {
        var reels = new Reels(Symbol.Seven, Symbol.Seven, Symbol.Seven);
        const int bet = 5;

        var payout = _paytable.CalculatePayout(reels, bet);

        Assert.Equal(500, payout);
        Assert.True(_paytable.IsJackpot(reels));
    }

    [Fact]
    public void Paytable_WildSymbol_SubstitutesInWinningLine()
    {
        // Two Bars and a Wild should substitute and count as three Bars (50x multiplier)
        var reels = new Reels(Symbol.Bar, Symbol.Wild, Symbol.Bar);
        const int bet = 2;

        var payout = _paytable.CalculatePayout(reels, bet);

        Assert.Equal(100, payout);
    }

    [Fact]
    public void Paytable_TwoCherries_AwardsConsolationPayout()
    {
        // Two cherries pay 3x bet
        var reels = new Reels(Symbol.Cherry, Symbol.Cherry, Symbol.Lemon);
        const int bet = 10;

        var payout = _paytable.CalculatePayout(reels, bet);

        Assert.Equal(30, payout);
    }

    [Fact]
    public void Paytable_LosingCombination_AwardsZeroPayout()
    {
        var reels = new Reels(Symbol.Lemon, Symbol.Bell, Symbol.Orange);
        const int bet = 10;

        var payout = _paytable.CalculatePayout(reels, bet);

        Assert.Equal(0, payout);
    }

    [Fact]
    public void SlotMachine_BetExceedingCredits_IsRefused()
    {
        var machine = new SlotMachine();
        var session = new Session
        {
            Id = "sess-01",
            PlayerName = "Sam",
            StartingCredits = 50,
            CurrentCredits = 50,
            StartedAt = DateTimeOffset.UtcNow,
        };

        var ex = Assert.Throws<InvalidOperationException>(() => machine.Spin(session, 100));
        Assert.Contains("Insufficient credits", ex.Message);
        Assert.Equal(50, session.CurrentCredits);
    }

    [Fact]
    public void SlotMachine_NegativeOrZeroBet_ThrowsArgumentOutOfRangeException()
    {
        var machine = new SlotMachine();
        var session = new Session
        {
            Id = "sess-01",
            PlayerName = "Sam",
            StartingCredits = 100,
            CurrentCredits = 100,
            StartedAt = DateTimeOffset.UtcNow,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Spin(session, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Spin(session, -5));
    }

    [Fact]
    public void SlotMachine_SeededRandom_ProducesDeterministicSpins()
    {
        const int seed = 42;
        var machine1 = new SlotMachine(new Random(seed));
        var machine2 = new SlotMachine(new Random(seed));

        var session1 = new Session
        {
            Id = "s1",
            PlayerName = "Player 1",
            StartingCredits = 1000,
            CurrentCredits = 1000,
            StartedAt = DateTimeOffset.UtcNow,
        };
        var session2 = new Session
        {
            Id = "s2",
            PlayerName = "Player 2",
            StartingCredits = 1000,
            CurrentCredits = 1000,
            StartedAt = DateTimeOffset.UtcNow,
        };

        for (int i = 0; i < 5; i++)
        {
            var spin1 = machine1.Spin(session1, 10);
            var spin2 = machine2.Spin(session2, 10);

            Assert.Equal(spin1.Reels, spin2.Reels);
            Assert.Equal(spin1.Payout, spin2.Payout);
            Assert.Equal(session1.CurrentCredits, session2.CurrentCredits);
        }
    }

    [Fact]
    public void SlotMachine_WinningSpin_UpdatesSessionCreditsCorrectly()
    {
        // Force machine with specific reels via paytable
        var machine = new SlotMachine(new Random(1));
        var session = new Session
        {
            Id = "sess-test",
            PlayerName = "Alex",
            StartingCredits = 100,
            CurrentCredits = 100,
            StartedAt = DateTimeOffset.UtcNow,
        };

        var spin = machine.Spin(session, 10);

        // Expected balance: initial 100 - bet 10 + payout
        Assert.Equal(100 - 10 + spin.Payout, session.CurrentCredits);
        Assert.Equal(session.CurrentCredits, spin.CreditsAfter);
    }
}
