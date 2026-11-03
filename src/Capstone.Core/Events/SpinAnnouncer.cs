using Capstone.Core.Model;

namespace Capstone.Core.Events;

/// <summary>
/// First subscriber: narrates every spin. Writes to a sink the caller
/// supplies so the arcade demo can print to the console while tests
/// capture the lines instead.
/// </summary>
public sealed class SpinAnnouncer
{
    private readonly Action<string> _write;

    public SpinAnnouncer(Action<string> write) => _write = write;

    public void OnSpinCompleted(object? sender, SpinCompletedEventArgs e) =>
        _write($"{e.Spin.Reels} -> {e.Spin.Payout} credits");

    public void Subscribe(SlotMachine machine) => machine.SpinCompleted += OnSpinCompleted;
}
