namespace Capstone.Core.Model;

/// <summary>
/// Standard 3-reel paytable evaluator with Wild substitution and Cherry consolations.
/// </summary>
public sealed class Paytable : IPaytable
{
    public const int JackpotMultiplier = 100;

    public bool IsJackpot(Reels reels)
    {
        return EvaluateMultiplier(reels) == JackpotMultiplier;
    }

    public int CalculatePayout(Reels reels, int bet)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bet);
        return bet * EvaluateMultiplier(reels);
    }

    /// <summary>
    /// Evaluates the bet multiplier based on matching lines and wild substitutions.
    /// </summary>
    public int EvaluateMultiplier(Reels reels)
    {
        var (s1, s2, s3) = reels;

        // Three wilds or three sevens is the top jackpot
        if (s1 == Symbol.Wild && s2 == Symbol.Wild && s3 == Symbol.Wild) return JackpotMultiplier;
        if (MatchesThree(reels, Symbol.Seven)) return JackpotMultiplier;
        if (MatchesThree(reels, Symbol.Bar)) return 50;
        if (MatchesThree(reels, Symbol.Bell)) return 25;
        if (MatchesThree(reels, Symbol.Plum)) return 15;
        if (MatchesThree(reels, Symbol.Orange)) return 10;
        if (MatchesThree(reels, Symbol.Lemon)) return 8;
        if (MatchesThree(reels, Symbol.Cherry)) return 5;

        // Two cherries (with or without Wild) pays 3x
        int cherryCount = CountSymbolOrWild(reels, Symbol.Cherry);
        if (cherryCount >= 2) return 3;

        // Single cherry on any reel pays 1x
        if (s1 == Symbol.Cherry || s2 == Symbol.Cherry || s3 == Symbol.Cherry) return 1;

        return 0;
    }

    private static bool MatchesThree(Reels reels, Symbol target)
    {
        return (reels.First == target || reels.First == Symbol.Wild)
            && (reels.Second == target || reels.Second == Symbol.Wild)
            && (reels.Third == target || reels.Third == Symbol.Wild);
    }

    private static int CountSymbolOrWild(Reels reels, Symbol target)
    {
        int count = 0;
        if (reels.First == target || reels.First == Symbol.Wild) count++;
        if (reels.Second == target || reels.Second == Symbol.Wild) count++;
        if (reels.Third == target || reels.Third == Symbol.Wild) count++;
        return count;
    }
}
