using Capstone.Core.Json;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

/// <summary>
/// Connects the course's automated contract tests to our domain types.
/// </summary>
public sealed class Lab02Wiring : ILab02Wiring
{
    private static readonly Symbol[] AllSymbols = Enum.GetValues<Symbol>();
    private static readonly Paytable SharedPaytable = new();

    public IItemStore CreateStore() => new SpinHistoryStore();

    public IItemMapper CreateMapper() => new SpinMapper();

    public IItem CreateSample(int seed)
    {
        var absSeed = Math.Abs(seed);
        var s1 = AllSymbols[absSeed % AllSymbols.Length];
        var s2 = AllSymbols[(absSeed + 1) % AllSymbols.Length];
        var s3 = AllSymbols[(absSeed + 2) % AllSymbols.Length];
        var reels = new Reels(s1, s2, s3);

        var bet = 1 + (absSeed % 10);
        var payout = SharedPaytable.CalculatePayout(reels, bet);

        return new Spin
        {
            Id = $"spin-{seed:D4}",
            Timestamp = DateTimeOffset.UnixEpoch.AddMinutes(absSeed),
            Bet = bet,
            Reels = reels,
            Payout = payout,
            CreditsAfter = 100 + payout,
            IsFreeSpin = false,
            SessionId = $"sess-{(absSeed % 5) + 1:D2}",
        };
    }
}
