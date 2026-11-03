namespace Capstone.Core.Model;

/// <summary>
/// A prize the machine can award on top of the regular payline.
/// Abstract so the machine can hand out different kinds of bonus
/// and the caller can treat them uniformly.
/// </summary>
public abstract class Bonus : ISlotEntity
{
    /// <summary>Why the bonus was awarded, e.g. the reels that triggered it.</summary>
    public required string Reason { get; init; }

    /// <summary>What the player receives. Each derived type fills this in differently.</summary>
    public abstract string Reward { get; }

    public abstract string Describe();
}

/// <summary>
/// Awards extra spins that cost the player nothing.
/// </summary>
public sealed class FreeSpinBonus : Bonus
{
    public required int Spins { get; init; }

    public override string Reward => $"{Spins} free spin(s)";

    public override string Describe() => $"Free spins: {Spins} on the house ({Reason}).";
}

/// <summary>
/// Awards a lump of credits straight onto the session balance.
/// </summary>
public sealed class CreditBonus : Bonus
{
    public required int Credits { get; init; }

    public override string Reward => $"{Credits} bonus credits";

    public override string Describe() => $"Credit bonus: +{Credits} credits ({Reason}).";
}
