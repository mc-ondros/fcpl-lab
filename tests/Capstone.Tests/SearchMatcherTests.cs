using SharpQuest.Contracts;

namespace Capstone.Tests;

/// <summary>
/// Lab 03: Tests covering all 8 SearchMatcher specification rules (R1–R8)
/// to catch all 8 mutants.
/// </summary>
public class SearchMatcherTests
{
    private readonly SearchMatcher _matcher = SearchMatcher.Create();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void R1_EmptyOrWhitespaceQuery_MatchesNonNullText(string query)
    {
        Assert.True(_matcher.Matches("Cherry Cherry Lemon", query));
    }

    [Fact]
    public void R2_Matching_IgnoresCase()
    {
        Assert.True(_matcher.Matches("Pikachu", "PIKA"));
    }

    [Theory]
    [InlineData("Café", "cafe")]
    [InlineData("Sosea", "șosea")]
    public void R3_Matching_IgnoresDiacriticsInBothDirections(string text, string query)
    {
        Assert.True(_matcher.Matches(text, query));
    }

    [Fact]
    public void R4_EveryWordInQueryMustMatch()
    {
        Assert.True(_matcher.Matches("fire lizard dragon", "fire dragon"));
        Assert.False(_matcher.Matches("fire lizard", "fire water"));
    }

    [Fact]
    public void R5_WordOrderDoesNotMatter()
    {
        Assert.True(_matcher.Matches("fire lizard", "lizard fire"));
    }

    [Fact]
    public void R6_WordMayOccurInsideLongerWord()
    {
        Assert.True(_matcher.Matches("Pikachu", "chu"));
    }

    [Fact]
    public void R7_NullText_ReturnsFalseWithoutThrowing()
    {
        Assert.False(_matcher.Matches(null, "cherry"));
        Assert.False(_matcher.Matches(null, ""));
    }

    [Fact]
    public void R8_NullQuery_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _matcher.Matches("Pikachu", null!));
        Assert.Throws<ArgumentNullException>(() => _matcher.Matches(null, null!));
    }
}

