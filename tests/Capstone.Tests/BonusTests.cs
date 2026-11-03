using Capstone.Core.Model;

namespace Capstone.Tests;

/// <summary>
/// Lab S4: the bonus hierarchy, the polymorphic loop and the switch expression.
/// </summary>
public class BonusTests
{
    [Fact]
    public void DescribeBonus_SwitchExpression_DistinguishesEveryBonusKind()
    {
        Bonus freeSpins = new FreeSpinBonus { Spins = 5, Reason = "three bars" };
        Bonus oneSpin = new FreeSpinBonus { Spins = 1, Reason = "consolation" };
        Bonus credits = new CreditBonus { Credits = 40, Reason = "lucky lemon" };

        Assert.Equal("5 free spins — three bars", BonusRules.DescribeBonus(freeSpins));
        Assert.Equal("a free spin — consolation", BonusRules.DescribeBonus(oneSpin));
        Assert.Equal("+40 credits — lucky lemon", BonusRules.DescribeBonus(credits));
    }

    [Fact]
    public void PolymorphicLoop_CallsTheRightDerivedReward()
    {
        List<Bonus> bonuses =
        [
            new FreeSpinBonus { Spins = 3, Reason = "bars" },
            new CreditBonus { Credits = 25, Reason = "lemon" },
        ];

        // One loop, the abstract type, two different results.
        var rewards = bonuses.Select(bonus => bonus.Reward).ToList();

        Assert.Equal(["3 free spin(s)", "25 bonus credits"], rewards);
    }
}