using Capstone.Core.Model;

namespace Capstone.Core.Events;

/// <summary>Raised whenever a spin finishes, win or lose.</summary>
public sealed class SpinCompletedEventArgs : EventArgs
{
    public required Spin Spin { get; init; }
}

/// <summary>Raised only when the top jackpot lands.</summary>
public sealed class JackpotLandedEventArgs : EventArgs
{
    public required Spin Spin { get; init; }

    public required int Payout { get; init; }
}
