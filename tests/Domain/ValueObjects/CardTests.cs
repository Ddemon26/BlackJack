using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.ValueObjects;

public class CardTests
{
    [Theory]
    [InlineData(Suit.Hearts, Rank.Ace, "A of Hearts")]
    [InlineData(Suit.Spades, Rank.King, "K of Spades")]
    [InlineData(Suit.Diamonds, Rank.Queen, "Q of Diamonds")]
    [InlineData(Suit.Clubs, Rank.Jack, "J of Clubs")]
    [InlineData(Suit.Hearts, Rank.Ten, "10 of Hearts")]
    [InlineData(Suit.Spades, Rank.Nine, "9 of Spades")]
    [InlineData(Suit.Diamonds, Rank.Two, "2 of Diamonds")]
    public void ToString_WithVariousCards_ReturnsCorrectFormat(Suit suit, Rank rank, string expected)
    {
        // Arrange
        var card = new Card(suit, rank);

        // Act
        var result = card.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Constructor_WithValidSuitAndRank_InitializesCorrectly()
    {
        // Arrange & Act
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Assert
        Assert.Equal(Suit.Hearts, card.Suit);
        Assert.Equal(Rank.Ace, card.Rank);
    }

    [Fact]
    public void Equals_WithSameCard_ReturnsTrue()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Hearts, Rank.Ace);

        // Act & Assert
        Assert.Equal(card1, card2);
        Assert.True(card1.Equals(card2));
    }

    [Fact]
    public void Equals_WithDifferentSuit_ReturnsFalse()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Spades, Rank.Ace);

        // Act & Assert
        Assert.NotEqual(card1, card2);
        Assert.False(card1.Equals(card2));
    }

    [Fact]
    public void Equals_WithDifferentRank_ReturnsFalse()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Hearts, Rank.King);

        // Act & Assert
        Assert.NotEqual(card1, card2);
        Assert.False(card1.Equals(card2));
    }

    [Fact]
    public void GetHashCode_WithSameCard_ReturnsSameHashCode()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Hearts, Rank.Ace);

        // Act
        var hash1 = card1.GetHashCode();
        var hash2 = card2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentCards_ReturnsDifferentHashCodes()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Spades, Rank.King);

        // Act
        var hash1 = card1.GetHashCode();
        var hash2 = card2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Card_IsValueType()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = card1; // Copy

        // Act
        card2 = new Card(Suit.Spades, Rank.King); // Reassign

        // Assert
        Assert.Equal(Suit.Hearts, card1.Suit); // Original unchanged
        Assert.Equal(Rank.Ace, card1.Rank);
        Assert.Equal(Suit.Spades, card2.Suit);
        Assert.Equal(Rank.King, card2.Rank);
    }

    [Theory]
    [InlineData(Suit.Spades)]
    [InlineData(Suit.Hearts)]
    [InlineData(Suit.Diamonds)]
    [InlineData(Suit.Clubs)]
    public void Card_WithAllSuits_InitializesCorrectly(Suit suit)
    {
        // Arrange & Act
        var card = new Card(suit, Rank.Ace);

        // Assert
        Assert.Equal(suit, card.Suit);
        Assert.Equal(Rank.Ace, card.Rank);
    }

    [Theory]
    [InlineData(Rank.Ace)]
    [InlineData(Rank.Two)]
    [InlineData(Rank.Three)]
    [InlineData(Rank.Four)]
    [InlineData(Rank.Five)]
    [InlineData(Rank.Six)]
    [InlineData(Rank.Seven)]
    [InlineData(Rank.Eight)]
    [InlineData(Rank.Nine)]
    [InlineData(Rank.Ten)]
    [InlineData(Rank.Jack)]
    [InlineData(Rank.Queen)]
    [InlineData(Rank.King)]
    public void Card_WithAllRanks_InitializesCorrectly(Rank rank)
    {
        // Arrange & Act
        var card = new Card(Suit.Hearts, rank);

        // Assert
        Assert.Equal(Suit.Hearts, card.Suit);
        Assert.Equal(rank, card.Rank);
    }

    [Fact]
    public void Card_SupportsDeconstruction()
    {
        // Arrange
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Act
        var (suit, rank) = card;

        // Assert
        Assert.Equal(Suit.Hearts, suit);
        Assert.Equal(Rank.Ace, rank);
    }

    [Fact]
    public void Card_SupportsPatternMatching()
    {
        // Arrange
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Act & Assert
        var result = card switch
        {
            { Suit: Suit.Hearts, Rank: Rank.Ace } => "Ace of Hearts",
            { Suit: Suit.Spades, Rank: Rank.Ace } => "Ace of Spades",
            _ => "Other card"
        };

        Assert.Equal("Ace of Hearts", result);
    }

    [Fact]
    public void ToString_WithAllFaceCards_UsesCorrectAbbreviations()
    {
        // Arrange
        var ace = new Card(Suit.Hearts, Rank.Ace);
        var jack = new Card(Suit.Spades, Rank.Jack);
        var queen = new Card(Suit.Diamonds, Rank.Queen);
        var king = new Card(Suit.Clubs, Rank.King);

        // Act & Assert
        Assert.Equal("A of Hearts", ace.ToString());
        Assert.Equal("J of Spades", jack.ToString());
        Assert.Equal("Q of Diamonds", queen.ToString());
        Assert.Equal("K of Clubs", king.ToString());
    }

    [Fact]
    public void ToString_WithAllSuits_UsesFullSuitNames()
    {
        // Arrange
        var spades = new Card(Suit.Spades, Rank.Ace);
        var hearts = new Card(Suit.Hearts, Rank.Ace);
        var diamonds = new Card(Suit.Diamonds, Rank.Ace);
        var clubs = new Card(Suit.Clubs, Rank.Ace);

        // Act & Assert
        Assert.Equal("A of Spades", spades.ToString());
        Assert.Equal("A of Hearts", hearts.ToString());
        Assert.Equal("A of Diamonds", diamonds.ToString());
        Assert.Equal("A of Clubs", clubs.ToString());
    }
}