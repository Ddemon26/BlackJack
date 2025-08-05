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
        Assert.Equal("Player John: A of Hearts, K of Spades (Value: 21)", result);
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
        Assert.Equal("Player John: Empty hand (Value: 0)", result);
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
        // When hand is empty, it returns the normal ToString() which includes value
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
        // When hideFirstCard is false, it returns the normal ToString() which includes value
        Assert.Equal("Player John: A of Hearts, K of Spades (Value: 21)", result);
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
}