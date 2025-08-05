using GroupProject.Domain.Entities;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.Services;

/// <summary>
/// Unit tests for the SplitHandManager class.
/// </summary>
public class SplitHandManagerTests
{
    private readonly SplitHandManager _splitHandManager;

    public SplitHandManagerTests()
    {
        _splitHandManager = new SplitHandManager();
    }

    #region CanSplit Tests

    [Fact]
    public void CanSplit_WithMatchingPair_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Eight)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanSplit_WithAces_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Clubs, Rank.Ace)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanSplit_WithFaceCards_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.King),
            new Card(Suit.Spades, Rank.King)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanSplit_WithDifferentRanks_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Nine)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSplit_WithOneCard_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSplit_WithThreeCards_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Eight),
            new Card(Suit.Clubs, Rank.Five)
        );

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanSplit_WithEmptyHand_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();

        // Act
        var result = _splitHandManager.CanSplit(hand);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region SplitHand Tests

    [Fact]
    public void SplitHand_WithValidPair_ReturnsTwoSplitHands()
    {
        // Arrange
        var originalHand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Eight)
        );

        // Act
        var (firstHand, secondHand) = _splitHandManager.SplitHand(originalHand);

        // Assert
        Assert.NotNull(firstHand);
        Assert.NotNull(secondHand);
        Assert.Equal(1, firstHand.CardCount);
        Assert.Equal(1, secondHand.CardCount);
        Assert.True(firstHand.IsSplitHand);
        Assert.True(secondHand.IsSplitHand);
        Assert.Equal(Rank.Eight, firstHand.Cards[0].Rank);
        Assert.Equal(Rank.Eight, secondHand.Cards[0].Rank);
    }

    [Fact]
    public void SplitHand_WithAces_ReturnsTwoSplitAceHands()
    {
        // Arrange
        var originalHand = CreateHand(
            new Card(Suit.Hearts, Rank.Ace),
            new Card(Suit.Spades, Rank.Ace)
        );

        // Act
        var (firstHand, secondHand) = _splitHandManager.SplitHand(originalHand);

        // Assert
        Assert.NotNull(firstHand);
        Assert.NotNull(secondHand);
        Assert.Equal(1, firstHand.CardCount);
        Assert.Equal(1, secondHand.CardCount);
        Assert.True(firstHand.IsSplitHand);
        Assert.True(secondHand.IsSplitHand);
        Assert.Equal(Rank.Ace, firstHand.Cards[0].Rank);
        Assert.Equal(Rank.Ace, secondHand.Cards[0].Rank);
    }

    [Fact]
    public void SplitHand_WithInvalidHand_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidHand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Nine)
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _splitHandManager.SplitHand(invalidHand));
        Assert.Contains("Hand cannot be split", exception.Message);
    }

    #endregion

    #region IsSplitAcesHand Tests

    [Fact]
    public void IsSplitAcesHand_WithSplitAceHand_ReturnsTrue()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.MarkAsSplitHand();

        // Act
        var result = _splitHandManager.IsSplitAcesHand(hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSplitAcesHand_WithSplitNonAceHand_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        hand.MarkAsSplitHand();

        // Act
        var result = _splitHandManager.IsSplitAcesHand(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSplitAcesHand_WithNonSplitAceHand_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        // Not marked as split hand

        // Act
        var result = _splitHandManager.IsSplitAcesHand(hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSplitAcesHand_WithMultipleCards_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.Five));
        hand.MarkAsSplitHand();

        // Act
        var result = _splitHandManager.IsSplitAcesHand(hand);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region HasSufficientFundsForSplit Tests

    [Fact]
    public void HasSufficientFundsForSplit_WithSufficientFunds_ReturnsTrue()
    {
        // Arrange
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));

        // Act
        var result = _splitHandManager.HasSufficientFundsForSplit(player);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSufficientFundsForSplit_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(15m));
        player.PlaceBet(new Money(10m)); // Bankroll becomes 5, need 10 more for split

        // Act
        var result = _splitHandManager.HasSufficientFundsForSplit(player);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasSufficientFundsForSplit_WithNoBet_ReturnsFalse()
    {
        // Arrange
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(100m));
        // No bet placed

        // Act
        var result = _splitHandManager.HasSufficientFundsForSplit(player);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CreateSplitBet Tests

    [Fact]
    public void CreateSplitBet_WithValidBet_ReturnsMatchingSplitBet()
    {
        // Arrange
        var originalBet = new Bet(new Money(10m), "TestPlayer", BetType.Standard);

        // Act
        var splitBet = _splitHandManager.CreateSplitBet(originalBet);

        // Assert
        Assert.NotNull(splitBet);
        Assert.Equal(originalBet.Amount, splitBet.Amount);
        Assert.Equal(originalBet.PlayerName, splitBet.PlayerName);
        Assert.Equal(BetType.Split, splitBet.Type);
        Assert.True(splitBet.IsActive);
    }

    [Fact]
    public void CreateSplitBet_WithNullBet_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _splitHandManager.CreateSplitBet(null!));
    }

    [Fact]
    public void CreateSplitBet_WithSettledBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var settledBet = new Bet(new Money(10m), "TestPlayer", BetType.Standard);
        settledBet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _splitHandManager.CreateSplitBet(settledBet));
        Assert.Contains("Cannot create split bet from a settled bet", exception.Message);
    }

    [Fact]
    public void CreateSplitBet_WithNonStandardBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var doubleDownBet = new Bet(new Money(20m), "TestPlayer", BetType.DoubleDown);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _splitHandManager.CreateSplitBet(doubleDownBet));
        Assert.Contains("Can only split standard bets", exception.Message);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void GetMaximumSplits_ReturnsThree()
    {
        // Act
        var maxSplits = _splitHandManager.GetMaximumSplits();

        // Assert
        Assert.Equal(3, maxSplits);
    }

    [Fact]
    public void CountSplitHands_WithBasicPlayer_ReturnsZero()
    {
        // Arrange
        var player = new Player("TestPlayer");

        // Act
        var count = _splitHandManager.CountSplitHands(player);

        // Assert
        Assert.Equal(0, count);
    }

    #endregion

    #region ValidateSplitOperation Tests

    [Fact]
    public void ValidateSplitOperation_WithValidConditions_ReturnsTrue()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Eight)
        );
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));

        // Act
        var result = _splitHandManager.ValidateSplitOperation(hand, player);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSplitOperation_WithInvalidHand_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Nine)
        );
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(100m));
        player.PlaceBet(new Money(10m));

        // Act
        var result = _splitHandManager.ValidateSplitOperation(hand, player);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSplitOperation_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var hand = CreateHand(
            new Card(Suit.Hearts, Rank.Eight),
            new Card(Suit.Spades, Rank.Eight)
        );
        var player = new Player("TestPlayer");
        player.SetBankroll(new Money(5m));
        player.PlaceBet(new Money(10m)); // Insufficient funds for split

        // Act
        var result = _splitHandManager.ValidateSplitOperation(hand, player);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Helper Methods

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