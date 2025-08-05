using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

public class DeckTests
{
    private readonly Mock<IRandomProvider> _mockRandomProvider;

    public DeckTests()
    {
        _mockRandomProvider = new Mock<IRandomProvider>();
    }

    [Fact]
    public void Constructor_WithValidRandomProvider_InitializesCorrectly()
    {
        // Arrange & Act
        var deck = new Deck(_mockRandomProvider.Object);

        // Assert
        Assert.Equal(52, deck.RemainingCards);
        Assert.False(deck.IsEmpty);
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullRandomProvider_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Deck(null!));
    }

    [Fact]
    public void Draw_WithCardsRemaining_ReturnsCardAndReducesCount()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        var initialCount = deck.RemainingCards;

        // Act
        var card = deck.Draw();

        // Assert
        Assert.IsType<Card>(card);
        Assert.Equal(initialCount - 1, deck.RemainingCards);
    }

    [Fact]
    public void Draw_FromEmptyDeck_ThrowsInvalidOperationException()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        
        // Draw all cards
        while (!deck.IsEmpty)
        {
            deck.Draw();
        }

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => deck.Draw());
    }

    [Fact]
    public void Draw_AllCards_ReturnsStandardDeck()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        var drawnCards = new List<Card>();

        // Act
        while (!deck.IsEmpty)
        {
            drawnCards.Add(deck.Draw());
        }

        // Assert
        Assert.Equal(52, drawnCards.Count);
        
        // Verify we have all suits and ranks
        var expectedCards = CardExtensions.CreateStandardDeck().ToList();
        Assert.Equal(expectedCards.Count, drawnCards.Count);
        
        // Check that all expected cards are present (order may be different due to shuffling)
        foreach (var expectedCard in expectedCards)
        {
            Assert.Contains(expectedCard, drawnCards);
        }
    }

    [Fact]
    public void IsEmpty_WithNewDeck_ReturnsFalse()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);

        // Act & Assert
        Assert.False(deck.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterDrawingAllCards_ReturnsTrue()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);

        // Act
        while (deck.RemainingCards > 0)
        {
            deck.Draw();
        }

        // Assert
        Assert.True(deck.IsEmpty);
        Assert.Equal(0, deck.RemainingCards);
    }

    [Fact]
    public void Shuffle_CallsRandomProviderShuffle()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        _mockRandomProvider.Reset(); // Reset to clear constructor call

        // Act
        deck.Shuffle();

        // Assert
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Fact]
    public void Reset_RestoresFullDeckAndShuffles()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        
        // Draw some cards
        for (int i = 0; i < 10; i++)
        {
            deck.Draw();
        }
        
        Assert.Equal(42, deck.RemainingCards);
        _mockRandomProvider.Reset(); // Reset to clear previous calls

        // Act
        deck.Reset();

        // Assert
        Assert.Equal(52, deck.RemainingCards);
        Assert.False(deck.IsEmpty);
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Once);
    }

    [Fact]
    public void Reset_AfterCompletelyEmpty_RestoresFullDeck()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        
        // Draw all cards
        while (!deck.IsEmpty)
        {
            deck.Draw();
        }
        
        Assert.True(deck.IsEmpty);

        // Act
        deck.Reset();

        // Assert
        Assert.Equal(52, deck.RemainingCards);
        Assert.False(deck.IsEmpty);
    }

    [Fact]
    public void ToString_WithFullDeck_ReturnsCorrectString()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);

        // Act
        var result = deck.ToString();

        // Assert
        Assert.Equal("Deck with 52 cards remaining", result);
    }

    [Fact]
    public void ToString_WithPartialDeck_ReturnsCorrectString()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        deck.Draw();
        deck.Draw();

        // Act
        var result = deck.ToString();

        // Assert
        Assert.Equal("Deck with 50 cards remaining", result);
    }

    [Fact]
    public void ToString_WithEmptyDeck_ReturnsCorrectString()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        while (!deck.IsEmpty)
        {
            deck.Draw();
        }

        // Act
        var result = deck.ToString();

        // Assert
        Assert.Equal("Deck with 0 cards remaining", result);
    }

    [Fact]
    public void RemainingCards_DecreasesWithEachDraw()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        var initialCount = deck.RemainingCards;

        // Act & Assert
        for (int i = 1; i <= 5; i++)
        {
            deck.Draw();
            Assert.Equal(initialCount - i, deck.RemainingCards);
        }
    }

    [Fact]
    public void MultipleShuffle_CallsRandomProviderMultipleTimes()
    {
        // Arrange
        var deck = new Deck(_mockRandomProvider.Object);
        _mockRandomProvider.Reset(); // Reset to clear constructor call

        // Act
        deck.Shuffle();
        deck.Shuffle();
        deck.Shuffle();

        // Assert
        _mockRandomProvider.Verify(x => x.Shuffle(It.IsAny<List<Card>>()), Times.Exactly(3));
    }
}