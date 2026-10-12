namespace Capstone.Core.Model;

/// <summary>
/// A 3-reel slot machine engine with configurable paytable and seeded randomness.
/// </summary>
public sealed class SlotMachine
{
    private static readonly Symbol[] AllSymbols = Enum.GetValues<Symbol>();

    private readonly Random _random;
    private readonly IPaytable _paytable;
    private int _spinCounter;

    public SlotMachine(Random? random = null, IPaytable? paytable = null)
    {
        _random = random ?? new Random();
        _paytable = paytable ?? new Paytable();
    }

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

        // Calculate winnings
        var payout = _paytable.CalculatePayout(reels, bet);
        session.CurrentCredits += payout;

        _spinCounter++;
        var time = timestamp ?? DateTimeOffset.UtcNow;

        return new Spin
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
    }

    private Symbol RollSymbol() => AllSymbols[_random.Next(AllSymbols.Length)];
}
