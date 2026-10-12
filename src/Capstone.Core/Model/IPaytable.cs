namespace Capstone.Core.Model;

/// <summary>
/// Evaluates reel outcomes to determine payout amounts and jackpot triggers.
/// </summary>
public interface IPaytable
{
    /// <summary>
    /// Calculates the credit payout awarded for the given reel combination and bet.
    /// </summary>
    int CalculatePayout(Reels reels, int bet);

    /// <summary>
    /// Indicates whether the combination constitutes the top jackpot.
    /// </summary>
    bool IsJackpot(Reels reels);
}
