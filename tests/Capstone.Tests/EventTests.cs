using Capstone.Core.Events;
using Capstone.Core.Model;

namespace Capstone.Tests;

/// <summary>
/// Lab S6: one publisher, two subscribers, and a forced jackpot so the bell rings.
/// </summary>
public class EventTests
{
    /// <summary>A paytable stand-in that reports a fixed payout, jackpot or not.</summary>
    private sealed class FixedPaytable(int payout, bool jackpot) : IPaytable
    {
        public int CalculatePayout(Reels reels, int bet) => payout;

        public bool IsJackpot(Reels reels) => jackpot;
    }

    private static Session NewSession() => new()
    {
        Id = "sess-01",
        PlayerName = "Sam",
        StartingCredits = 100,
        CurrentCredits = 100,
        StartedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void Spin_NotifiesBothSubscribers()
    {
        var machine = new SlotMachine(paytable: new FixedPaytable(payout: 500, jackpot: true));

        var lines = new List<string>();
        var announcer = new SpinAnnouncer(lines.Add);
        announcer.Subscribe(machine);

        var bell = new JackpotBell();
        bell.Subscribe(machine);

        machine.Spin(NewSession(), bet: 5);

        Assert.Single(lines);
        Assert.Equal(1, bell.JackpotsSeen);
        Assert.Equal(500, bell.CreditsWon);
    }

    [Fact]
    public void Spin_WithoutJackpot_RingsOnlyTheAnnouncer()
    {
        var machine = new SlotMachine(paytable: new FixedPaytable(payout: 0, jackpot: false));

        var bell = new JackpotBell();
        bell.Subscribe(machine);

        var announced = 0;
        machine.SpinCompleted += (_, _) => announced++;

        machine.Spin(NewSession(), bet: 5);

        Assert.Equal(1, announced);
        Assert.Equal(0, bell.JackpotsSeen);
    }
}