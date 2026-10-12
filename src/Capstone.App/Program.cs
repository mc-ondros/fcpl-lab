using Capstone.Core.Model;
using Capstone.Core.Storage;

// LuckySpin — Slot Machine Arcade Demo
// Run with: dotnet run --project src/Capstone.App

Console.WriteLine("🎰 Welcome to LuckySpin Arcade! 🎰");
Console.WriteLine("-----------------------------------");

var session = new Session
{
    Id = "sess-001",
    PlayerName = "Sam",
    StartingCredits = 100,
    CurrentCredits = 100,
    StartedAt = DateTimeOffset.UtcNow,
};

Console.WriteLine(session.Describe());
Console.WriteLine();

var history = new SpinHistoryStore();
var machine = new SlotMachine(new Random(42));

const int bet = 10;
Console.WriteLine($"Spinning 5 times with a bet of {bet} credits...\n");

for (int i = 1; i <= 5; i++)
{
    var spin = machine.Spin(session, bet);
    history.Add(spin);
    Console.WriteLine($"[Spin {i}] {spin.Reels} -> Payout: {spin.Payout,3} credits | Balance: {spin.CreditsAfter,4} credits");
}

Console.WriteLine();
Console.WriteLine($"Total spins recorded in store: {history.Count}");
session.EndedAt = DateTimeOffset.UtcNow;
Console.WriteLine($"Session finished: {session.Describe()}");
