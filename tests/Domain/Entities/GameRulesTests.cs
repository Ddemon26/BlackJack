using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

public class GameRulesTests
{
    private readonly GameRules _gameRules;

    public GameRulesTests()
    {
        _gameRules = new GameRules();
    }

    [Theory]
    [InlineData(Rank.Two, 0, 2)]
    [InlineData(Rank.Three, 0, 3)]
    [InlineData(Rank.Four, 0, 4)]
    [InlineData(Rank.Five, 0, 5)]
    [InlineData(Rank.Six, 0, 6)]
    [InlineData(Rank.Seven, 0, 7)]
    [InlineData(Rank.Eight, 0, 8)]
    [InlineData(Rank.Nine, 0, 9)]
    [InlineData(Rank.Ten, 0, 10)]
    [InlineData(Rank.Jack, 0, 10)]
    [InlineData(Rank.Queen, 0, 10)]
    [InlineData(Rank.King, 0, 10)]
    public void GetCardValue_WithNumberAndFaceCards_ReturnsCorrectValue(Rank rank, int currentHandValue, int expectedValue)
    {
        // Arrange
        var card = new Card(Suit.Hearts, rank);

        // Act
        var value = _gameRules.GetCardValue(card, currentHandValue);

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(0, 11)]   // Ace as 11 when hand is empty
    [InlineData(5, 11)]   // Ace as 11 when total would be 16
    [InlineData(10, 11)]  // Ace as 11 when total would be 21
    [InlineData(11, 1)]   // Ace as 1 when total would be 22
    [InlineData(15, 1)]   // Ace as 1 when total would be 26
    [InlineData(20, 1)]   // Ace as 1 when total would be 31
    public void GetCardValue_WithAce_ReturnsCorrectValue(int currentHandValue, int expectedValue)
    {
        // Arrange
        var ace = new Card(Suit.Hearts, Rank.Ace);

        // Act
        var value = _gameRules.GetCardValue(ace, currentHandValue);

        // Assert
        Assert.Equal(expectedValue, value);
    }

    [Theory]
    [InlineData(16, true)]   // Dealer hits on 16
    [InlineData(15, true)]   // Dealer hits on 15
    [InlineData(10, true)]   // Dealer hits on 10
    [InlineData(0, true)]    // Dealer hits on 0
    [InlineData(17, false)]  // Dealer stands on 17
    [InlineData(18, false)]  // Dealer stands on 18
    [InlineData(21, false)]  // Dealer stands on 21
    [InlineData(22, false)]  // Dealer stands on bust (shouldn't happen in practice)
    public void ShouldDealerHit_WithVariousValues_ReturnsCorrectResult(int dealerValue, bool expectedShouldHit)
    {
        // Act
        var shouldHit = _gameRules.ShouldDealerHit(dealerValue);

        // Assert
        Assert.Equal(expectedShouldHit, shouldHit);
    }

    [Fact]
    public void DetermineResult_PlayerBusted_ReturnsLose()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Nine));
        playerHand.AddCard(new Card(Suit.Diamonds, Rank.Five)); // 24 - busted

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.Seven)); // 17

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_DealerBustedPlayerNotBusted_ReturnsWin()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Nine)); // 19

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.Seven));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Six)); // 23 - busted

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Win, result);
    }

    [Fact]
    public void DetermineResult_DealerBustedPlayerHasBlackjack_ReturnsBlackjack()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        playerHand.AddCard(new Card(Suit.Spades, Rank.King)); // Blackjack

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.Seven));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Six)); // 23 - busted

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Blackjack, result);
    }

    [Fact]
    public void DetermineResult_BothHaveBlackjack_ReturnsPush()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        playerHand.AddCard(new Card(Suit.Spades, Rank.King)); // Blackjack

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ace));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Queen)); // Blackjack

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Push, result);
    }

    [Fact]
    public void DetermineResult_OnlyPlayerHasBlackjack_ReturnsBlackjack()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        playerHand.AddCard(new Card(Suit.Spades, Rank.King)); // Blackjack

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Nine)); // 19

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Blackjack, result);
    }

    [Fact]
    public void DetermineResult_OnlyDealerHasBlackjack_ReturnsLose()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Nine)); // 19

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ace));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Queen)); // Blackjack

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_PlayerHigherValue_ReturnsWin()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Nine)); // 19

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Seven)); // 17

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Win, result);
    }

    [Fact]
    public void DetermineResult_DealerHigherValue_ReturnsLose()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Seven)); // 17

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Ten));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Nine)); // 19

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Lose, result);
    }

    [Fact]
    public void DetermineResult_SameValue_ReturnsPush()
    {
        // Arrange
        var playerHand = new Hand();
        playerHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        playerHand.AddCard(new Card(Suit.Spades, Rank.Eight)); // 18

        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Clubs, Rank.Nine));
        dealerHand.AddCard(new Card(Suit.Diamonds, Rank.Nine)); // 18

        // Act
        var result = _gameRules.DetermineResult(playerHand, dealerHand);

        // Assert
        Assert.Equal(GameResult.Push, result);
    }

    [Theory]
    [InlineData(PlayerAction.Hit, true)]         // Can hit if not busted
    [InlineData(PlayerAction.Stand, true)]
    [InlineData(PlayerAction.DoubleDown, false)] // Can't double down with 3 cards
    [InlineData(PlayerAction.Split, false)]      // Can't split with 3 cards
    public void IsValidPlayerAction_WithThreeCards_ReturnsCorrectResult(PlayerAction action, bool expectedValid)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Seven));
        hand.AddCard(new Card(Suit.Spades, Rank.Six));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Four)); // 17 with 3 cards

        // Act
        var isValid = _gameRules.IsValidPlayerAction(action, hand);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(PlayerAction.Hit, true)]
    [InlineData(PlayerAction.Stand, true)]
    [InlineData(PlayerAction.DoubleDown, true)]  // Can double down with 2 cards
    [InlineData(PlayerAction.Split, false)]      // Can't split different ranks
    public void IsValidPlayerAction_WithTwoCardsNonPair_ReturnsCorrectResult(PlayerAction action, bool expectedValid)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        hand.AddCard(new Card(Suit.Spades, Rank.Seven)); // 17 with 2 different cards

        // Act
        var isValid = _gameRules.IsValidPlayerAction(action, hand);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(PlayerAction.Hit, true)]
    [InlineData(PlayerAction.Stand, true)]
    [InlineData(PlayerAction.DoubleDown, true)]  // Can double down with 2 cards
    [InlineData(PlayerAction.Split, true)]       // Can split same ranks
    public void IsValidPlayerAction_WithPair_ReturnsCorrectResult(PlayerAction action, bool expectedValid)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        hand.AddCard(new Card(Suit.Spades, Rank.Eight)); // Pair of 8s

        // Act
        var isValid = _gameRules.IsValidPlayerAction(action, hand);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(PlayerAction.Hit, false)]
    [InlineData(PlayerAction.Stand, false)]
    [InlineData(PlayerAction.DoubleDown, false)]
    [InlineData(PlayerAction.Split, false)]
    public void IsValidPlayerAction_WithBustedHand_ReturnsAllFalse(PlayerAction action, bool expectedValid)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        hand.AddCard(new Card(Suit.Spades, Rank.Nine));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Five)); // 24 - busted

        // Act
        var isValid = _gameRules.IsValidPlayerAction(action, hand);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void IsNaturalBlackjack_WithBlackjack_ReturnsTrue()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        hand.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var isBlackjack = _gameRules.IsNaturalBlackjack(hand);

        // Assert
        Assert.True(isBlackjack);
    }

    [Fact]
    public void IsNaturalBlackjack_With21ButThreeCards_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Seven));
        hand.AddCard(new Card(Suit.Spades, Rank.Seven));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Seven));

        // Act
        var isBlackjack = _gameRules.IsNaturalBlackjack(hand);

        // Assert
        Assert.False(isBlackjack);
    }

    [Fact]
    public void IsBusted_WithBustedHand_ReturnsTrue()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        hand.AddCard(new Card(Suit.Spades, Rank.Nine));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Five)); // 24

        // Act
        var isBusted = _gameRules.IsBusted(hand);

        // Assert
        Assert.True(isBusted);
    }

    [Fact]
    public void IsBusted_WithValidHand_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        hand.AddCard(new Card(Suit.Spades, Rank.Nine)); // 19

        // Act
        var isBusted = _gameRules.IsBusted(hand);

        // Assert
        Assert.False(isBusted);
    }

    [Theory]
    [InlineData(Rank.Ace, Rank.Ace, true)]
    [InlineData(Rank.Eight, Rank.Eight, true)]
    [InlineData(Rank.King, Rank.King, true)]
    [InlineData(Rank.Ten, Rank.Jack, false)] // Different ranks but same value
    [InlineData(Rank.Queen, Rank.King, false)]
    [InlineData(Rank.Seven, Rank.Eight, false)]
    public void IsValidPlayerAction_Split_WithVariousPairs_ReturnsCorrectResult(Rank rank1, Rank rank2, bool expectedCanSplit)
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, rank1));
        hand.AddCard(new Card(Suit.Spades, rank2));

        // Act
        var canSplit = _gameRules.IsValidPlayerAction(PlayerAction.Split, hand);

        // Assert
        Assert.Equal(expectedCanSplit, canSplit);
    }

    [Fact]
    public void IsValidPlayerAction_Split_WithMoreThanTwoCards_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        hand.AddCard(new Card(Suit.Spades, Rank.Eight));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Three)); // 3 cards, even though first two are pair

        // Act
        var canSplit = _gameRules.IsValidPlayerAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(canSplit);
    }

    [Fact]
    public void IsValidPlayerAction_DoubleDown_WithMoreThanTwoCards_ReturnsFalse()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Five));
        hand.AddCard(new Card(Suit.Spades, Rank.Six));
        hand.AddCard(new Card(Suit.Diamonds, Rank.Two)); // 3 cards

        // Act
        var canDoubleDown = _gameRules.IsValidPlayerAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.False(canDoubleDown);
    }

    [Fact]
    public void IsValidPlayerAction_DoubleDown_WithExactlyTwoCards_ReturnsTrue()
    {
        // Arrange
        var hand = new Hand();
        hand.AddCard(new Card(Suit.Hearts, Rank.Five));
        hand.AddCard(new Card(Suit.Spades, Rank.Six)); // 2 cards, total 11

        // Act
        var canDoubleDown = _gameRules.IsValidPlayerAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.True(canDoubleDown);
    }
}