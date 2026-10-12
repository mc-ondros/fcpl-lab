using Capstone.Core.Model;

namespace Capstone.Core.Json;

/// <summary>
/// Data transfer object for serializing spins to and from JSON.
/// All fields are nullable to safely handle partial or malformed input.
/// </summary>
public sealed record SpinDto(
    string? Id,
    DateTimeOffset? Timestamp,
    int? Bet,
    Symbol? Reel1,
    Symbol? Reel2,
    Symbol? Reel3,
    int? Payout,
    int? CreditsAfter,
    bool? IsFreeSpin,
    string? SessionId);
