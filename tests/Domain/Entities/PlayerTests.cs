using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

public class PlayerTests
{
    [Fact]
    public void Constructor_WithValidName_InitializesCorrectly()
    {
        // Arrange & Act
        var player = new Player("John");

        // Assert
        Assert.Equal("John", player.Name);
        Assert.Equal(PlayerType.Human, player.Type);
        Assert.NotNull(player.Hand);
        Assert.Empty(player.Hand.Cards);
        Assert.True(player.IsHuman);
        Assert.False(player.IsDealer);
    }

    [Fact]
    public void Constructor_WithValidNameAndDealerType_InitializesCorrectly()
    {
        // Arrange & Act
        var dealer = new Player("Dealer", PlayerType.Dealer);

        // Assert
        Assert.Equal("Dealer", dealer.Name);
        Assert.Equal(PlayerType.Dealer, dealer.Type);
        Assert.NotNull(dealer.Hand);
        Assert.Empty(dealer.Hand.Cards);
        Assert.False(dealer.IsHuman);
        Assert.True(dealer.IsDealer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new Player(invalidName!));
    }

    [Fact]
    public void Constructor_WithNameWithWhitespace_TrimsName()
    {
        // Arrange & Act
        var player = new Player("  John  ");

        // Assert
        Assert.Equal("John", player.Name);
    }

    [Fact]
    public void AddCard_AddsCardToHand()
    {
        // Arrange
        var player = new Player("John");
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Act
        player.AddCard(card);

        // Assert
        Assert.Single(player.Hand.Cards);
        Assert.Equal(card, player.Hand.Cards[0]);
        Assert.Equal(1, player.GetCardCount());
    }

    [Fact]
    public void AddCard_MultipleCards_AddsAllCards()
    {
        // Arrange
        var player = new Player("John");
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Spades, Rank.King);

        // Act
        player.AddCard(card1);
        player.AddCard(card2);

        // Assert
        Assert.Equal(2, player.Hand.Cards.Count);
        Assert.Equal(card1, player.Hand.Cards[0]);
        Assert.Equal(card2, player.Hand.Cards[1]);
        Assert.Equal(2, player.GetCardCount());
    }

    [Fact]
    public void ClearHand_RemovesAllCards()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        player.ClearHand();

        // Assert
        Assert.Empty(player.Hand.Cards);
        Assert.Equal(0, player.GetCardCount());
    }

    [Fact]
    public void GetHandValue_WithEmptyHand_ReturnsZero()
    {
        // Arrange
        var player = new Player("John");

        // Act
        var value = player.GetHandValue();

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void GetHandValue_WithCards_ReturnsCorrectValue()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var value = player.GetHandValue();

        // Assert
        Assert.Equal(21, value); // Blackjack
    }

    [Fact]
    public void IsBusted_WithHandUnder21_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ten));
        player.AddCard(new Card(Suit.Spades, Rank.Nine));

        // Act
        var isBusted = player.IsBusted();

        // Assert
        Assert.False(isBusted);
        Assert.Equal(19, player.GetHandValue());
    }

    [Fact]
    public void IsBusted_WithHandOver21_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ten));
        player.AddCard(new Card(Suit.Spades, Rank.Nine));
        player.AddCard(new Card(Suit.Diamonds, Rank.Five));

        // Act
        var isBusted = player.IsBusted();

        // Assert
        Assert.True(isBusted);
        Assert.Equal(24, player.GetHandValue());
    }

    [Fact]
    public void HasBlackjack_WithBlackjackHand_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var hasBlackjack = player.HasBlackjack();

        // Assert
        Assert.True(hasBlackjack);
        Assert.Equal(21, player.GetHandValue());
    }

    [Fact]
    public void HasBlackjack_With21ButThreeCards_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Seven));
        player.AddCard(new Card(Suit.Spades, Rank.Seven));
        player.AddCard(new Card(Suit.Diamonds, Rank.Seven));

        // Act
        var hasBlackjack = player.HasBlackjack();

        // Assert
        Assert.False(hasBlackjack);
        Assert.Equal(21, player.GetHandValue());
    }

    [Fact]
    public void HasSoftHand_WithSoftHand_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.Six));

        // Act
        var hasSoftHand = player.HasSoftHand();

        // Assert
        Assert.True(hasSoftHand);
        Assert.Equal(17, player.GetHandValue());
    }

    [Fact]
    public void HasSoftHand_WithHardHand_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ten));
        player.AddCard(new Card(Suit.Spades, Rank.Seven));

        // Act
        var hasSoftHand = player.HasSoftHand();

        // Assert
        Assert.False(hasSoftHand);
        Assert.Equal(17, player.GetHandValue());
    }

    [Fact]
    public void HasSoftHand_WithBlackjack_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var hasSoftHand = player.HasSoftHand();

        // Assert
        Assert.False(hasSoftHand); // Blackjack is not considered soft
    }

    [Fact]
    public void GetCardCount_WithEmptyHand_ReturnsZero()
    {
        // Arrange
        var player = new Player("John");

        // Act
        var count = player.GetCardCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetCardCount_WithCards_ReturnsCorrectCount()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));
        player.AddCard(new Card(Suit.Diamonds, Rank.Five));

        // Act
        var count = player.GetCardCount();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void ToString_WithHumanPlayer_ReturnsCorrectFormat()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var result = player.ToString();

        // Assert
        Assert.Equal("Player John: A of Hearts, K of Spades (Value: 21) (Bankroll: 0.00 USD)", result);
    }

    [Fact]
    public void ToString_WithDealer_ReturnsCorrectFormat()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer);
        dealer.AddCard(new Card(Suit.Hearts, Rank.Ten));
        dealer.AddCard(new Card(Suit.Spades, Rank.Seven));

        // Act
        var result = dealer.ToString();

        // Assert
        Assert.Equal("Dealer Dealer: 10 of Hearts, 7 of Spades (Value: 17)", result);
    }

    [Fact]
    public void ToString_WithEmptyHand_ReturnsCorrectFormat()
    {
        // Arrange
        var player = new Player("John");

        // Act
        var result = player.ToString();

        // Assert
        Assert.Equal("Player John: Empty hand (Value: 0) (Bankroll: 0.00 USD)", result);
    }

    [Fact]
    public void ToStringWithHiddenCard_HideFirstCard_HidesFirstCard()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer);
        dealer.AddCard(new Card(Suit.Hearts, Rank.Ten));
        dealer.AddCard(new Card(Suit.Spades, Rank.Seven));

        // Act
        var result = dealer.ToStringWithHiddenCard(true);

        // Assert
        Assert.Equal("Dealer Dealer: [Hidden], 7 of Spades", result);
    }

    [Fact]
    public void ToStringWithHiddenCard_HideLastCard_HidesLastCard()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer);
        dealer.AddCard(new Card(Suit.Hearts, Rank.Ten));
        dealer.AddCard(new Card(Suit.Spades, Rank.Seven));

        // Act
        var result = dealer.ToStringWithHiddenCard(false);

        // Assert
        // When hideFirstCard is false, it returns the normal ToString() which includes value
        Assert.Equal("Dealer Dealer: 10 of Hearts, 7 of Spades (Value: 17)", result);
    }

    [Fact]
    public void ToStringWithHiddenCard_WithSingleCard_ShowsHiddenOnly()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer);
        dealer.AddCard(new Card(Suit.Hearts, Rank.Ten));

        // Act
        var result = dealer.ToStringWithHiddenCard(true);

        // Assert
        Assert.Equal("Dealer Dealer: [Hidden]", result);
    }

    [Fact]
    public void ToStringWithHiddenCard_WithEmptyHand_ReturnsEmptyHandMessage()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer);

        // Act
        var result = dealer.ToStringWithHiddenCard(true);

        // Assert
        // When hand is empty, it returns the normal ToString() which includes value (no bankroll for dealer)
        Assert.Equal("Dealer Dealer: Empty hand (Value: 0)", result);
    }

    [Fact]
    public void ToStringWithHiddenCard_WithFalseParameter_ShowsNormalString()
    {
        // Arrange
        var player = new Player("John");
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var result = player.ToStringWithHiddenCard(false);

        // Assert
        // When hideFirstCard is false, it returns the normal ToString() which includes value and bankroll
        Assert.Equal("Player John: A of Hearts, K of Spades (Value: 21) (Bankroll: 0.00 USD)", result);
    }

    [Fact]
    public void Equals_WithSameNameAndType_ReturnsTrue()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("John", PlayerType.Human);

        // Act & Assert
        Assert.True(player1.Equals(player2));
        Assert.True(player2.Equals(player1));
    }

    [Fact]
    public void Equals_WithSameNameDifferentCase_ReturnsTrue()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("JOHN", PlayerType.Human);

        // Act & Assert
        Assert.True(player1.Equals(player2));
    }

    [Fact]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("Jane", PlayerType.Human);

        // Act & Assert
        Assert.False(player1.Equals(player2));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("John", PlayerType.Dealer);

        // Act & Assert
        Assert.False(player1.Equals(player2));
    }

    [Fact]
    public void Equals_WithNullObject_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");

        // Act & Assert
        Assert.False(player.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentObjectType_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John");
        var otherObject = "Not a player";

        // Act & Assert
        Assert.False(player.Equals(otherObject));
    }

    [Fact]
    public void GetHashCode_WithSameNameAndType_ReturnsSameHashCode()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("john", PlayerType.Human); // Different case

        // Act
        var hash1 = player1.GetHashCode();
        var hash2 = player2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentPlayers_ReturnsDifferentHashCodes()
    {
        // Arrange
        var player1 = new Player("John", PlayerType.Human);
        var player2 = new Player("Jane", PlayerType.Human);

        // Act
        var hash1 = player1.GetHashCode();
        var hash2 = player2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData(PlayerType.Human, true, false)]
    [InlineData(PlayerType.Dealer, false, true)]
    public void PlayerType_Properties_ReturnCorrectValues(PlayerType type, bool expectedIsHuman, bool expectedIsDealer)
    {
        // Arrange
        var player = new Player("Test", type);

        // Act & Assert
        Assert.Equal(expectedIsHuman, player.IsHuman);
        Assert.Equal(expectedIsDealer, player.IsDealer);
        Assert.Equal(type, player.Type);
    }

    // Bankroll Management Tests

    [Fact]
    public void Constructor_WithInitialBankroll_SetsBankrollCorrectly()
    {
        // Arrange
        var initialBankroll = Money.FromUsd(100m);

        // Act
        var player = new Player("John", PlayerType.Human, initialBankroll);

        // Assert
        Assert.Equal(initialBankroll, player.Bankroll);
        Assert.False(player.HasActiveBet);
        Assert.Null(player.CurrentBet);
    }

    [Fact]
    public void Constructor_WithoutInitialBankroll_SetsZeroBankroll()
    {
        // Arrange & Act
        var player = new Player("John");

        // Assert
        Assert.Equal(Money.Zero, player.Bankroll);
        Assert.False(player.HasActiveBet);
        Assert.Null(player.CurrentBet);
    }

    [Fact]
    public void Constructor_WithNegativeBankroll_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var negativeBankroll = Money.FromUsd(-50m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Player("John", PlayerType.Human, negativeBankroll));
    }

    [Fact]
    public void HasSufficientFunds_WithSufficientFunds_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(50m);

        // Act
        var result = player.HasSufficientFunds(betAmount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSufficientFunds_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(30m));
        var betAmount = Money.FromUsd(50m);

        // Act
        var result = player.HasSufficientFunds(betAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasSufficientFunds_WithExactAmount_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(50m));
        var betAmount = Money.FromUsd(50m);

        // Act
        var result = player.HasSufficientFunds(betAmount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void PlaceBet_WithValidAmount_PlacesBetAndDeductsFunds()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);

        // Act
        player.PlaceBet(betAmount);

        // Assert
        Assert.True(player.HasActiveBet);
        Assert.NotNull(player.CurrentBet);
        Assert.Equal(betAmount, player.CurrentBet.Amount);
        Assert.Equal(Money.FromUsd(75m), player.Bankroll);
        Assert.Equal("John", player.CurrentBet.PlayerName);
        Assert.Equal(BetType.Standard, player.CurrentBet.Type);
    }

    [Fact]
    public void PlaceBet_WithCustomBetType_PlacesBetWithCorrectType()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);

        // Act
        player.PlaceBet(betAmount, BetType.DoubleDown);

        // Assert
        Assert.True(player.HasActiveBet);
        Assert.Equal(BetType.DoubleDown, player.CurrentBet!.Type);
    }

    [Fact]
    public void PlaceBet_WithInsufficientFunds_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(20m));
        var betAmount = Money.FromUsd(50m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.PlaceBet(betAmount));
        Assert.Contains("Insufficient funds", exception.Message);
        Assert.False(player.HasActiveBet);
        Assert.Equal(Money.FromUsd(20m), player.Bankroll);
    }

    [Fact]
    public void PlaceBet_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaceBet(Money.Zero));
        Assert.False(player.HasActiveBet);
    }

    [Fact]
    public void PlaceBet_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var negativeAmount = Money.FromUsd(-10m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaceBet(negativeAmount));
        Assert.False(player.HasActiveBet);
    }

    [Fact]
    public void PlaceBet_WithExistingActiveBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.PlaceBet(Money.FromUsd(25m)));
        Assert.Contains("already has an active bet", exception.Message);
    }

    [Fact]
    public void UpdateBankroll_WithPositiveAmount_IncreasesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var addition = Money.FromUsd(50m);

        // Act
        player.UpdateBankroll(addition);

        // Assert
        Assert.Equal(Money.FromUsd(150m), player.Bankroll);
    }

    [Fact]
    public void UpdateBankroll_WithNegativeAmount_DecreasesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var deduction = Money.FromUsd(-30m);

        // Act
        player.UpdateBankroll(deduction);

        // Assert
        Assert.Equal(Money.FromUsd(70m), player.Bankroll);
    }

    [Fact]
    public void UpdateBankroll_ResultingInNegativeBankroll_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(50m));
        var largeDeduction = Money.FromUsd(-100m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.UpdateBankroll(largeDeduction));
        Assert.Contains("negative bankroll", exception.Message);
        Assert.Equal(Money.FromUsd(50m), player.Bankroll);
    }

    [Fact]
    public void AddFunds_WithPositiveAmount_IncreasesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var addition = Money.FromUsd(75m);

        // Act
        player.AddFunds(addition);

        // Assert
        Assert.Equal(Money.FromUsd(175m), player.Bankroll);
    }

    [Fact]
    public void AddFunds_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.AddFunds(Money.Zero));
        Assert.Equal(Money.FromUsd(100m), player.Bankroll);
    }

    [Fact]
    public void AddFunds_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var negativeAmount = Money.FromUsd(-25m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.AddFunds(negativeAmount));
        Assert.Equal(Money.FromUsd(100m), player.Bankroll);
    }

    [Fact]
    public void DeductFunds_WithValidAmount_DecreasesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var deduction = Money.FromUsd(30m);

        // Act
        player.DeductFunds(deduction);

        // Assert
        Assert.Equal(Money.FromUsd(70m), player.Bankroll);
    }

    [Fact]
    public void DeductFunds_WithInsufficientFunds_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(50m));
        var largeDeduction = Money.FromUsd(100m);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.DeductFunds(largeDeduction));
        Assert.Contains("Insufficient funds", exception.Message);
        Assert.Equal(Money.FromUsd(50m), player.Bankroll);
    }

    [Fact]
    public void DeductFunds_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.DeductFunds(Money.Zero));
        Assert.Equal(Money.FromUsd(100m), player.Bankroll);
    }

    [Fact]
    public void SettleBet_WithWin_ReturnsCorrectAmountAndUpdatesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        var totalReturn = player.SettleBet(GameResult.Win);

        // Assert
        Assert.Equal(Money.FromUsd(50m), totalReturn); // Original bet + payout
        Assert.Equal(Money.FromUsd(125m), player.Bankroll); // 75 (after bet) + 50 (return)
        Assert.False(player.HasActiveBet);
        Assert.Null(player.CurrentBet);
    }

    [Fact]
    public void SettleBet_WithBlackjack_ReturnsCorrectAmountAndUpdatesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(20m);
        player.PlaceBet(betAmount);

        // Act
        var totalReturn = player.SettleBet(GameResult.Blackjack);

        // Assert
        Assert.Equal(Money.FromUsd(50m), totalReturn); // 20 + (20 * 1.5) = 50
        Assert.Equal(Money.FromUsd(130m), player.Bankroll); // 80 (after bet) + 50 (return)
        Assert.False(player.HasActiveBet);
    }

    [Fact]
    public void SettleBet_WithCustomBlackjackMultiplier_ReturnsCorrectAmount()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(20m);
        player.PlaceBet(betAmount);

        // Act
        var totalReturn = player.SettleBet(GameResult.Blackjack, 2.0m);

        // Assert
        Assert.Equal(Money.FromUsd(60m), totalReturn); // 20 + (20 * 2.0) = 60
        Assert.Equal(Money.FromUsd(140m), player.Bankroll); // 80 (after bet) + 60 (return)
    }

    [Fact]
    public void SettleBet_WithLoss_ReturnsZeroAndDoesNotUpdateBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        var totalReturn = player.SettleBet(GameResult.Lose);

        // Assert
        Assert.Equal(Money.Zero, totalReturn);
        Assert.Equal(Money.FromUsd(75m), player.Bankroll); // Remains at 75 (bet was already deducted)
        Assert.False(player.HasActiveBet);
    }

    [Fact]
    public void SettleBet_WithPush_ReturnsOriginalBetAndUpdatesBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        var totalReturn = player.SettleBet(GameResult.Push);

        // Assert
        Assert.Equal(Money.FromUsd(25m), totalReturn); // Original bet returned
        Assert.Equal(Money.FromUsd(100m), player.Bankroll); // Back to original amount
        Assert.False(player.HasActiveBet);
    }

    [Fact]
    public void SettleBet_WithoutActiveBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.SettleBet(GameResult.Win));
        Assert.Contains("no active bet", exception.Message);
    }

    [Fact]
    public void ClearBet_WithActiveBet_ClearsBetAndReturnsFunds()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        player.ClearBet();

        // Assert
        Assert.False(player.HasActiveBet);
        Assert.Null(player.CurrentBet);
        Assert.Equal(Money.FromUsd(100m), player.Bankroll); // Funds returned
    }

    [Fact]
    public void ClearBet_WithoutActiveBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => player.ClearBet());
        Assert.Contains("no active bet", exception.Message);
    }

    [Fact]
    public void SetBankroll_WithValidAmount_SetsBankroll()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var newAmount = Money.FromUsd(250m);

        // Act
        player.SetBankroll(newAmount);

        // Assert
        Assert.Equal(newAmount, player.Bankroll);
    }

    [Fact]
    public void SetBankroll_WithZeroAmount_SetsBankrollToZero()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Act
        player.SetBankroll(Money.Zero);

        // Assert
        Assert.Equal(Money.Zero, player.Bankroll);
    }

    [Fact]
    public void SetBankroll_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var negativeAmount = Money.FromUsd(-50m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.SetBankroll(negativeAmount));
        Assert.Equal(Money.FromUsd(100m), player.Bankroll);
    }

    [Fact]
    public void ResetForNewRound_ClearsHandAndBet()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.PlaceBet(Money.FromUsd(25m));

        // Act
        player.ResetForNewRound();

        // Assert
        Assert.Empty(player.Hand.Cards);
        Assert.False(player.HasActiveBet);
        Assert.Null(player.CurrentBet);
        Assert.Equal(Money.FromUsd(75m), player.Bankroll); // Bet amount remains deducted
    }

    [Fact]
    public void ToString_WithHumanPlayerAndBankroll_IncludesBankrollInformation()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(150m));
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        var result = player.ToString();

        // Assert
        Assert.Contains("Player John:", result);
        Assert.Contains("(Value: 21)", result);
        Assert.Contains("(Bankroll: 150.00 USD)", result);
    }

    [Fact]
    public void ToString_WithDealer_DoesNotIncludeBankrollInformation()
    {
        // Arrange
        var dealer = new Player("Dealer", PlayerType.Dealer, Money.FromUsd(1000m));
        dealer.AddCard(new Card(Suit.Hearts, Rank.Ten));
        dealer.AddCard(new Card(Suit.Spades, Rank.Seven));

        // Act
        var result = dealer.ToString();

        // Assert
        Assert.Contains("Dealer Dealer:", result);
        Assert.Contains("(Value: 17)", result);
        Assert.DoesNotContain("Bankroll", result);
    }

    // Statistics Tests

    [Fact]
    public void Constructor_InitializesStatisticsCorrectly()
    {
        // Arrange & Act
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));

        // Assert
        Assert.NotNull(player.Statistics);
        Assert.Equal("John", player.Statistics.PlayerName);
        Assert.Equal(0, player.Statistics.GamesPlayed);
        Assert.Equal(0, player.Statistics.GamesWon);
        Assert.Equal(0, player.Statistics.GamesLost);
        Assert.Equal(Money.Zero, player.Statistics.NetWinnings);
    }

    [Fact]
    public void SettleBet_WithWin_UpdatesStatistics()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        player.SettleBet(GameResult.Win);

        // Assert
        Assert.Equal(1, player.Statistics.GamesPlayed);
        Assert.Equal(1, player.Statistics.GamesWon);
        Assert.Equal(0, player.Statistics.GamesLost);
        Assert.Equal(0, player.Statistics.GamesPushed);
        Assert.Equal(0, player.Statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, player.Statistics.TotalWagered);
        Assert.Equal(betAmount, player.Statistics.NetWinnings);
    }

    [Fact]
    public void SettleBet_WithBlackjack_UpdatesStatistics()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(20m);
        player.PlaceBet(betAmount);

        // Act
        player.SettleBet(GameResult.Blackjack);

        // Assert
        Assert.Equal(1, player.Statistics.GamesPlayed);
        Assert.Equal(1, player.Statistics.GamesWon);
        Assert.Equal(0, player.Statistics.GamesLost);
        Assert.Equal(0, player.Statistics.GamesPushed);
        Assert.Equal(1, player.Statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, player.Statistics.TotalWagered);
        Assert.Equal(Money.FromUsd(30m), player.Statistics.NetWinnings); // 3:2 payout
    }

    [Fact]
    public void SettleBet_WithLoss_UpdatesStatistics()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        player.SettleBet(GameResult.Lose);

        // Assert
        Assert.Equal(1, player.Statistics.GamesPlayed);
        Assert.Equal(0, player.Statistics.GamesWon);
        Assert.Equal(1, player.Statistics.GamesLost);
        Assert.Equal(0, player.Statistics.GamesPushed);
        Assert.Equal(0, player.Statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, player.Statistics.TotalWagered);
        Assert.Equal(Money.FromUsd(-25m), player.Statistics.NetWinnings);
    }

    [Fact]
    public void SettleBet_WithPush_UpdatesStatistics()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        var betAmount = Money.FromUsd(25m);
        player.PlaceBet(betAmount);

        // Act
        player.SettleBet(GameResult.Push);

        // Assert
        Assert.Equal(1, player.Statistics.GamesPlayed);
        Assert.Equal(0, player.Statistics.GamesWon);
        Assert.Equal(0, player.Statistics.GamesLost);
        Assert.Equal(1, player.Statistics.GamesPushed);
        Assert.Equal(0, player.Statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, player.Statistics.TotalWagered);
        Assert.Equal(Money.Zero, player.Statistics.NetWinnings);
    }

    [Fact]
    public void ResetStatistics_ClearsAllStatistics()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Win);

        // Act
        player.ResetStatistics();

        // Assert
        Assert.Equal(0, player.Statistics.GamesPlayed);
        Assert.Equal(0, player.Statistics.GamesWon);
        Assert.Equal(Money.Zero, player.Statistics.NetWinnings);
        Assert.Equal(Money.Zero, player.Statistics.TotalWagered);
    }

    [Fact]
    public void GetWinPercentage_WithMultipleGames_ReturnsCorrectPercentage()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(200m));
        
        // Win 3 out of 5 games
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Win);
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Win);
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Win);
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Lose);
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Lose);

        // Act
        var winPercentage = player.GetWinPercentage();

        // Assert
        Assert.Equal(0.6, winPercentage, 2);
    }

    [Fact]
    public void GetNetWinnings_ReturnsCorrectAmount()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Win);

        // Act
        var netWinnings = player.GetNetWinnings();

        // Assert
        Assert.Equal(Money.FromUsd(25m), netWinnings);
    }

    [Fact]
    public void GetGamesPlayed_ReturnsCorrectCount()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Win);
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Lose);

        // Act
        var gamesPlayed = player.GetGamesPlayed();

        // Assert
        Assert.Equal(2, gamesPlayed);
    }

    [Fact]
    public void GetBlackjacksAchieved_ReturnsCorrectCount()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Blackjack);
        player.PlaceBet(Money.FromUsd(20m));
        player.SettleBet(GameResult.Win);

        // Act
        var blackjacks = player.GetBlackjacksAchieved();

        // Assert
        Assert.Equal(1, blackjacks);
    }

    [Fact]
    public void GetReturnOnInvestment_WithProfitablePlay_ReturnsPositiveROI()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Win); // Net +$25 on $25 wagered = 100% ROI

        // Act
        var roi = player.GetReturnOnInvestment();

        // Assert
        Assert.Equal(1.0, roi, 2);
    }

    [Fact]
    public void IsProfitable_WithPositiveNetWinnings_ReturnsTrue()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Win);

        // Act
        var isProfitable = player.IsProfitable();

        // Assert
        Assert.True(isProfitable);
    }

    [Fact]
    public void IsProfitable_WithNegativeNetWinnings_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Lose);

        // Act
        var isProfitable = player.IsProfitable();

        // Assert
        Assert.False(isProfitable);
    }

    [Fact]
    public void IsProfitable_WithZeroNetWinnings_ReturnsFalse()
    {
        // Arrange
        var player = new Player("John", PlayerType.Human, Money.FromUsd(100m));
        player.PlaceBet(Money.FromUsd(25m));
        player.SettleBet(GameResult.Push);

        // Act
        var isProfitable = player.IsProfitable();

        // Assert
        Assert.False(isProfitable);
    }
}