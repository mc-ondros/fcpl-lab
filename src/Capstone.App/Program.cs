using Capstone.Core.Collections;
using Capstone.Core.Events;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

// LuckySpin — Slot Machine Arcade Demo (uses Labs 02–08 features end-to-end)
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
var recentSymbols = new RecentlyUsed<Symbol>(4);

// Lab 06: Events & subscribers
var announcer = new SpinAnnouncer(line => Console.WriteLine($"  [Announcer] {line}"));
announcer.Subscribe(machine);
var bell = new JackpotBell();
bell.Subscribe(machine);

const int bet = 10;
Console.WriteLine($"Spinning 6 times with a bet of {bet} credits...\n");

for (int i = 1; i <= 6; i++)
{
    var spin = machine.Spin(session, bet);
    history.Add(spin);
    recentSymbols.Push(spin.Reels.First);
    recentSymbols.Push(spin.Reels.Second);
    recentSymbols.Push(spin.Reels.Third);
    Console.WriteLine($"[Spin {i}] Balance: {spin.CreditsAfter,4} credits");
}

// Lab 04: Polymorphic Bonus + pattern matching
List<Bonus> bonuses =
[
    new FreeSpinBonus { Id = "b-1", Reason = "Demo reward", Spins = 3 },
    new CreditBonus { Id = "b-2", Reason = "Welcome gift", Credits = 25 },
];
Console.WriteLine("\nBonuses:");
foreach (var bonus in bonuses)
{
    history.Add(bonus);
    Console.WriteLine($"  - {BonusRules.DescribeBonus(bonus)}");
}

// Lab 03 & Lab 07: SearchMatcher + LINQ summary
var matcher = SearchMatcher.Create();
var cherryMatches = history.Where(item => matcher.Matches(item.DisplayName, "Cherry")).ToList();
Console.WriteLine($"\nSearch 'Cherry' matches: {cherryMatches.Count}");
Console.WriteLine($"Recent symbols: {string.Join(", ", recentSymbols)}");
Console.WriteLine($"Jackpots seen: {bell.JackpotsSeen}");

// Lab 08: Async save & reload
string savePath = Path.Combine(Path.GetTempPath(), "luckyspin-demo-store.json");
await StoreJsonStorage.SaveAsync(history, savePath);
var reloaded = await StoreJsonStorage.LoadAsync(savePath);
Console.WriteLine($"Saved & reloaded {reloaded.Count} items from {savePath}");

session.EndedAt = DateTimeOffset.UtcNow;
Console.WriteLine($"Session finished: {session.Describe()}");
