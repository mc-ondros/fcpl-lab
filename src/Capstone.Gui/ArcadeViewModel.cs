using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Capstone.Core.Events;
using Capstone.Core.Model;

namespace Capstone.Gui;

/// <summary>
/// What the window binds to. It owns one session and one machine, and it is
/// the second subscriber: the announcer narrates, this refreshes the screen.
/// </summary>
public sealed class ArcadeViewModel : INotifyPropertyChanged
{
    private const int StartingCredits = 100;

    private readonly SlotMachine _machine;
    private readonly SpinAnnouncer _announcer;
    private readonly JackpotBell _bell = new();
    private Session _session;
    private int _bet = 5;
    private string _status = "Pull the lever.";

    public ArcadeViewModel()
    {
        _machine = new SlotMachine(new Random(42));
        _announcer = new SpinAnnouncer(line => Status = line);
        _announcer.Subscribe(_machine);
        _bell.Subscribe(_machine);
        _machine.SpinCompleted += OnSpinCompleted;

        _session = NewSession();
        SpinCommand = new RelayCommand(Spin);
        NewGameCommand = new RelayCommand(NewGame);
    }

    public ICommand SpinCommand { get; }

    public ICommand NewGameCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SpinRow> History { get; } = [];

    public int[] BetChoices { get; } = [1, 5, 10, 25];

    public int Bet
    {
        get => _bet;
        set
        {
            if (_bet == value) return;
            _bet = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSpin));
        }
    }

    public string PlayerName => _session.PlayerName;

    public int Credits => _session.CurrentCredits;

    public int SpinsPlayed => _session.Spins.Count;

    public int Jackpots => _bell.JackpotsSeen;

    public bool CanSpin => _bet <= _session.CurrentCredits;

    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public string Reel1 { get; private set; } = "?";

    public string Reel2 { get; private set; } = "?";

    public string Reel3 { get; private set; } = "?";

    /// <summary>Bound to the SPIN button. One spin, then the bindings catch up.</summary>
    public void Spin()
    {
        if (!CanSpin)
        {
            Status = "Not enough credits for that bet.";
            return;
        }

        _machine.Spin(_session, _bet);
    }

    /// <summary>Bound to NEW SESSION. The machine and its subscribers stay.</summary>
    public void NewGame()
    {
        _session = NewSession();
        History.Clear();
        SetReels("?", "?", "?");
        Status = "New session. Pull the lever.";
        Refresh();
    }

    private void OnSpinCompleted(object? sender, SpinCompletedEventArgs e)
    {
        var spin = e.Spin;
        SetReels(spin.Reels.First.ToString(), spin.Reels.Second.ToString(), spin.Reels.Third.ToString());
        History.Insert(0, new SpinRow(spin));
        Refresh();
    }

    private void SetReels(string first, string second, string third)
    {
        Reel1 = first;
        Reel2 = second;
        Reel3 = third;
        OnPropertyChanged(nameof(Reel1));
        OnPropertyChanged(nameof(Reel2));
        OnPropertyChanged(nameof(Reel3));
    }

    private void Refresh()
    {
        OnPropertyChanged(nameof(Credits));
        OnPropertyChanged(nameof(SpinsPlayed));
        OnPropertyChanged(nameof(Jackpots));
        OnPropertyChanged(nameof(CanSpin));
    }

    private static Session NewSession() => new()
    {
        Id = $"sess-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
        PlayerName = "Sam",
        StartingCredits = StartingCredits,
        CurrentCredits = StartingCredits,
        StartedAt = DateTimeOffset.UtcNow,
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A command with no parameter, so a button can call a plain method.</summary>
public sealed class RelayCommand(Action action) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => action();
}

/// <summary>One line of the history list. Plain text, so the grid stays simple.</summary>
public sealed record SpinRow(string When, string Reels, int Bet, int Payout, int CreditsAfter)
{
    public SpinRow(Spin spin)
        : this(
            spin.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
            spin.Reels.ToString(),
            spin.Bet,
            spin.Payout,
            spin.CreditsAfter)
    {
    }
}
