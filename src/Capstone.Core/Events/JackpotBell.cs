using Capstone.Core.Model;

namespace Capstone.Core.Events;

/// <summary>
/// Second subscriber: watches only for the jackpot and keeps a tally,
/// which is exactly the "how often does the jackpot really land" question.
/// </summary>
public sealed class JackpotBell
{
    public int JackpotsSeen { get; private set; }

    public int CreditsWon { get; private set; }

    public void OnJackpotLanded(object? sender, JackpotLandedEventArgs e)
    {
        JackpotsSeen++;
        CreditsWon += e.Payout;
    }

    public void Subscribe(SlotMachine machine) => machine.JackpotLanded += OnJackpotLanded;
}
