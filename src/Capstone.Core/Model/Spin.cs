using SharpQuest.Contracts;

namespace Capstone.Core.Model;

/// <summary>
/// Represents a single recorded spin on the slot machine.
/// Implements IItem for storage and contract verification.
/// </summary>
public sealed class Spin : IItem, ISlotEntity
{
    private readonly string _id = "";
    private readonly int _bet = 1;
    private readonly int _payout = 0;
    private readonly int _creditsAfter = 0;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A spin requires a non-empty Id.", nameof(Id))
            : value.Trim();
    }

    public required DateTimeOffset Timestamp { get; init; }

    public required int Bet
    {
        get => _bet;
        init => _bet = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Bet), value, "Bet must be greater than zero.");
    }

    public required Reels Reels { get; init; }

    public required int Payout
    {
        get => _payout;
        init => _payout = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Payout), value, "Payout cannot be negative.");
    }

    public required int CreditsAfter
    {
        get => _creditsAfter;
        init => _creditsAfter = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(CreditsAfter), value, "Credits after cannot be negative.");
    }

    public bool IsFreeSpin { get; init; }

    public string? SessionId { get; init; }

    public string DisplayName => $"Spin #{Id} {Reels} (Bet: {Bet}, Payout: {Payout})";

    public string Describe() =>
        $"{DisplayName} · Balance: {CreditsAfter}{(IsFreeSpin ? " [FREE SPIN]" : "")} at {Timestamp:u}";

    public override string ToString() => Describe();
}
