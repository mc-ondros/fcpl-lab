namespace Capstone.Core.Model;

/// <summary>
/// A player's gaming visit at the arcade containing multiple spins.
/// </summary>
public sealed class Session : ISlotEntity
{
    private readonly string _id = "";
    private readonly string _playerName = "";
    private int _currentCredits;

    public required string Id
    {
        get => _id;
        init => _id = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Session requires an Id.", nameof(Id))
            : value.Trim();
    }

    public required string PlayerName
    {
        get => _playerName;
        init => _playerName = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Player name cannot be empty.", nameof(PlayerName))
            : value.Trim();
    }

    public required int StartingCredits { get; init; }

    public int CurrentCredits
    {
        get => _currentCredits;
        set => _currentCredits = value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(CurrentCredits), value, "Credits cannot drop below zero.");
    }

    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Nullable end time: null indicates an ongoing session that has not ended yet.
    /// </summary>
    public DateTimeOffset? EndedAt { get; set; }

    public bool IsActive => EndedAt is null;

    public string Describe()
    {
        var status = EndedAt is null ? "Active" : $"Ended at {EndedAt:u}";
        return $"Session #{Id} ({PlayerName}) · Balance: {CurrentCredits} credits (Started: {StartingCredits}) · {status}";
    }

    public override string ToString() => Describe();
}
