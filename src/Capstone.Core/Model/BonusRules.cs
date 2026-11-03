namespace Capstone.Core.Model;

/// <summary>
/// Pattern matching over the bonus hierarchy: one switch expression that
/// describes any bonus without the caller knowing which kind it is.
/// </summary>
public static class BonusRules
{
    /// <summary>
    /// A single switch expression over the two derived types, plus a
    /// discard arm so a future bonus kind still compiles.
    /// </summary>
    public static string DescribeBonus(Bonus bonus) => bonus switch
    {
        FreeSpinBonus { Spins: > 1 } many => $"{many.Spins} free spins — {many.Reason}",
        FreeSpinBonus one => $"a free spin — {one.Reason}",
        CreditBonus credits => $"+{credits.Credits} credits — {credits.Reason}",
        _ => bonus.Describe(),
    };
}
