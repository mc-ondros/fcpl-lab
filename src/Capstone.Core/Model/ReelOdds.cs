namespace Capstone.Core.Model;

/// <summary>
/// Weighted virtual reel probabilities for each symbol, modeling how real slot
/// machines make common symbols frequent and jackpot symbols rare.
/// </summary>
public sealed class ReelOdds
{
    private static readonly Symbol[] AllSymbols = Enum.GetValues<Symbol>();
    private readonly Dictionary<Symbol, int> _weights = new();

    public ReelOdds(IDictionary<Symbol, int>? initialWeights = null)
    {
        foreach (var symbol in AllSymbols)
        {
            int weight = initialWeights is null
                ? 1
                : initialWeights.TryGetValue(symbol, out var w) ? Math.Max(0, w) : 0;
            _weights[symbol] = weight;
        }

        if (TotalWeight <= 0)
            _weights[Symbol.Cherry] = 1;
    }

    /// <summary>
    /// Realistic casino-style weights (sum = 100) where cherries and lemons
    /// appear often while 7s and Wilds are rare.
    /// </summary>
    public static ReelOdds Realistic() => new(new Dictionary<Symbol, int>
    {
        [Symbol.Cherry] = 24,
        [Symbol.Lemon] = 20,
        [Symbol.Orange] = 17,
        [Symbol.Plum] = 14,
        [Symbol.Bell] = 10,
        [Symbol.Bar] = 7,
        [Symbol.Seven] = 4,
        [Symbol.Wild] = 4,
    });

    /// <summary>Equal probability (12.5% each) for every symbol.</summary>
    public static ReelOdds Uniform() => new(AllSymbols.ToDictionary(s => s, _ => 10));

    /// <summary>High-payout preset with boosted Wilds and Sevens.</summary>
    public static ReelOdds Generous() => new(new Dictionary<Symbol, int>
    {
        [Symbol.Cherry] = 18,
        [Symbol.Lemon] = 14,
        [Symbol.Orange] = 14,
        [Symbol.Plum] = 12,
        [Symbol.Bell] = 12,
        [Symbol.Bar] = 10,
        [Symbol.Seven] = 10,
        [Symbol.Wild] = 10,
    });

    /// <summary>Tight house-edge preset dominated by non-matching fruit symbols.</summary>
    public static ReelOdds Tight() => new(new Dictionary<Symbol, int>
    {
        [Symbol.Cherry] = 10,
        [Symbol.Lemon] = 28,
        [Symbol.Orange] = 26,
        [Symbol.Plum] = 20,
        [Symbol.Bell] = 8,
        [Symbol.Bar] = 4,
        [Symbol.Seven] = 2,
        [Symbol.Wild] = 2,
    });

    public int TotalWeight => _weights.Values.Sum();

    public int GetWeight(Symbol symbol) => _weights.GetValueOrDefault(symbol, 0);

    public void SetWeight(Symbol symbol, int weight)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(weight);

        // Prevent setting total weight to zero so Roll() always has a valid outcome.
        if (weight == 0 && TotalWeight - GetWeight(symbol) <= 0)
            throw new InvalidOperationException("At least one symbol must have a weight greater than zero.");

        _weights[symbol] = weight;
    }

    public void ApplyFrom(ReelOdds other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var symbol in AllSymbols)
            _weights[symbol] = other.GetWeight(symbol);
    }

    /// <summary>Probability (0.0 to 1.0) of landing <paramref name="symbol"/> on one reel.</summary>
    public double GetProbability(Symbol symbol)
    {
        int total = TotalWeight;
        return total == 0 ? 0 : (double)GetWeight(symbol) / total;
    }

    /// <summary>Rolls a single symbol according to the configured weights.</summary>
    public Symbol Roll(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        int total = TotalWeight;
        int roll = random.Next(total);
        int cumulative = 0;

        foreach (var symbol in AllSymbols)
        {
            cumulative += _weights[symbol];
            if (roll < cumulative)
                return symbol;
        }

        return AllSymbols[^1];
    }

    /// <summary>
    /// Computes the exact theoretical Return-To-Player percentage (e.g. 95.4)
    /// across all 512 possible 3-reel outcomes for the given paytable.
    /// </summary>
    public double CalculateTheoreticalRtp(IPaytable paytable)
    {
        ArgumentNullException.ThrowIfNull(paytable);

        double total = TotalWeight;
        if (total <= 0) return 0;

        double expectedReturnPerCredit = 0;

        foreach (var s1 in AllSymbols)
        {
            double p1 = _weights[s1] / total;
            if (p1 == 0) continue;

            foreach (var s2 in AllSymbols)
            {
                double p2 = _weights[s2] / total;
                if (p2 == 0) continue;

                foreach (var s3 in AllSymbols)
                {
                    double p3 = _weights[s3] / total;
                    if (p3 == 0) continue;

                    int payout = paytable.CalculatePayout(new Reels(s1, s2, s3), bet: 1);
                    expectedReturnPerCredit += p1 * p2 * p3 * payout;
                }
            }
        }

        return expectedReturnPerCredit * 100.0;
    }
}
