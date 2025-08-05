using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.ValueObjects;

public class CardExtensionsTests
{
    [Fact]
    public void CreateStandardDeck_ReturnsCorrectNumberOfCards()
    {
        // Act
        var deck = CardExtensions.CreateStandardDeck().ToList();

        // Assert
        Assert.Equal(52, deck.Count);
    }

    [Fact]
    public void CreateStandardDeck_ContainsAllSuits()
    {
        // Act
        var deck = CardExtensions.CreateStandardDeck().ToList();

        // Assert
        var suits = deck.Select(c => c.Suit).Distinct().ToList();
        Assert.Equal(4, suits.Count);
        Assert.Contains(Suit.Spades, suits);
        Assert.Contains(Suit.Hearts, suits);
        Assert.Contains(Suit.Diamonds, suits);
        Assert.Contains(Suit.Clubs, suits);
    }

    [Fact]
    public void CreateStandardDeck_ContainsAllRanks()
    {
        // Act
        var deck = CardExtensions.CreateStandardDeck().ToList();

        // Assert
        var ranks = deck.Select(c => c.Rank).Distinct().ToList();
        Assert.Equal(13, ranks.Count);
        
        foreach (var expectedRank in Enum.GetValues<Rank>())
        {
            Assert.Contains(expectedRank, ranks);
        }
    }

    [Fact]
    public void CreateStandardDeck_ContainsExactlyOneOfEachCard()
    {
        // Act
        var deck = CardExtensions.CreateStandardDeck().ToList();

        // Assert
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                var expectedCard = new Card(suit, rank);
                var count = deck.Count(c => c.Equals(expectedCard));
                Assert.Equal(1, count);
            }
        }
    }

    [Fact]
    public void AreAllValid_WithValidCards_ReturnsTrue()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Diamonds, Rank.Queen),
            new Card(Suit.Clubs, Rank.Jack)
        };

        // Act
        var result = cards.AreAllValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllValid_WithStandardDeck_ReturnsTrue()
    {
        // Arrange
        var cards = CardExtensions.CreateStandardDeck();

        // Act
        var result = cards.AreAllValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllValid_WithEmptyCollection_ReturnsTrue()
    {
        // Arrange
        var cards = Array.Empty<Card>();

        // Act
        var result = cards.AreAllValid();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(new[] { Rank.Two }, 2)]
    [InlineData(new[] { Rank.Three }, 3)]
    [InlineData(new[] { Rank.Four }, 4)]
    [InlineData(new[] { Rank.Five }, 5)]
    [InlineData(new[] { Rank.Six }, 6)]
    [InlineData(new[] { Rank.Seven }, 7)]
    [InlineData(new[] { Rank.Eight }, 8)]
    [InlineData(new[] { Rank.Nine }, 9)]
    [InlineData(new[] { Rank.Ten }, 10)]
    [InlineData(new[] { Rank.Jack }, 10)]
    [InlineData(new[] { Rank.Queen }, 10)]
    [InlineData(new[] { Rank.King }, 10)]
    public void GetBlackjackValue_WithSingleCard_ReturnsCorrectValue(Rank[] ranks, int expectedValue)
    {
        // Arrange
        var cards = ranks.Select(r => new Card(Suit.Hearts, r));

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void GetBlackjackValue_WithSingleAce_Returns11()
    {
        // Arrange
        var cards = new[] { new Card(Suit.Hearts, Rank.Ace) };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(11, value);
    }

    [Fact]
    public void GetBlackjackValue_WithAceAndTen_Returns21()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ten)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(21, value);
    }

    [Fact]
    public void GetBlackjackValue_WithTwoAces_Returns12()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ace)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(12, value); // One Ace as 11, one as 1
    }

    [Fact]
    public void GetBlackjackValue_WithThreeAces_Returns13()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Diamonds, Rank.Ace)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(13, value); // One Ace as 11, two as 1
    }

    [Fact]
    public void GetBlackjackValue_WithFourAces_Returns14()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Diamonds, Rank.Ace),
            new Card(Suit.Clubs, Rank.Ace)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(14, value); // One Ace as 11, three as 1
    }

    [Fact]
    public void GetBlackjackValue_WithAceAndCardsOver21_ConvertsAceToOne()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Six),
            new Card(Suit.Diamonds, Rank.Seven)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(14, value); // Ace as 1, 6 + 7 = 14
    }

    [Fact]
    public void GetBlackjackValue_WithMultipleAcesAndBustScenario_ConvertsAcesAsNeeded()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ace),
            new Card(Suit.Diamonds, Rank.Nine)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(21, value); // One Ace as 11, one as 1, plus 9
    }

    [Fact]
    public void GetBlackjackValue_WithComplexAceScenario_HandlesCorrectly()
    {
        // Test case: A, A, A, A, A, A, 5 should equal 21 (one Ace as 11, five as 1, plus 5)
        var cards = new List<Card>();
        for (int i = 0; i < 6; i++)
        {
            cards.Add(new Card(Suit.Hearts, Rank.Ace));
        }
        cards.Add(new Card(Suit.Spades, Rank.Five));

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(21, value);
    }

    [Fact]
    public void GetBlackjackValue_WithAllFaceCards_ReturnsCorrectValue()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Jack),
            new Card(Suit.Spades, Rank.Queen),
            new Card(Suit.Diamonds, Rank.King)
        };

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(30, value);
    }

    [Fact]
    public void GetBlackjackValue_WithEmptyCollection_ReturnsZero()
    {
        // Arrange
        var cards = Array.Empty<Card>();

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void FormatCards_WithMultipleCards_ReturnsCorrectFormat()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Diamonds, Rank.Queen)
        };

        // Act
        var result = cards.FormatCards();

        // Assert
        Assert.Equal("A of Hearts, K of Spades, Q of Diamonds", result);
    }

    [Fact]
    public void FormatCards_WithCustomSeparator_UsesCustomSeparator()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King)
        };

        // Act
        var result = cards.FormatCards(" | ");

        // Assert
        Assert.Equal("A of Hearts | K of Spades", result);
    }

    [Fact]
    public void FormatCards_WithSingleCard_ReturnsCardString()
    {
        // Arrange
        var cards = new[] { new Card(Suit.Hearts, Rank.Ace) };

        // Act
        var result = cards.FormatCards();

        // Assert
        Assert.Equal("A of Hearts", result);
    }

    [Fact]
    public void FormatCards_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var cards = Array.Empty<Card>();

        // Act
        var result = cards.FormatCards();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatCardsDetailed_WithMultipleCards_ReturnsCorrectFormat()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King),
            new Card(Suit.Diamonds, Rank.Queen)
        };

        // Act
        var result = cards.FormatCardsDetailed();

        // Assert
        Assert.Equal("A of Hearts, K of Spades, Q of Diamonds", result);
    }

    [Fact]
    public void FormatCardsDetailed_WithCustomSeparator_UsesCustomSeparator()
    {
        // Arrange
        var cards = new[]
        {
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.King)
        };

        // Act
        var result = cards.FormatCardsDetailed(" - ");

        // Assert
        Assert.Equal("A of Hearts - K of Spades", result);
    }

    [Theory]
    [InlineData(new[] { Rank.Two, Rank.Three }, 5)]
    [InlineData(new[] { Rank.Ten, Rank.Jack }, 20)]
    [InlineData(new[] { Rank.Ace, Rank.Five, Rank.Five }, 21)] // Ace as 11
    [InlineData(new[] { Rank.Ace, Rank.Six, Rank.Five }, 12)] // Ace as 1
    [InlineData(new[] { Rank.King, Rank.Queen, Rank.Jack }, 30)]
    public void GetBlackjackValue_WithVariousCombinations_ReturnsCorrectValue(Rank[] ranks, int expectedValue)
    {
        // Arrange
        var cards = ranks.Select(r => new Card(Suit.Hearts, r));

        // Act
        var value = cards.GetBlackjackValue();

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void CreateStandardDeck_IsEnumerable()
    {
        // Act
        var deck = CardExtensions.CreateStandardDeck();

        // Assert
        Assert.IsAssignableFrom<IEnumerable<Card>>(deck);
        
        // Should be able to enumerate multiple times
        var firstEnumeration = deck.ToList();
        var secondEnumeration = deck.ToList();
        
        Assert.Equal(firstEnumeration.Count, secondEnumeration.Count);
        Assert.Equal(52, firstEnumeration.Count);
    }

    [Fact]
    public void GetBlackjackValue_WithMixedCards_HandlesAcesCorrectly()
    {
        // Test various scenarios where Aces need to be adjusted
        var scenarios = new[]
        {
            (new[] { Rank.Ace, Rank.Ace, Rank.Ace, Rank.Eight }, 21), // A,A,A,8 = 1+1+1+8 = 11 (but one ace as 11 would bust, so all as 1) = 1+1+1+8 = 11, but we want one as 11 if possible = 11+1+1+8 = 21
            (new[] { Rank.Ace, Rank.Two, Rank.Three, Rank.Five }, 21), // A,2,3,5 = 11+2+3+5 = 21
            (new[] { Rank.Ace, Rank.Two, Rank.Three, Rank.Six }, 12), // A,2,3,6 = 1+2+3+6 = 12 (11+2+3+6 would be 22)
        };

        foreach (var (ranks, expectedValue) in scenarios)
        {
            // Arrange
            var cards = ranks.Select(r => new Card(Suit.Hearts, r));

            // Act
            var value = cards.GetBlackjackValue();

            // Assert
            Assert.Equal(expectedValue, value);
        }
    }
}