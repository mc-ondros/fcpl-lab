namespace Capstone.Core.Model;

/// <summary>
/// A combination of three reel symbols resulting from a spin.
/// Compares by value for paytable evaluations.
/// </summary>
public readonly record struct Reels(Symbol First, Symbol Second, Symbol Third)
{
    public override string ToString() => $"[{First} | {Second} | {Third}]";
}
