using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using Capstone.Core.Collections;
using Capstone.Core.Events;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Gui;

/// <summary>
/// What the window binds to. It owns one session and one machine, and it is
/// the third subscriber: the announcer narrates, the bell counts jackpots,
/// and this refreshes the slot cabinet screen.
/// </summary>
public sealed class ArcadeViewModel : INotifyPropertyChanged
{
    private const int StartingCredits = 100;
    private static readonly Symbol[] StripOrder =
    [
        Symbol.Cherry,
        Symbol.Lemon,
        Symbol.Orange,
        Symbol.Plum,
        Symbol.Bell,
        Symbol.Bar,
        Symbol.Seven,
        Symbol.Wild,
    ];

    private readonly SlotMachine _machine;
    private readonly SpinAnnouncer _announcer;
    private readonly JackpotBell _bell = new();
    private readonly SpinHistoryStore _historyStore = new();
    private readonly RecentlyUsed<Symbol> _recentSymbols = new(4);
    private readonly SearchMatcher _searchMatcher = SearchMatcher.Create();
    private readonly string _savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LuckySpin",
        "store.json");
    private readonly Random _visualRng = new();
    private readonly DispatcherTimer? _spinTimer;
    private readonly DispatcherTimer? _winTimer;

    private Session _session;
    private int _bet = 5;
    private int _lastWin;
    private int _displayedLastWin;
    private bool _isSpinning;
    private bool _isJackpotWin;
    private bool _isCelebrating;
    private bool _winFlashOn;
    private bool _isAdminOpen;
    private bool _forceJackpotNextSpin;
    private int _spinTick;
    private int _winTick;
    private int _winTotalTicks;
    private Spin? _pendingSpin;
    private string _searchQuery = string.Empty;
    private string _status = "READY";

    public ArcadeViewModel()
    {
        _machine = new SlotMachine();
        _announcer = new SpinAnnouncer(line => Status = line);
        _announcer.Subscribe(_machine);
        _bell.Subscribe(_machine);
        _machine.SpinCompleted += OnSpinCompleted;

        _session = NewSession();
        SpinCommand = new RelayCommand(Spin);
        NewGameCommand = new RelayCommand(NewGame);
        MaxBetCommand = new RelayCommand(SelectMaxBet);
        ToggleAdminCommand = new RelayCommand(ToggleAdmin);
        CloseAdminCommand = new RelayCommand(() => IsAdminOpen = false);
        AddTestCreditsCommand = new RelayCommand(AddTestCredits);
        ApplyRealisticOddsCommand = new RelayCommand(() => ApplyOddsPreset(ReelOdds.Realistic()));
        ApplyUniformOddsCommand = new RelayCommand(() => ApplyOddsPreset(ReelOdds.Uniform()));
        ApplyGenerousOddsCommand = new RelayCommand(() => ApplyOddsPreset(ReelOdds.Generous()));
        ApplyTightOddsCommand = new RelayCommand(() => ApplyOddsPreset(ReelOdds.Tight()));
        SaveStoreCommand = new RelayCommand(() => _ = SaveStoreAsync());
        LoadStoreCommand = new RelayCommand(() => _ = LoadStoreAsync());

        BetButtons = BetChoices.Select(amount => new BetOption(amount, amount == _bet, SelectBet)).ToList();
        SymbolOddsList = Enum.GetValues<Symbol>()
            .Select(symbol => new SymbolOddsItem(symbol, _machine.Odds, OnOddsChanged))
            .ToList();

        if (Application.Current is not null)
        {
            _spinTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(65) };
            _spinTimer.Tick += OnSpinTimerTick;

            _winTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(95) };
            _winTimer.Tick += OnWinTimerTick;
        }
    }

    public ICommand SpinCommand { get; }

    public ICommand NewGameCommand { get; }

    public ICommand MaxBetCommand { get; }

    public ICommand ToggleAdminCommand { get; }

    public ICommand CloseAdminCommand { get; }

    public ICommand AddTestCreditsCommand { get; }

    public ICommand ApplyRealisticOddsCommand { get; }

    public ICommand ApplyUniformOddsCommand { get; }

    public ICommand ApplyGenerousOddsCommand { get; }

    public ICommand ApplyTightOddsCommand { get; }

    public ICommand SaveStoreCommand { get; }

    public ICommand LoadStoreCommand { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            string next = value ?? string.Empty;
            if (string.Equals(_searchQuery, next, StringComparison.Ordinal)) return;
            _searchQuery = next;
            OnPropertyChanged();
            RebuildFilteredHistory();
        }
    }

    public string RecentSymbolsText => _recentSymbols.Count == 0
        ? "—"
        : string.Join(" ", _recentSymbols.Select(s => DescribeSymbol(s).Icon));

    public string SessionStatsText
    {
        get
        {
            if (_session.Spins.Count == 0)
                return "0% win · Best 0";

            int wins = _session.Spins.Count(s => s.Payout > 0);
            int best = _session.Spins.Max(s => s.Payout);
            double rate = (double)wins / _session.Spins.Count * 100.0;
            return $"{rate:F0}% win · Best {best}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SpinRow> History { get; } = [];

    public int[] BetChoices { get; } = [1, 5, 10, 25];

    public IReadOnlyList<BetOption> BetButtons { get; }

    public IReadOnlyList<SymbolOddsItem> SymbolOddsList { get; }

    public bool IsAdminOpen
    {
        get => _isAdminOpen;
        set
        {
            if (_isAdminOpen == value) return;
            _isAdminOpen = value;
            OnPropertyChanged();
        }
    }

    public bool ForceJackpotNextSpin
    {
        get => _forceJackpotNextSpin;
        set
        {
            if (_forceJackpotNextSpin == value) return;
            _forceJackpotNextSpin = value;
            OnPropertyChanged();
        }
    }

    public string TheoreticalRtpText =>
        $"Theoretical RTP: {_machine.Odds.CalculateTheoreticalRtp(new Paytable()):F1}%";

    public string TotalWeightText => $"Total Weight: {_machine.Odds.TotalWeight}";

    public ReelState Reel1State { get; } = new();

    public ReelState Reel2State { get; } = new();

    public ReelState Reel3State { get; } = new();

    public int Bet
    {
        get => _bet;
        set
        {
            if (_bet == value) return;
            _bet = value;
            foreach (var option in BetButtons)
                option.IsSelected = option.Amount == _bet;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSpin));
        }
    }

    public string PlayerName => _session.PlayerName;

    public int Credits => _session.CurrentCredits;

    public int SpinsPlayed => _session.Spins.Count;

    public int Jackpots => _bell.JackpotsSeen;

    public int JackpotCreditsWon => _bell.CreditsWon;

    public int LastWin
    {
        get => _lastWin;
        private set
        {
            _lastWin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasWin));
        }
    }

    public int DisplayedLastWin
    {
        get => _displayedLastWin;
        private set
        {
            if (_displayedLastWin == value) return;
            _displayedLastWin = value;
            OnPropertyChanged();
        }
    }

    public bool HasWin => _lastWin > 0;

    public bool IsJackpotWin
    {
        get => _isJackpotWin;
        private set
        {
            _isJackpotWin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJackpotPulse));
        }
    }

    public bool IsCelebrating
    {
        get => _isCelebrating;
        private set
        {
            if (_isCelebrating == value) return;
            _isCelebrating = value;
            OnPropertyChanged();
        }
    }

    public bool WinFlashOn
    {
        get => _winFlashOn;
        private set
        {
            if (_winFlashOn == value) return;
            _winFlashOn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWinPulse));
            OnPropertyChanged(nameof(IsJackpotPulse));
        }
    }

    public bool IsWinPulse => _winFlashOn && !_isJackpotWin;

    public bool IsJackpotPulse => _winFlashOn && _isJackpotWin;

    public bool IsSpinning
    {
        get => _isSpinning;
        private set
        {
            _isSpinning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSpin));
            OnPropertyChanged(nameof(SpinButtonText));
        }
    }

    public string SpinButtonText => _isSpinning ? "ROLLING..." : "SPIN";

    public bool CanSpin => !_isSpinning && _bet <= _session.CurrentCredits;

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

    /// <summary>Bound to the SPIN button. Animates the reels sequentially, then settles the spin.</summary>
    public void Spin()
    {
        if (!CanSpin)
        {
            if (!_isSpinning)
                Status = "LOW CREDITS";
            return;
        }

        if (_spinTimer is null)
        {
            ExecuteDomainSpin();
            return;
        }

        StopWinCelebration();
        IsSpinning = true;
        IsJackpotWin = false;
        LastWin = 0;
        DisplayedLastWin = 0;
        Status = "SPINNING...";
        _spinTick = 0;
        _pendingSpin = null;
        _spinTimer.Start();
    }

    /// <summary>Bound to NEW SESSION. The machine and its subscribers stay.</summary>
    public void NewGame()
    {
        _spinTimer?.Stop();
        StopWinCelebration();
        IsSpinning = false;
        _pendingSpin = null;
        _session = NewSession();
        History.Clear();
        LastWin = 0;
        DisplayedLastWin = 0;
        IsJackpotWin = false;
        Reel1State.Reset();
        Reel2State.Reset();
        Reel3State.Reset();
        SetReels("?", "?", "?");
        Status = "READY";
        Refresh();
    }

    private void SelectBet(int amount)
    {
        if (!IsSpinning)
            Bet = amount;
    }

    private void SelectMaxBet()
    {
        if (IsSpinning) return;
        var affordable = BetChoices.Where(b => b <= _session.CurrentCredits).DefaultIfEmpty(BetChoices[0]).Max();
        Bet = affordable;
    }

    private void ToggleAdmin() => IsAdminOpen = !IsAdminOpen;

    private void AddTestCredits()
    {
        _session.CurrentCredits += 100;
        Refresh();
        Status = "Admin: +100 test credits added.";
    }

    private void ApplyOddsPreset(ReelOdds preset)
    {
        _machine.Odds.ApplyFrom(preset);
        OnOddsChanged();
    }

    private void OnOddsChanged()
    {
        foreach (var item in SymbolOddsList)
            item.SyncFromModel();

        OnPropertyChanged(nameof(TheoreticalRtpText));
        OnPropertyChanged(nameof(TotalWeightText));
    }

    private Spin ExecuteDomainSpin()
    {
        if (!_forceJackpotNextSpin)
            return _machine.Spin(_session, _bet);

        var snapshot = new ReelOdds();
        snapshot.ApplyFrom(_machine.Odds);
        try
        {
            _machine.Odds.ApplyFrom(new ReelOdds(new Dictionary<Symbol, int> { [Symbol.Seven] = 1 }));
            ForceJackpotNextSpin = false;
            return _machine.Spin(_session, _bet);
        }
        finally
        {
            _machine.Odds.ApplyFrom(snapshot);
        }
    }

    private void OnSpinTimerTick(object? sender, EventArgs e)
    {
        _spinTick++;

        // Execute the actual domain spin on tick 1, then reveal reels left-to-right.
        if (_spinTick == 1)
        {
            _pendingSpin = ExecuteDomainSpin();
        }

        if (_spinTick < 6)
            Reel1State.SetSymbol(RandomSymbol());
        else if (_spinTick == 6 && _pendingSpin is not null)
            Reel1State.SetSymbol(_pendingSpin.Reels.First);

        if (_spinTick < 10)
            Reel2State.SetSymbol(RandomSymbol());
        else if (_spinTick == 10 && _pendingSpin is not null)
            Reel2State.SetSymbol(_pendingSpin.Reels.Second);

        if (_spinTick < 14)
        {
            Reel3State.SetSymbol(RandomSymbol());
            return;
        }

        _spinTimer?.Stop();
        if (_pendingSpin is not null)
            FinalizeSpinDisplay(_pendingSpin);

        IsSpinning = false;
        _pendingSpin = null;
    }

    private void OnSpinCompleted(object? sender, SpinCompletedEventArgs e)
    {
        // During animated spins, defer updating balance/history until Reel 3 locks in.
        if (_isSpinning)
            return;

        FinalizeSpinDisplay(e.Spin);
    }

    private void FinalizeSpinDisplay(Spin spin)
    {
        Reel1State.SetSymbol(spin.Reels.First);
        Reel2State.SetSymbol(spin.Reels.Second);
        Reel3State.SetSymbol(spin.Reels.Third);
        SetReels(spin.Reels.First.ToString(), spin.Reels.Second.ToString(), spin.Reels.Third.ToString());

        LastWin = spin.Payout;
        var paytable = new Paytable();
        IsJackpotWin = paytable.IsJackpot(spin.Reels);

        _recentSymbols.Push(spin.Reels.First);
        _recentSymbols.Push(spin.Reels.Second);
        _recentSymbols.Push(spin.Reels.Third);
        if (_historyStore.FindById(spin.Id) is null)
            _historyStore.Add(spin);

        if (IsJackpotWin)
        {
            var bonus = new FreeSpinBonus
            {
                Id = $"bonus-{spin.Id}",
                Reason = spin.Reels.ToString(),
                Spins = 3,
            };
            _historyStore.Add(bonus);
            Status = $"★ JACKPOT +{spin.Payout}! ({BonusRules.DescribeBonus(bonus)}) ★";
        }
        else if (spin.Payout > 0)
        {
            Status = $"✨ +{spin.Payout} CREDITS ✨";
        }
        else
        {
            Status = "TRY AGAIN";
        }

        if (_searchMatcher.Matches(spin.DisplayName, _searchQuery))
            History.Insert(0, new SpinRow(spin));
        Refresh();

        if (spin.Payout > 0 && _winTimer is not null)
            StartWinCelebration(IsJackpotWin);
        else
            DisplayedLastWin = spin.Payout;
    }

    private void StartWinCelebration(bool isJackpot)
    {
        _winTick = 0;
        _winTotalTicks = isJackpot ? 18 : 10;
        DisplayedLastWin = 0;
        IsCelebrating = true;
        WinFlashOn = true;
        _winTimer?.Start();
    }

    private void StopWinCelebration()
    {
        _winTimer?.Stop();
        IsCelebrating = false;
        WinFlashOn = false;
        DisplayedLastWin = LastWin;
    }

    private void OnWinTimerTick(object? sender, EventArgs e)
    {
        _winTick++;
        WinFlashOn = _winTick % 2 == 1;

        double progress = Math.Min(1.0, (double)_winTick / _winTotalTicks);
        DisplayedLastWin = (int)Math.Round(LastWin * progress);

        if (_winTick >= _winTotalTicks)
            StopWinCelebration();
    }

    private Symbol RandomSymbol() => StripOrder[_visualRng.Next(StripOrder.Length)];

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
        OnPropertyChanged(nameof(JackpotCreditsWon));
        OnPropertyChanged(nameof(CanSpin));
        OnPropertyChanged(nameof(RecentSymbolsText));
        OnPropertyChanged(nameof(SessionStatsText));
    }

    private void RebuildFilteredHistory()
    {
        History.Clear();
        foreach (var spin in _session.Spins.Reverse())
        {
            if (_searchMatcher.Matches(spin.DisplayName, _searchQuery))
                History.Add(new SpinRow(spin));
        }
    }

    private async Task SaveStoreAsync()
    {
        await StoreJsonStorage.SaveAsync(_historyStore, _savePath);
        Status = $"Saved {_historyStore.Count} item(s) to disk.";
    }

    private async Task LoadStoreAsync()
    {
        var loaded = await StoreJsonStorage.LoadAsync(_savePath);
        var items = loaded.All();
        var loadedSpins = items.OfType<Spin>().ToList();

        _session = NewSession();
        foreach (var id in _historyStore.All().Select(i => i.Id).ToList())
            _historyStore.Remove(id);

        foreach (var item in items)
            _historyStore.Add(item);

        foreach (var spin in loadedSpins)
            _session.Spins.Add(spin);

        if (loadedSpins.Count > 0)
        {
            var latest = loadedSpins[^1];
            _session.CurrentCredits = latest.CreditsAfter;
            LastWin = latest.Payout;
            DisplayedLastWin = latest.Payout;
            Reel1State.SetSymbol(latest.Reels.First);
            Reel2State.SetSymbol(latest.Reels.Second);
            Reel3State.SetSymbol(latest.Reels.Third);
            SetReels(latest.Reels.First.ToString(), latest.Reels.Second.ToString(), latest.Reels.Third.ToString());
            _recentSymbols.Push(latest.Reels.First);
            _recentSymbols.Push(latest.Reels.Second);
            _recentSymbols.Push(latest.Reels.Third);
        }
        else
        {
            LastWin = 0;
            DisplayedLastWin = 0;
            Reel1State.Reset();
            Reel2State.Reset();
            Reel3State.Reset();
            SetReels("?", "?", "?");
        }

        RebuildFilteredHistory();
        Refresh();
        Status = $"Loaded {loaded.Count} item(s) from disk.";
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

    internal static (string Icon, string Label, IBrush Brush) DescribeSymbol(Symbol symbol) => symbol switch
    {
        Symbol.Cherry => ("🍒", "CHERRY", SolidColorBrush.Parse("#FF4D6D")),
        Symbol.Lemon => ("🍋", "LEMON", SolidColorBrush.Parse("#FFE066")),
        Symbol.Orange => ("🍊", "ORANGE", SolidColorBrush.Parse("#FF9F43")),
        Symbol.Plum => ("🍇", "PLUM", SolidColorBrush.Parse("#C77DFF")),
        Symbol.Bell => ("🔔", "BELL", SolidColorBrush.Parse("#FFD166")),
        Symbol.Bar => ("BAR", "BAR", SolidColorBrush.Parse("#E2E8F0")),
        Symbol.Seven => ("7", "SEVEN", SolidColorBrush.Parse("#FF3B30")),
        Symbol.Wild => ("★", "WILD", SolidColorBrush.Parse("#00F5D4")),
        _ => ("?", "?", Brushes.White),
    };

    internal static Symbol PreviousSymbol(Symbol symbol)
    {
        int idx = Array.IndexOf(StripOrder, symbol);
        return idx < 0 ? Symbol.Cherry : StripOrder[(idx - 1 + StripOrder.Length) % StripOrder.Length];
    }

    internal static Symbol NextSymbol(Symbol symbol)
    {
        int idx = Array.IndexOf(StripOrder, symbol);
        return idx < 0 ? Symbol.Lemon : StripOrder[(idx + 1) % StripOrder.Length];
    }
}

/// <summary>Visual state for one vertical slot reel (top peek, center payline, bottom peek).</summary>
public sealed class ReelState : INotifyPropertyChanged
{
    private static readonly IBrush DefaultBrush = SolidColorBrush.Parse("#F5D061");

    private string _topIcon = "★";
    private string _centerIcon = "?";
    private string _centerLabel = "READY";
    private IBrush _centerBrush = DefaultBrush;
    private string _bottomIcon = "🍒";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TopIcon
    {
        get => _topIcon;
        private set { _topIcon = value; OnPropertyChanged(); }
    }

    public string CenterIcon
    {
        get => _centerIcon;
        private set { _centerIcon = value; OnPropertyChanged(); }
    }

    public string CenterLabel
    {
        get => _centerLabel;
        private set { _centerLabel = value; OnPropertyChanged(); }
    }

    public IBrush CenterBrush
    {
        get => _centerBrush;
        private set { _centerBrush = value; OnPropertyChanged(); }
    }

    public string BottomIcon
    {
        get => _bottomIcon;
        private set { _bottomIcon = value; OnPropertyChanged(); }
    }

    public void SetSymbol(Symbol symbol)
    {
        var (icon, label, brush) = ArcadeViewModel.DescribeSymbol(symbol);
        var (prevIcon, _, _) = ArcadeViewModel.DescribeSymbol(ArcadeViewModel.PreviousSymbol(symbol));
        var (nextIcon, _, _) = ArcadeViewModel.DescribeSymbol(ArcadeViewModel.NextSymbol(symbol));

        TopIcon = prevIcon;
        CenterIcon = icon;
        CenterLabel = label;
        CenterBrush = brush;
        BottomIcon = nextIcon;
    }

    public void Reset()
    {
        TopIcon = "★";
        CenterIcon = "?";
        CenterLabel = "READY";
        CenterBrush = DefaultBrush;
        BottomIcon = "🍒";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Selectable bet chip button model.</summary>
public sealed class BetOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public BetOption(int amount, bool isSelected, Action<int> onSelect)
    {
        Amount = amount;
        _isSelected = isSelected;
        SelectCommand = new RelayCommand(() => onSelect(amount));
    }

    public int Amount { get; }

    public string Label => $"{Amount}";

    public ICommand SelectCommand { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>Editable row for one symbol in the hidden Admin Odds menu.</summary>
public sealed class SymbolOddsItem : INotifyPropertyChanged
{
    private readonly Symbol _symbol;
    private readonly ReelOdds _odds;
    private readonly Action _onChanged;

    public SymbolOddsItem(Symbol symbol, ReelOdds odds, Action onChanged)
    {
        _symbol = symbol;
        _odds = odds;
        _onChanged = onChanged;
        var (icon, label, brush) = ArcadeViewModel.DescribeSymbol(symbol);
        Icon = icon;
        Label = label;
        Brush = brush;
        IncreaseCommand = new RelayCommand(() => Weight++);
        DecreaseCommand = new RelayCommand(() => Weight--);
    }

    public string Icon { get; }

    public string Label { get; }

    public IBrush Brush { get; }

    public ICommand IncreaseCommand { get; }

    public ICommand DecreaseCommand { get; }

    public int Weight
    {
        get => _odds.GetWeight(_symbol);
        set
        {
            int clamped = Math.Max(0, value);
            if (clamped == _odds.GetWeight(_symbol)) return;
            try
            {
                _odds.SetWeight(_symbol, clamped);
                _onChanged();
            }
            catch (InvalidOperationException)
            {
                OnPropertyChanged(nameof(Weight));
            }
        }
    }

    public string ProbabilityText => $"{_odds.GetProbability(_symbol) * 100:F1}%";

    public void SyncFromModel()
    {
        OnPropertyChanged(nameof(Weight));
        OnPropertyChanged(nameof(ProbabilityText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
