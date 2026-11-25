using Capstone.Core.Events;

namespace Capstone.Core.Model;

/// <summary>
/// A 3-reel slot machine engine with configurable paytable and seeded randomness.
/// </summary>
public sealed class SlotMachine
{
    private readonly Random _random;
    private readonly IPaytable _paytable;
    private int _spinCounter;

    public SlotMachine(Random? random = null, IPaytable? paytable = null, ReelOdds? odds = null, int lossStreakThreshold = 9)
    {
        _random = random ?? new Random();
        _paytable = paytable ?? new Paytable();
        Odds = odds ?? ReelOdds.Realistic();
        LossStreakThreshold = Math.Max(0, lossStreakThreshold);
    }

    /// <summary>Weighted symbol probabilities used when rolling each reel.</summary>
    public ReelOdds Odds { get; }

    /// <summary>
    /// Number of consecutive losing spins before the machine awards a medium
    /// retention prize on the next spin (0 disables the hook).
    /// </summary>
    public int LossStreakThreshold { get; set; }

    /// <summary>Fires after every completed spin.</summary>
    public event EventHandler<SpinCompletedEventArgs>? SpinCompleted;

    /// <summary>Fires only when the reels land the jackpot.</summary>
    public event EventHandler<JackpotLandedEventArgs>? JackpotLanded;

    /// <summary>
    /// Executes a spin for the given session.
    /// Deducts the bet, rolls reels, calculates payout, and updates session credits.
    /// </summary>
    public Spin Spin(Session session, int bet, DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bet);

        if (bet > session.CurrentCredits)
        {
            throw new InvalidOperationException(
                $"Insufficient credits: session has {session.CurrentCredits} credits, but bet requires {bet}.");
        }

        // Deduct wager
        session.CurrentCredits -= bet;

        // Roll reels
        var reels = new Reels(RollSymbol(), RollSymbol(), RollSymbol());

        // Calculate winnings; if the player is on a losing streak, award a medium prize to keep them hooked
        var payout = _paytable.CalculatePayout(reels, bet);
        if (payout == 0 && LossStreakThreshold > 0 && CountTrailingLosses(session) >= LossStreakThreshold)
        {
            var hookSymbol = _random.Next(2) == 0 ? Symbol.Orange : Symbol.Plum;
            reels = new Reels(hookSymbol, hookSymbol, hookSymbol);
            payout = _paytable.CalculatePayout(reels, bet);
        }

        session.CurrentCredits += payout;

        _spinCounter++;
        var time = timestamp ?? DateTimeOffset.UtcNow;

        var spin = new Spin
        {
            Id = $"spin-{_spinCounter:D5}",
            Timestamp = time,
            Bet = bet,
            Reels = reels,
            Payout = payout,
            CreditsAfter = session.CurrentCredits,
            IsFreeSpin = false,
            SessionId = session.Id,
        };

        session.Spins.Add(spin);

        SpinCompleted?.Invoke(this, new SpinCompletedEventArgs { Spin = spin });

        if (_paytable.IsJackpot(reels))
            JackpotLanded?.Invoke(this, new JackpotLandedEventArgs { Spin = spin, Payout = payout });

        return spin;
    }

    private Symbol RollSymbol() => Odds.Roll(_random);

    private static int CountTrailingLosses(Session session)
    {
        int losses = 0;
        for (int i = session.Spins.Count - 1; i >= 0; i--)
        {
            if (session.Spins[i].Payout > 0)
                break;
            losses++;
        }

        return losses;
    }
}
