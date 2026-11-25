using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Capstone.Core.Model;
using Capstone.Core.Storage;
using SharpQuest.Contracts;

namespace Capstone.Core.ViewModels;

/// <summary>
/// UI-agnostic MVVM view model over <see cref="IItemStore"/> (Lab 09).
/// </summary>
public sealed class CatalogueViewModel : ICatalogueViewModel
{
    private readonly IItemStore _store;
    private readonly ObservableCollection<IItem> _items = [];
    private readonly DelegateCommand _addCommand;
    private readonly DelegateCommand _removeCommand;

    private string _filter = string.Empty;
    private IItem? _selected;

    public CatalogueViewModel(IItemStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;

        if (_store is SpinHistoryStore observableStore)
        {
            observableStore.ItemAdded += OnStoreItemAdded;
            observableStore.ItemRemoved += OnStoreItemRemoved;
        }

        _addCommand = new DelegateCommand(
            param =>
            {
                if (param is IItem item)
                {
                    _store.Add(item);
                    if (_store is not SpinHistoryStore)
                        RefreshItems();
                }
            });

        _removeCommand = new DelegateCommand(
            _ =>
            {
                if (_selected is null) return;
                string id = _selected.Id;
                _store.Remove(id);
                Selected = null;
                if (_store is not SpinHistoryStore)
                    RefreshItems();
            },
            _ => _selected is not null);

        RefreshItems();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IEnumerable<IItem> Items => _items;

    public string Filter
    {
        get => _filter;
        set
        {
            string normalized = value ?? string.Empty;
            if (string.Equals(_filter, normalized, StringComparison.Ordinal))
                return;

            _filter = normalized;
            OnPropertyChanged();
            RefreshItems();
        }
    }

    public IItem? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value))
                return;

            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Details));
            _removeCommand.RaiseCanExecuteChanged();
        }
    }

    public string Details => _selected switch
    {
        null => "No item selected.",
        Bonus bonus => bonus.Describe(),
        Spin spin => spin.Describe(),
        _ => _selected.DisplayName,
    };

    public int Count => _items.Count;

    public ICommand AddCommand => _addCommand;

    public ICommand RemoveCommand => _removeCommand;

    private void OnStoreItemAdded(IItem item)
    {
        if (!MatchesFilter(item))
            return;

        _items.Add(item);
        OnPropertyChanged(nameof(Count));
    }

    private void OnStoreItemRemoved(IItem item)
    {
        if (_items.Remove(item))
            OnPropertyChanged(nameof(Count));
    }

    private bool MatchesFilter(IItem item) =>
        string.IsNullOrWhiteSpace(_filter) ||
        item.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase);

    private void RefreshItems()
    {
        int previousCount = _items.Count;
        _items.Clear();

        foreach (var item in _store.All())
        {
            if (MatchesFilter(item))
                _items.Add(item);
        }

        if (_items.Count != previousCount)
            OnPropertyChanged(nameof(Count));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

