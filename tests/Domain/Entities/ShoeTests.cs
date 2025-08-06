using GroupProject.Domain.Entities;
using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

public class ShoeTests
{
    private readonly Mock<IRandomProvider> _mockRandomProvider;

    public ShoeTests()
    {
        _mockRandomProvider = new Mock<IRandomProvider>();
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var shoe = new Shoe(6, _mockRandomProvider.Object);

        // Assert
        Assert.Equal(6, shoe.DeckCount);
        Assert.Equal(312, shoe.RemainingCards); // 6 decks * 52 cards
        Assert.False(shoe.IsEmpty);
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Theory]
    [InlineData(1, 52)]
    [InlineData(2, 104)]
    [InlineData(4, 208)]
    [InlineData(8, 416)]
    public void Constructor_WithDifferentDeckCounts_InitializesCorrectCardCount(int deckCount, int expectedCards)
    {
        // Arrange & Act
        var shoe = new Shoe(deckCount, _mockRandomProvider.Object);

        // Assert
        Assert.Equal(deckCount, shoe.DeckCount);
        Assert.Equal(expectedCards, shoe.RemainingCards);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Constructor_WithInvalidDeckCount_ThrowsArgumentOutOfRangeException(int invalidDeckCount)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Shoe(invalidDeckCount, _mockRandomProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullRandomProvider_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Shoe(1, null!));
    }

    [Fact]
    public void Draw_WithCardsRemaining_ReturnsCardAndReducesCount()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var initialCount = shoe.RemainingCards;

        // Act
        var card = shoe.Draw();

        // Assert
        Assert.IsType<Card>(card);
        Assert.Equal(initialCount - 1, shoe.RemainingCards);
    }

    [Fact]
    public void Draw_FromEmptyShoe_ThrowsInvalidOperationException()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        
        // Draw all cards
        while (!shoe.IsEmpty)
        {
            shoe.Draw();
        }

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shoe.Draw());
    }

    [Fact]
    public void Draw_AllCardsFromMultipleDeckShoe_ReturnsCorrectCards()
    {
        // Arrange
        var shoe = new Shoe(2, _mockRandomProvider.Object);
        var drawnCards = new List<Card>();

        // Act
        while (!shoe.IsEmpty)
        {
            drawnCards.Add(shoe.Draw());
        }

        // Assert
        Assert.Equal(104, drawnCards.Count); // 2 decks * 52 cards
        
        // Verify we have exactly 2 of each card
        var standardDeck = CardExtensions.CreateStandardDeck().ToList();
        foreach (var expectedCard in standardDeck)
        {
            var count = drawnCards.Count(c => c.Equals(expectedCard));
            Assert.Equal(2, count); // Should have exactly 2 of each card
        }
    }

    [Fact]
    public void IsEmpty_WithNewShoe_ReturnsFalse()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act & Assert
        Assert.False(shoe.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterDrawingAllCards_ReturnsTrue()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act
        while (shoe.RemainingCards > 0)
        {
            shoe.Draw();
        }

        // Assert
        Assert.True(shoe.IsEmpty);
        Assert.Equal(0, shoe.RemainingCards);
    }

    [Fact]
    public void Shuffle_CallsRandomProviderShuffle()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        _mockRandomProvider.Reset(); // Reset to clear constructor call

        // Act
        shoe.Shuffle();

        // Assert
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Fact]
    public void Reset_RestoresFullShoeAndShuffles()
    {
        // Arrange
        var shoe = new Shoe(2, _mockRandomProvider.Object);
        var originalCount = shoe.RemainingCards;
        
        // Draw some cards
        for (int i = 0; i < 20; i++)
        {
            shoe.Draw();
        }
        
        Assert.Equal(originalCount - 20, shoe.RemainingCards);
        _mockRandomProvider.Reset(); // Reset to clear previous calls

        // Act
        shoe.Reset();

        // Assert
        Assert.Equal(originalCount, shoe.RemainingCards);
        Assert.False(shoe.IsEmpty);
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Theory]
    [InlineData(1, 52, 1.0)]
    [InlineData(1, 26, 0.5)]
    [InlineData(1, 13, 0.25)]
    [InlineData(1, 0, 0.0)]
    [InlineData(6, 312, 1.0)]
    [InlineData(6, 156, 0.5)]
    public void GetRemainingPercentage_WithVariousCardCounts_ReturnsCorrectPercentage(
        int deckCount, int remainingCards, double expectedPercentage)
    {
        // Arrange
        var shoe = new Shoe(deckCount, _mockRandomProvider.Object);
        var totalCards = deckCount * 52;
        var cardsToDraw = totalCards - remainingCards;
        
        for (int i = 0; i < cardsToDraw; i++)
        {
            shoe.Draw();
        }

        // Act
        var percentage = shoe.GetRemainingPercentage();

        // Assert
        Assert.Equal(expectedPercentage, percentage, 3); // 3 decimal places precision
    }

    [Theory]
    [InlineData(0.25, 0.3, false)] // 30% remaining, threshold 25% - no reshuffle needed
    [InlineData(0.25, 0.2, true)]  // 20% remaining, threshold 25% - reshuffle needed
    [InlineData(0.5, 0.4, true)]   // 40% remaining, threshold 50% - reshuffle needed
    [InlineData(0.5, 0.6, false)]  // 60% remaining, threshold 50% - no reshuffle needed
    public void NeedsReshuffle_WithVariousThresholds_ReturnsCorrectResult(
        double threshold, double remainingPercentage, bool expectedNeedsReshuffle)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var totalCards = 52;
        var targetRemainingCards = (int)(totalCards * remainingPercentage);
        var cardsToDraw = totalCards - targetRemainingCards;
        
        for (int i = 0; i < cardsToDraw; i++)
        {
            shoe.Draw();
        }

        // Act
        var needsReshuffle = shoe.NeedsReshuffle(threshold);

        // Assert
        Assert.Equal(expectedNeedsReshuffle, needsReshuffle);
    }

    [Fact]
    public void NeedsReshuffle_WithDefaultThreshold_UsesQuarterThreshold()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        
        // Draw cards to get below 25%
        for (int i = 0; i < 40; i++) // Leave 12 cards (23%)
        {
            shoe.Draw();
        }

        // Act
        var needsReshuffle = shoe.NeedsReshuffle();

        // Assert
        Assert.True(needsReshuffle);
    }

    [Fact]
    public void ToString_WithFullShoe_ReturnsCorrectString()
    {
        // Arrange
        var shoe = new Shoe(6, _mockRandomProvider.Object);

        // Act
        var result = shoe.ToString();

        // Assert
        Assert.Equal("Shoe with 6 decks, 312 cards remaining (100.0%)", result);
    }

    [Fact]
    public void ToString_WithPartialShoe_ReturnsCorrectString()
    {
        // Arrange
        var shoe = new Shoe(2, _mockRandomProvider.Object);
        
        // Draw half the cards
        for (int i = 0; i < 52; i++)
        {
            shoe.Draw();
        }

        // Act
        var result = shoe.ToString();

        // Assert
        Assert.Equal("Shoe with 2 decks, 52 cards remaining (50.0%)", result);
    }

    [Fact]
    public void ToString_WithEmptyShoe_ReturnsCorrectString()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        while (!shoe.IsEmpty)
        {
            shoe.Draw();
        }

        // Act
        var result = shoe.ToString();

        // Assert
        Assert.Equal("Shoe with 1 decks, 0 cards remaining (0.0%)", result);
    }

    [Fact]
    public void RemainingCards_DecreasesWithEachDraw()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var initialCount = shoe.RemainingCards;

        // Act & Assert
        for (int i = 1; i <= 5; i++)
        {
            shoe.Draw();
            Assert.Equal(initialCount - i, shoe.RemainingCards);
        }
    }

    [Fact]
    public void DeckCount_RemainsConstantThroughoutOperations()
    {
        // Arrange
        var shoe = new Shoe(4, _mockRandomProvider.Object);
        var originalDeckCount = shoe.DeckCount;

        // Act
        shoe.Draw();
        shoe.Shuffle();
        shoe.Reset();

        // Assert
        Assert.Equal(originalDeckCount, shoe.DeckCount);
    }

    // Enhanced Shoe Tests for Automatic Reshuffling

    [Fact]
    public void PenetrationThreshold_DefaultValue_IsQuarter()
    {
        // Arrange & Act
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Assert
        Assert.Equal(0.25, shoe.PenetrationThreshold);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void PenetrationThreshold_SetValidValue_UpdatesCorrectly(double threshold)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act
        shoe.PenetrationThreshold = threshold;

        // Assert
        Assert.Equal(threshold, shoe.PenetrationThreshold);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void PenetrationThreshold_SetInvalidValue_ThrowsArgumentOutOfRangeException(double invalidThreshold)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => shoe.PenetrationThreshold = invalidThreshold);
    }

    [Fact]
    public void AutoReshuffleEnabled_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Assert
        Assert.True(shoe.AutoReshuffleEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutoReshuffleEnabled_SetValue_UpdatesCorrectly(bool enabled)
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);

        // Act
        shoe.AutoReshuffleEnabled = enabled;

        // Assert
        Assert.Equal(enabled, shoe.AutoReshuffleEnabled);
    }

    [Fact]
    public void ReshuffleNeeded_Event_RaisedWhenThresholdReached()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        shoe.PenetrationThreshold = 0.5; // 50% threshold
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoe.ReshuffleNeeded += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act - Draw cards to reach threshold
        for (int i = 0; i < 27; i++) // Leave 25 cards (48%)
        {
            shoe.Draw();
        }

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Contains("Automatic reshuffle triggered", eventArgs.Reason);
        Assert.Equal(0.5, eventArgs.PenetrationThreshold);
    }

    [Fact]
    public void ReshuffleNeeded_Event_NotRaisedWhenAutoReshuffleDisabled()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        shoe.AutoReshuffleEnabled = false;
        shoe.PenetrationThreshold = 0.5;
        var eventRaised = false;

        shoe.ReshuffleNeeded += (sender, e) => eventRaised = true;

        // Act - Draw cards to reach threshold
        for (int i = 0; i < 27; i++) // Leave 25 cards (48%)
        {
            shoe.Draw();
        }

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void TriggerReshuffle_WithDefaultReason_RaisesReshuffledEvent()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoe.Reshuffled += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Draw some cards first
        for (int i = 0; i < 10; i++)
        {
            shoe.Draw();
        }

        // Act
        shoe.TriggerReshuffle();

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal("Manual reshuffle", eventArgs.Reason);
        Assert.Equal(52, shoe.RemainingCards); // Should be reset to full
    }

    [Fact]
    public void TriggerReshuffle_WithCustomReason_RaisesReshuffledEventWithReason()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var customReason = "End of round reshuffle";
        var eventRaised = false;
        ShoeReshuffleEventArgs? eventArgs = null;

        shoe.Reshuffled += (sender, e) =>
        {
            eventRaised = true;
            eventArgs = e;
        };

        // Act
        shoe.TriggerReshuffle(customReason);

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(eventArgs);
        Assert.Equal(customReason, eventArgs.Reason);
    }

    [Fact]
    public void TriggerReshuffle_ResetsShoeToFullDeck()
    {
        // Arrange
        var shoe = new Shoe(2, _mockRandomProvider.Object);
        
        // Draw half the cards
        for (int i = 0; i < 52; i++)
        {
            shoe.Draw();
        }
        
        Assert.Equal(52, shoe.RemainingCards); // Verify we drew cards

        // Act
        shoe.TriggerReshuffle("Test reshuffle");

        // Assert
        Assert.Equal(104, shoe.RemainingCards); // Should be back to full 2-deck shoe
        Assert.Equal(1.0, shoe.GetRemainingPercentage()); // 100%
    }

    [Fact]
    public void Draw_WhenAutoReshuffleEnabled_ChecksForReshuffleNeeded()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        shoe.PenetrationThreshold = 0.9; // Very high threshold to trigger easily
        var reshuffleNeededEventCount = 0;

        shoe.ReshuffleNeeded += (sender, e) => reshuffleNeededEventCount++;

        // Act - Draw cards to trigger reshuffle check
        for (int i = 0; i < 10; i++) // Draw 10 cards (80% remaining)
        {
            shoe.Draw();
        }

        // Assert
        Assert.True(reshuffleNeededEventCount > 0);
    }

    [Fact]
    public void Multiple_Events_CanBeSubscribed()
    {
        // Arrange
        var shoe = new Shoe(1, _mockRandomProvider.Object);
        var reshuffleNeededCount = 0;
        var reshuffledCount = 0;

        shoe.ReshuffleNeeded += (sender, e) => reshuffleNeededCount++;
        shoe.Reshuffled += (sender, e) => reshuffledCount++;

        // Act
        shoe.TriggerReshuffle("Test multiple events");

        // Assert
        Assert.Equal(1, reshuffledCount);
        // ReshuffleNeeded might or might not be called depending on the state
    }
}