using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

public class HandTests
{
    [Fact]
    public void Constructor_CreatesEmptyHand()
    {
        // Arrange & Act
        var hand = new Hand();

        // Assert
        Assert.Empty(hand.Cards);
        Assert.Equal(0, hand.CardCount);
    }

    [Fact]
    public void AddCard_AddsCardToHand()
    {
        // Arrange
        var hand = new Hand();
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Act
        hand.AddCard(card);

        // Assert
        Assert.Single(hand.Cards);
        Assert.Equal(card, hand.Cards[0]);
        Assert.Equal(1, hand.CardCount);
    }

    [Fact]
    public void Clear_RemovesAllCards()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        hand.Clear();

        // Assert
        Assert.Empty(hand.Cards);
        Assert.Equal(0, hand.CardCount);
    }

    [Theory]
    [InlineData(Rank.Two, 2)]
    [InlineData(Rank.Three, 3)]
    [InlineData(Rank.Four, 4)]
    [InlineData(Rank.Five, 5)]
    [InlineData(Rank.Six, 6)]
    [InlineData(Rank.Seven, 7)]
    [InlineData(Rank.Eight, 8)]
    [InlineData(Rank.Nine, 9)]
    [InlineData(Rank.Ten, 10)]
    [InlineData(Rank.Jack, 10)]
    [InlineData(Rank.Queen, 10)]
    [InlineData(Rank.King, 10)]
    public void GetValue_WithSingleNumberOrFaceCard_ReturnsCorrectValue(Rank rank, int expectedValue)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, rank));

        // Act
        var actualValue = hand.GetValue();

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void GetValue_WithSingleAce_Returns11()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(11, value);
    }

    [Fact]
    public void GetValue_WithAceAndTen_Returns21()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ten));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(21, value);
    }

    [Fact]
    public void GetValue_WithAceAndFaceCard_Returns21()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(21, value);
    }

    [Fact]
    public void GetValue_WithTwoAces_Returns12()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ace));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(12, value); // One Ace as 11, one as 1
    }

    [Fact]
    public void GetValue_WithThreeAces_Returns13()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ace));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Ace));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(13, value); // One Ace as 11, two as 1
    }

    [Fact]
    public void GetValue_WithFourAces_Returns14()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ace));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Ace));
        hand.AddCard(new Card(Suit.Clubs, Rank.Ace));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(14, value); // One Ace as 11, three as 1
    }

    [Fact]
    public void GetValue_WithAceAndCardsOver21_ConvertsAceToOne()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Six));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Seven));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(14, value); // Ace as 1, 6 + 7 = 14
    }

    [Fact]
    public void GetValue_WithMultipleAcesAndBustScenario_ConvertsAcesAsNeeded()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Ace));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Nine));

        // Act
        var value = hand.GetValue();

        // Assert
        Assert.Equal(21, value); // One Ace as 11, one as 1, plus 9
    }

    [Theory]
    [InlineData(new[] { Rank.King, Rank.Queen }, 20, false)]
    [InlineData(new[] { Rank.King, Rank.Queen, Rank.Two }, 22, true)]
    [InlineData(new[] { Rank.Ten, Rank.Nine, Rank.Three }, 22, true)]
    [InlineData(new[] { Rank.Ace, Rank.King }, 21, false)]
    [InlineData(new[] { Rank.Ace, Rank.Ace, Rank.Ace, Rank.Ace, Rank.Ace, Rank.Ace, Rank.Six }, 12, false)]
    public void IsBusted_WithVariousHands_ReturnsCorrectResult(Rank[] ranks, int expectedValue, bool expectedBusted)
    {
        // Arrange
        var hand = new Hand();
        foreach (var rank in ranks)
        {
            hand.AddCard(new Card(Suit.Hearts, rank));
        }

        // Act
        var isBusted = hand.IsBusted();
        var value = hand.GetValue();

        // Assert
        Assert.Equal(expectedValue, value);
        Assert.Equal(expectedBusted, isBusted);
    }

    [Theory]
    [InlineData(new[] { Rank.Ace, Rank.King }, true)]
    [InlineData(new[] { Rank.Ace, Rank.Queen }, true)]
    [InlineData(new[] { Rank.Ace, Rank.Jack }, true)]
    [InlineData(new[] { Rank.Ace, Rank.Ten }, true)]
    [InlineData(new[] { Rank.King, Rank.Queen }, false)] // 20, not blackjack
    [InlineData(new[] { Rank.Seven, Rank.Seven, Rank.Seven }, false)] // 21 with 3 cards
    [InlineData(new[] { Rank.Ace, Rank.Five, Rank.Five }, false)] // 21 with 3 cards
    public void IsBlackjack_WithVariousHands_ReturnsCorrectResult(Rank[] ranks, bool expectedBlackjack)
    {
        // Arrange
        var hand = new Hand();
        foreach (var rank in ranks)
        {
            hand.AddCard(new Card(Suit.Hearts, rank));
        }

        // Act
        var isBlackjack = hand.IsBlackjack();

        // Assert
        Assert.Equal(expectedBlackjack, isBlackjack);
    }

    [Theory]
    [InlineData(new[] { Rank.Ace, Rank.Six }, true)] // Ace as 11, total 17
    [InlineData(new[] { Rank.Ace, Rank.Ace }, true)] // One Ace as 11, one as 1, total 12 - this IS soft
    [InlineData(new[] { Rank.Ace, Rank.King }, false)] // Blackjack - typically not considered soft since you can't hit
    [InlineData(new[] { Rank.Ace, Rank.Five, Rank.Five }, true)] // Ace as 11, total 21
    [InlineData(new[] { Rank.Ace, Rank.Six, Rank.Five }, false)] // Ace must be 1, total 12
    [InlineData(new[] { Rank.King, Rank.Queen }, false)] // No Ace
    public void IsSoft_WithVariousHands_ReturnsCorrectResult(Rank[] ranks, bool expectedSoft)
    {
        // Arrange
        var hand = new Hand();
        foreach (var rank in ranks)
        {
            hand.AddCard(new Card(Suit.Hearts, rank));
        }

        // Act
        var isSoft = hand.IsSoft();

        // Assert
        Assert.Equal(expectedSoft, isSoft);
    }

    [Fact]
    public void ToString_WithEmptyHand_ReturnsEmptyHandMessage()
    {
        // Arrange
        var hand = new Hand();

        // Act
        var result = hand.ToString();

        // Assert
        Assert.Equal("Empty hand", result);
    }

    [Fact]
    public void ToString_WithCards_ReturnsFormattedString()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var result = hand.ToString();

        // Assert
        Assert.Equal("A of Hearts, K of Spades", result);
    }

    [Fact]
    public void Cards_ReturnsReadOnlyCollection()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));

        // Act
        var cards = hand.Cards;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Card>>(cards);
        Assert.Single(cards);
    }

    // Edge case tests
    [Fact]
    public void GetValue_WithComplexAceScenario_HandlesCorrectly()
    {
        // Test case: A, A, A, A, A, A, 5 should equal 21 (one Ace as 11, five as 1, plus 5)
        var hand = new Hand();
        for (int i = 0; i < 6; i++)
        {
            hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        }
        hand.AddCard(new Card(Suit.Spades, Rank.Five));

        var value = hand.GetValue();

        Assert.Equal(21, value);
        Assert.False(hand.IsBusted());
        Assert.False(hand.IsBlackjack()); // More than 2 cards
    }

    [Fact]
    public void GetValue_WithAllFaceCards_ReturnsCorrectValue()
    {
        // Test case: J, Q, K should equal 30
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Jack));
        hand.AddCard(new Card(Suit.Spades, Rank.Queen));
        hand.AddCard(new Card(Suit.Diamonds, Rank.King));

        var value = hand.GetValue();

        Assert.Equal(30, value);
        Assert.True(hand.IsBusted());
    }
}