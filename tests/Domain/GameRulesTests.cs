using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain;

/// <summary>
/// Unit tests for the GameRules class.
/// </summary>
public class GameRulesTests
{
    private readonly GameRules _gameRules;

    public GameRulesTests()
    {
        _gameRules = new GameRules();
    }

    #region GetCardValue Tests

    [Theory]
    [InlineData(Rank.Ace, 10, 11)] // Ace as 11 when it doesn't bust
    [InlineData(Rank.Ace, 15, 1)]  // Ace as 1 when 11 would bust
    [InlineData(Rank.Ace, 21, 1)]  // Ace as 1 when 11 would bust exactly
    [InlineData(Rank.Two, 10, 2)]
    [InlineData(Rank.Three, 15, 3)]
    [InlineData(Rank.Four, 0, 4)]
    [InlineData(Rank.Five, 20, 5)]
    [InlineData(Rank.Six, 10, 6)]
    [InlineData(Rank.Seven, 5, 7)]
    [InlineData(Rank.Eight, 13, 8)]
    [InlineData(Rank.Nine, 2, 9)]
    [InlineData(Rank.Ten, 11, 10)]
    [InlineData(Rank.Jack, 5, 10)]
    [InlineData(Rank.Queen, 15, 10)]
    [InlineData(Rank.King, 0, 10)]
    public void GetCardValue_WithVariousCardsAndHandValues_ReturnsCorrectValue(
        Rank rank, int currentHandValue, int expectedValue)
    {
        // Arrange
        var card = new Card(Suit.Hearts, rank);

        // Act
        var result = _gameRules.GetCardValue(card, currentHandValue);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    #endregion

    #region ShouldDealerHit Tests

    [Theory]
    [InlineData(16, true)]  // Dealer hits on 16
    [InlineData(15, true)]  // Dealer hits on 15
    [InlineData(10, true)]  // Dealer hits on 10
    [InlineData(0, true)]   // Dealer hits on 0
    [InlineData(17, false)] // Dealer stands on 17
    [InlineData(18, false)] // Dealer stands on 18
    [InlineData(21, false)] // Dealer stands on 21
    [InlineData(22, false)] // Dealer stands even when busted
    public void ShouldDealerHit_WithVariousValues_ReturnsCorrectDecision(
        int dealerValue, bool expectedShouldHit)
    {
        // Act
        var result = _gameRules.ShouldDealerHit(dealerValue);

        // Assert
        Assert.Equal(expectedShouldHit, result);
    }

    #endregion

    #region DetermineResult Tests

    [Fact]
    public void DetermineResult_PlayerBusted_ReturnsLose()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25
        var dealerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Seven)); // 17

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_DealerBustedPlayerNotBusted_ReturnsWin()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19
        var dealerHand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Win, result);
    }

    [Fact]
    public void DetermineResult_DealerBustedPlayerHasBlackjack_ReturnsBlackjack()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)); // Blackjack
        var dealerHand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Blackjack, result);
    }

    [Fact]
    public void DetermineResult_BothHaveBlackjack_ReturnsPush()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)); // Blackjack
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Ace), new Card(Suit.Clubs, Rank.Queen)); // Blackjack

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Push, result);
    }

    [Fact]
    public void DetermineResult_OnlyPlayerHasBlackjack_ReturnsBlackjack()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)); // Blackjack
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Ten), new Card(Suit.Clubs, Rank.Nine)); // 19

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Blackjack, result);
    }

    [Fact]
    public void DetermineResult_OnlyDealerHasBlackjack_ReturnsLose()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Ace), new Card(Suit.Clubs, Rank.Queen)); // Blackjack

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_PlayerHigherValue_ReturnsWin()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Ten), new Card(Suit.Clubs, Rank.Seven)); // 17

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Win, result);
    }

    [Fact]
    public void DetermineResult_DealerHigherValue_ReturnsLose()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Seven)); // 17
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Ten), new Card(Suit.Clubs, Rank.Nine)); // 19

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_SameValue_ReturnsPush()
    {
        // Arrange
        var playerHand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Eight)); // 18
        var dealerHand = CreateHand(new Card(Suit.Diamonds, Rank.Nine), new Card(Suit.Clubs, Rank.Nine)); // 18

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Push, result);
    }

    #endregion

    #region IsValidPlayerAction Tests

    [Fact]
    public void IsValidPlayerAction_HitOnValidHand_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Hit, hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPlayerAction_HitOnBustedHand_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Hit, hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPlayerAction_StandOnValidHand_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Stand, hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPlayerAction_StandOnBustedHand_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Stand, hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPlayerAction_DoubleDownOnTwoCards_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15, 2 cards

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPlayerAction_DoubleDownOnThreeCards_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Five), new Card(Suit.Spades, Rank.Five), new Card(Suit.Diamonds, Rank.Five)); // 15, 3 cards

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPlayerAction_SplitOnPair_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight)); // Pair of 8s

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Split, hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidPlayerAction_SplitOnNonPair_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Nine)); // Not a pair

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidPlayerAction_SplitOnThreeCards_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight), new Card(Suit.Diamonds, Rank.Five)); // 3 cards

        // Act
        var result = _gameRules.IsValidPlayerAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNaturalBlackjack Tests

    [Fact]
    public void IsNaturalBlackjack_WithBlackjack_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)); // Blackjack

        // Act
        var result = _gameRules.IsNaturalBlackjack(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsNaturalBlackjack_With21ButThreeCards_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Seven), new Card(Suit.Spades, Rank.Seven), new Card(Suit.Diamonds, Rank.Seven)); // 21 with 3 cards

        // Act
        var result = _gameRules.IsNaturalBlackjack(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsNaturalBlackjack_WithNon21_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19

        // Act
        var result = _gameRules.IsNaturalBlackjack(hand);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsBusted Tests

    [Fact]
    public void IsBusted_WithBustedHand_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _gameRules.IsBusted(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBusted_WithValidHand_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19

        // Act
        var result = _gameRules.IsBusted(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBusted_WithExactly21_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King)); // 21

        // Act
        var result = _gameRules.IsBusted(hand);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a hand with the specified cards for testing.
    /// </summary>
    /// <param name="cards">The cards to add to the hand.</param>
    /// <returns>A hand containing the specified cards.</returns>
    private static Hand CreateHand(params Card[] cards)
    {
        var hand = new Hand();
        foreach (var card in cards)
        {
            hand.AddCard(card);
        }
        return hand;
    }

    #endregion
}