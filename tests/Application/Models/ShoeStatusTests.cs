using GroupProject.Application.Models;
using Xunit;

namespace GroupProject.Tests.Application.Models;

public class ShoeStatusTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var deckCount = 6;
        var remainingCards = 156;
        var remainingPercentage = 0.5;
        var penetrationThreshold = 0.25;
        var needsReshuffle = true;
        var autoReshuffleEnabled = true;

        // Act
        var status = new ShoeStatus(
            deckCount,
            remainingCards,
            remainingPercentage,
            penetrationThreshold,
            needsReshuffle,
            autoReshuffleEnabled);

        // Assert
        Assert.Equal(deckCount, status.DeckCount);
        Assert.Equal(remainingCards, status.RemainingCards);
        Assert.Equal(remainingPercentage, status.RemainingPercentage);
        Assert.Equal(penetrationThreshold, status.PenetrationThreshold);
        Assert.Equal(needsReshuffle, status.NeedsReshuffle);
        Assert.Equal(autoReshuffleEnabled, status.AutoReshuffleEnabled);
    }

    [Fact]
    public void TotalCards_CalculatedCorrectly()
    {
        // Arrange
        var deckCount = 4;
        var expectedTotalCards = 4 * 52; // 208

        // Act
        var status = new ShoeStatus(deckCount, 100, 0.5, 0.25, false, true);

        // Assert
        Assert.Equal(expectedTotalCards, status.TotalCards);
    }

    [Fact]
    public void CardsDealt_CalculatedCorrectly()
    {
        // Arrange
        var deckCount = 2;
        var remainingCards = 60;
        var expectedCardsDealt = (2 * 52) - 60; // 104 - 60 = 44

        // Act
        var status = new ShoeStatus(deckCount, remainingCards, 0.5, 0.25, false, true);

        // Assert
        Assert.Equal(expectedCardsDealt, status.CardsDealt);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(52, false)]
    public void IsEmpty_ReturnsCorrectValue(int remainingCards, bool expectedIsEmpty)
    {
        // Act
        var status = new ShoeStatus(1, remainingCards, 0.5, 0.25, false, true);

        // Assert
        Assert.Equal(expectedIsEmpty, status.IsEmpty);
    }

    [Theory]
    [InlineData(0.0, true)]   // 0% remaining
    [InlineData(0.03, true)]  // 3% remaining
    [InlineData(0.04, true)]  // 4% remaining
    [InlineData(0.05, false)] // 5% remaining
    [InlineData(0.1, false)]  // 10% remaining
    [InlineData(1.0, false)]  // 100% remaining
    public void IsNearlyEmpty_ReturnsCorrectValue(double remainingPercentage, bool expectedIsNearlyEmpty)
    {
        // Act
        var status = new ShoeStatus(1, 52, remainingPercentage, 0.25, false, true);

        // Assert
        Assert.Equal(expectedIsNearlyEmpty, status.IsNearlyEmpty);
    }

    [Fact]
    public void ToString_WithNormalStatus_ReturnsCorrectString()
    {
        // Arrange
        var status = new ShoeStatus(6, 200, 0.64, 0.25, false, true);

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("Shoe: 200/312 cards (64.0%)", result);
    }

    [Fact]
    public void ToString_WithReshuffleNeeded_IncludesReshuffleMessage()
    {
        // Arrange
        var status = new ShoeStatus(2, 20, 0.19, 0.25, true, true);

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("Shoe: 20/104 cards (19.0%) - RESHUFFLE NEEDED", result);
    }

    [Fact]
    public void ToString_WithEmptyShoe_ShowsZeroCards()
    {
        // Arrange
        var status = new ShoeStatus(1, 0, 0.0, 0.25, true, true);

        // Act
        var result = status.ToString();

        // Assert
        Assert.Equal("Shoe: 0/52 cards (0.0%) - RESHUFFLE NEEDED", result);
    }

    [Theory]
    [InlineData(1, 52, 0, 52)]
    [InlineData(2, 104, 50, 54)]
    [InlineData(6, 312, 100, 212)]
    [InlineData(8, 416, 416, 0)]
    public void Properties_WithVariousScenarios_CalculateCorrectly(
        int deckCount, 
        int expectedTotalCards, 
        int remainingCards, 
        int expectedCardsDealt)
    {
        // Act
        var status = new ShoeStatus(deckCount, remainingCards, 0.5, 0.25, false, true);

        // Assert
        Assert.Equal(expectedTotalCards, status.TotalCards);
        Assert.Equal(remainingCards, status.RemainingCards);
        Assert.Equal(expectedCardsDealt, status.CardsDealt);
    }

    [Fact]
    public void Constructor_WithAllFalseFlags_SetsPropertiesCorrectly()
    {
        // Act
        var status = new ShoeStatus(1, 52, 1.0, 0.5, false, false);

        // Assert
        Assert.False(status.NeedsReshuffle);
        Assert.False(status.AutoReshuffleEnabled);
        Assert.False(status.IsEmpty);
        Assert.False(status.IsNearlyEmpty);
    }

    [Fact]
    public void Constructor_WithAllTrueFlags_SetsPropertiesCorrectly()
    {
        // Act
        var status = new ShoeStatus(1, 0, 0.0, 0.25, true, true);

        // Assert
        Assert.True(status.NeedsReshuffle);
        Assert.True(status.AutoReshuffleEnabled);
        Assert.True(status.IsEmpty);
        Assert.True(status.IsNearlyEmpty);
    }
}