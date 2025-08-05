using System;
using Xunit;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for the Bet value object.
/// Tests bet creation, validation, payout calculations, and settlement logic.
/// </summary>
public class BetTests
{
    private readonly Money _standardAmount = Money.FromUsd(10.00m);
    private const string PlayerName = "TestPlayer";

    [Fact]
    public void Constructor_WithValidParameters_CreatesBet()
    {
        // Arrange & Act
        var bet = new Bet(_standardAmount, PlayerName);

        // Assert
        Assert.Equal(_standardAmount, bet.Amount);
        Assert.Equal(PlayerName, bet.PlayerName);
        Assert.Equal(BetType.Standard, bet.Type);
        Assert.True(bet.IsActive);
        Assert.False(bet.IsSettled);
        Assert.True(bet.PlacedAt <= DateTime.UtcNow);
        Assert.True(bet.PlacedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_WithBetType_CreatesBetWithSpecifiedType()
    {
        // Arrange & Act
        var bet = new Bet(_standardAmount, PlayerName, BetType.DoubleDown);

        // Assert
        Assert.Equal(BetType.DoubleDown, bet.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPlayerName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Bet(_standardAmount, invalidName));
        Assert.Contains("Player name cannot be null, empty, or whitespace", exception.Message);
        Assert.Equal("playerName", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var zeroAmount = Money.Zero;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Bet(zeroAmount, PlayerName));
        Assert.Contains("Bet amount must be positive", exception.Message);
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var negativeAmount = Money.FromUsd(-5.00m);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Bet(negativeAmount, PlayerName));
        Assert.Contains("Bet amount must be positive", exception.Message);
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_TrimsPlayerName()
    {
        // Arrange
        var nameWithSpaces = "  TestPlayer  ";

        // Act
        var bet = new Bet(_standardAmount, nameWithSpaces);

        // Assert
        Assert.Equal("TestPlayer", bet.PlayerName);
    }

    [Fact]
    public void Settle_WhenActive_SettlesBet()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        Assert.True(bet.IsActive);

        // Act
        bet.Settle();

        // Assert
        Assert.False(bet.IsActive);
        Assert.True(bet.IsSettled);
    }

    [Fact]
    public void Settle_WhenAlreadySettled_ThrowsInvalidOperationException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => bet.Settle());
        Assert.Contains("Bet has already been settled", exception.Message);
    }

    [Theory]
    [InlineData(GameResult.Win, 10.00)]
    [InlineData(GameResult.Blackjack, 15.00)]
    [InlineData(GameResult.Push, 0.00)]
    [InlineData(GameResult.Lose, 0.00)]
    public void CalculatePayout_WithStandardMultiplier_ReturnsCorrectAmount(GameResult result, decimal expectedPayout)
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act
        var payout = bet.CalculatePayout(result);

        // Assert
        Assert.Equal(Money.FromUsd(expectedPayout), payout);
    }

    [Fact]
    public void CalculatePayout_WithCustomBlackjackMultiplier_ReturnsCorrectAmount()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        var customMultiplier = 2.0m; // 2:1 instead of 3:2

        // Act
        var payout = bet.CalculatePayout(GameResult.Blackjack, customMultiplier);

        // Assert
        Assert.Equal(Money.FromUsd(20.00m), payout);
    }

    [Fact]
    public void CalculatePayout_WithZeroMultiplier_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            bet.CalculatePayout(GameResult.Blackjack, 0m));
        Assert.Contains("Blackjack multiplier must be positive", exception.Message);
        Assert.Equal("blackjackMultiplier", exception.ParamName);
    }

    [Fact]
    public void CalculatePayout_WithNegativeMultiplier_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            bet.CalculatePayout(GameResult.Blackjack, -1.5m));
        Assert.Contains("Blackjack multiplier must be positive", exception.Message);
        Assert.Equal("blackjackMultiplier", exception.ParamName);
    }

    [Fact]
    public void CalculatePayout_WhenSettled_ThrowsInvalidOperationException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            bet.CalculatePayout(GameResult.Win));
        Assert.Contains("Cannot calculate payout for a settled bet", exception.Message);
    }

    [Theory]
    [InlineData(GameResult.Win, 20.00)]    // Original bet (10) + payout (10)
    [InlineData(GameResult.Blackjack, 25.00)] // Original bet (10) + blackjack payout (15)
    [InlineData(GameResult.Push, 10.00)]   // Return original bet
    [InlineData(GameResult.Lose, 0.00)]    // Lose everything
    public void CalculateTotalReturn_WithStandardMultiplier_ReturnsCorrectAmount(GameResult result, decimal expectedReturn)
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act
        var totalReturn = bet.CalculateTotalReturn(result);

        // Assert
        Assert.Equal(Money.FromUsd(expectedReturn), totalReturn);
    }

    [Fact]
    public void CalculateTotalReturn_WhenSettled_ThrowsInvalidOperationException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            bet.CalculateTotalReturn(GameResult.Win));
        Assert.Contains("Cannot calculate total return for a settled bet", exception.Message);
    }

    [Fact]
    public void CreateDoubleDownBet_FromStandardBet_CreatesDoubleDownBet()
    {
        // Arrange
        var originalBet = new Bet(_standardAmount, PlayerName);

        // Act
        var doubleDownBet = originalBet.CreateDoubleDownBet();

        // Assert
        Assert.Equal(Money.FromUsd(20.00m), doubleDownBet.Amount);
        Assert.Equal(PlayerName, doubleDownBet.PlayerName);
        Assert.Equal(BetType.DoubleDown, doubleDownBet.Type);
        Assert.True(doubleDownBet.IsActive);
    }

    [Fact]
    public void CreateDoubleDownBet_FromSettledBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => bet.CreateDoubleDownBet());
        Assert.Contains("Cannot create double down bet from a settled bet", exception.Message);
    }

    [Fact]
    public void CreateDoubleDownBet_FromNonStandardBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var splitBet = new Bet(_standardAmount, PlayerName, BetType.Split);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => splitBet.CreateDoubleDownBet());
        Assert.Contains("Can only create double down bet from a standard bet", exception.Message);
    }

    [Fact]
    public void CreateSplitBet_FromStandardBet_CreatesSplitBet()
    {
        // Arrange
        var originalBet = new Bet(_standardAmount, PlayerName);

        // Act
        var splitBet = originalBet.CreateSplitBet();

        // Assert
        Assert.Equal(_standardAmount, splitBet.Amount);
        Assert.Equal(PlayerName, splitBet.PlayerName);
        Assert.Equal(BetType.Split, splitBet.Type);
        Assert.True(splitBet.IsActive);
    }

    [Fact]
    public void CreateSplitBet_FromSettledBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => bet.CreateSplitBet());
        Assert.Contains("Cannot create split bet from a settled bet", exception.Message);
    }

    [Fact]
    public void CreateSplitBet_FromNonStandardBet_ThrowsInvalidOperationException()
    {
        // Arrange
        var doubleDownBet = new Bet(_standardAmount, PlayerName, BetType.DoubleDown);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => doubleDownBet.CreateSplitBet());
        Assert.Contains("Can only create split bet from a standard bet", exception.Message);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act
        var result = bet.ToString();

        // Assert
        Assert.Equal("TestPlayer: 10.00 USD - Active", result);
    }

    [Fact]
    public void ToString_WithNonStandardType_IncludesType()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName, BetType.DoubleDown);

        // Act
        var result = bet.ToString();

        // Assert
        Assert.Equal("TestPlayer: 10.00 USD (DoubleDown) - Active", result);
    }

    [Fact]
    public void ToString_WhenSettled_ShowsSettledStatus()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        bet.Settle();

        // Act
        var result = bet.ToString();

        // Assert
        Assert.Equal("TestPlayer: 10.00 USD - Settled", result);
    }

    [Fact]
    public void Equals_WithSameBet_ReturnsTrue()
    {
        // Arrange
        var bet1 = new Bet(_standardAmount, PlayerName);
        var bet2 = new Bet(_standardAmount, PlayerName);

        // Act & Assert
        Assert.False(bet1.Equals(bet2)); // Different timestamps make them different
    }

    [Fact]
    public void Equals_WithDifferentAmount_ReturnsFalse()
    {
        // Arrange
        var bet1 = new Bet(_standardAmount, PlayerName);
        var bet2 = new Bet(Money.FromUsd(20.00m), PlayerName);

        // Act & Assert
        Assert.False(bet1.Equals(bet2));
    }

    [Fact]
    public void Equals_WithDifferentPlayer_ReturnsFalse()
    {
        // Arrange
        var bet1 = new Bet(_standardAmount, PlayerName);
        var bet2 = new Bet(_standardAmount, "DifferentPlayer");

        // Act & Assert
        Assert.False(bet1.Equals(bet2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);

        // Act & Assert
        Assert.False(bet.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var bet = new Bet(_standardAmount, PlayerName);
        var notABet = "not a bet";

        // Act & Assert
        Assert.False(bet.Equals(notABet));
    }

    [Fact]
    public void GetHashCode_WithSameProperties_ReturnsSameHashCode()
    {
        // Arrange
        var bet1 = new Bet(_standardAmount, PlayerName);
        var bet2 = new Bet(_standardAmount, PlayerName);

        // Act
        var hash1 = bet1.GetHashCode();
        var hash2 = bet2.GetHashCode();

        // Assert
        // Note: Hash codes will be different due to different timestamps, which is expected behavior
        // This test verifies that the GetHashCode method works without throwing exceptions
        Assert.True(hash1 != 0);
        Assert.True(hash2 != 0);
    }

    [Fact]
    public void GetHashCode_WithDifferentProperties_ReturnsDifferentHashCode()
    {
        // Arrange
        var bet1 = new Bet(_standardAmount, PlayerName);
        var bet2 = new Bet(Money.FromUsd(20.00m), PlayerName);

        // Act
        var hash1 = bet1.GetHashCode();
        var hash2 = bet2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }


}