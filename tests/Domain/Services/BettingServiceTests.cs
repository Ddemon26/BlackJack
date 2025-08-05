using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.Services;

/// <summary>
/// Unit tests for the BettingService class.
/// Tests bet placement, validation, bankroll management, and payout processing.
/// </summary>
public class BettingServiceTests
{
    private readonly Money _defaultBankroll = Money.FromUsd(100.00m);
    private readonly Money _standardBet = Money.FromUsd(10.00m);
    private const string PlayerName = "TestPlayer";

    [Fact]
    public void Constructor_WithDefaultParameters_InitializesCorrectly()
    {
        // Arrange & Act
        var service = new BettingService();

        // Assert
        Assert.Equal(Money.FromUsd(1.00m), service.MinimumBet);
        Assert.Equal(Money.FromUsd(1000.00m), service.MaximumBet);
        Assert.Equal(1.5m, service.BlackjackMultiplier);
    }

    [Fact]
    public void Constructor_WithCustomParameters_InitializesCorrectly()
    {
        // Arrange
        var minBet = Money.FromUsd(5.00m);
        var maxBet = Money.FromUsd(500.00m);
        var multiplier = 2.0m;

        // Act
        var service = new BettingService(multiplier, minBet, maxBet);

        // Assert
        Assert.Equal(minBet, service.MinimumBet);
        Assert.Equal(maxBet, service.MaximumBet);
        Assert.Equal(multiplier, service.BlackjackMultiplier);
    }

    [Fact]
    public void Constructor_WithInvalidMultiplier_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => 
            new BettingService(blackjackMultiplier: 0m));
        Assert.Contains("Blackjack multiplier must be positive", exception.Message);
    }

    [Fact]
    public void Constructor_WithMinBetGreaterThanMaxBet_ThrowsArgumentException()
    {
        // Arrange
        var minBet = Money.FromUsd(100.00m);
        var maxBet = Money.FromUsd(50.00m);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new BettingService(minimumBet: minBet, maximumBet: maxBet));
        Assert.Contains("Minimum bet must be less than maximum bet", exception.Message);
    }

    [Fact]
    public async Task SetInitialBankrollAsync_WithValidAmount_SetsBankroll()
    {
        // Arrange
        var service = new BettingService();

        // Act
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);

        // Assert
        var bankroll = await service.GetPlayerBankrollAsync(PlayerName);
        Assert.Equal(_defaultBankroll, bankroll);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetInitialBankrollAsync_WithInvalidPlayerName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var service = new BettingService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            service.SetInitialBankrollAsync(invalidName, _defaultBankroll));
        Assert.Contains("Player name cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task SetInitialBankrollAsync_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var service = new BettingService();
        var negativeAmount = Money.FromUsd(-10.00m);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            service.SetInitialBankrollAsync(PlayerName, negativeAmount));
        Assert.Contains("Initial bankroll cannot be negative", exception.Message);
    }

    [Fact]
    public async Task GetPlayerBankrollAsync_WithUnknownPlayer_ReturnsZero()
    {
        // Arrange
        var service = new BettingService();

        // Act
        var bankroll = await service.GetPlayerBankrollAsync("UnknownPlayer");

        // Assert
        Assert.Equal(Money.Zero, bankroll);
    }

    [Fact]
    public async Task HasSufficientFundsAsync_WithSufficientFunds_ReturnsTrue()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);

        // Act
        var hasFunds = await service.HasSufficientFundsAsync(PlayerName, _standardBet);

        // Assert
        Assert.True(hasFunds);
    }

    [Fact]
    public async Task HasSufficientFundsAsync_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, Money.FromUsd(5.00m));

        // Act
        var hasFunds = await service.HasSufficientFundsAsync(PlayerName, _standardBet);

        // Assert
        Assert.False(hasFunds);
    }

    [Fact]
    public async Task ValidateBetAsync_WithValidBet_ReturnsSuccess()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);

        // Act
        var result = await service.ValidateBetAsync(PlayerName, _standardBet);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Bet validation successful", result.Message);
    }

    [Fact]
    public async Task ValidateBetAsync_WithInsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, Money.FromUsd(5.00m));

        // Act
        var result = await service.ValidateBetAsync(PlayerName, _standardBet);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Insufficient funds", result.Message);
    }

    [Fact]
    public async Task ValidateBetAsync_WithBetBelowMinimum_ReturnsFailure()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        var lowBet = Money.FromUsd(0.50m);

        // Act
        var result = await service.ValidateBetAsync(PlayerName, lowBet);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("below minimum bet", result.Message);
    }

    [Fact]
    public async Task ValidateBetAsync_WithBetAboveMaximum_ReturnsFailure()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, Money.FromUsd(2000.00m));
        var highBet = Money.FromUsd(1500.00m);

        // Act
        var result = await service.ValidateBetAsync(PlayerName, highBet);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("exceeds maximum bet", result.Message);
    }

    [Fact]
    public async Task PlaceBetAsync_WithValidBet_PlacesBetSuccessfully()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);

        // Act
        var result = await service.PlaceBetAsync(PlayerName, _standardBet);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Bet);
        Assert.Equal(_standardBet, result.Bet.Amount);
        Assert.Equal(PlayerName, result.Bet.PlayerName);

        // Verify bankroll was deducted
        var remainingBankroll = await service.GetPlayerBankrollAsync(PlayerName);
        Assert.Equal(Money.FromUsd(90.00m), remainingBankroll);
    }

    [Fact]
    public async Task PlaceBetAsync_WithExistingBet_ReturnsFailure()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        await service.PlaceBetAsync(PlayerName, _standardBet);

        // Act
        var result = await service.PlaceBetAsync(PlayerName, _standardBet);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already has an active bet", result.Message);
    }

    [Fact]
    public async Task GetCurrentBetAsync_WithExistingBet_ReturnsBet()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        await service.PlaceBetAsync(PlayerName, _standardBet);

        // Act
        var bet = await service.GetCurrentBetAsync(PlayerName);

        // Assert
        Assert.NotNull(bet);
        Assert.Equal(_standardBet, bet.Amount);
        Assert.Equal(PlayerName, bet.PlayerName);
    }

    [Fact]
    public async Task GetCurrentBetAsync_WithNoBet_ReturnsNull()
    {
        // Arrange
        var service = new BettingService();

        // Act
        var bet = await service.GetCurrentBetAsync(PlayerName);

        // Assert
        Assert.Null(bet);
    }

    [Fact]
    public async Task CalculatePayoutAsync_WithWin_ReturnsCorrectPayout()
    {
        // Arrange
        var service = new BettingService();
        var bet = new Bet(_standardBet, PlayerName);

        // Act
        var result = await service.CalculatePayoutAsync(GameResult.Win, bet);

        // Assert
        Assert.Equal(GameResult.Win, result.GameResult);
        Assert.Equal(_standardBet, result.PayoutAmount); // 1:1 payout
        Assert.Equal(Money.FromUsd(20.00m), result.TotalReturn); // Original bet + payout
    }

    [Fact]
    public async Task CalculatePayoutAsync_WithBlackjack_ReturnsCorrectPayout()
    {
        // Arrange
        var service = new BettingService();
        var bet = new Bet(_standardBet, PlayerName);

        // Act
        var result = await service.CalculatePayoutAsync(GameResult.Blackjack, bet);

        // Assert
        Assert.Equal(GameResult.Blackjack, result.GameResult);
        Assert.Equal(Money.FromUsd(15.00m), result.PayoutAmount); // 3:2 payout
        Assert.Equal(Money.FromUsd(25.00m), result.TotalReturn); // Original bet + payout
    }

    [Fact]
    public async Task CalculatePayoutAsync_WithLoss_ReturnsZeroPayout()
    {
        // Arrange
        var service = new BettingService();
        var bet = new Bet(_standardBet, PlayerName);

        // Act
        var result = await service.CalculatePayoutAsync(GameResult.Lose, bet);

        // Assert
        Assert.Equal(GameResult.Lose, result.GameResult);
        Assert.Equal(Money.Zero, result.PayoutAmount);
        Assert.Equal(Money.Zero, result.TotalReturn);
    }

    [Fact]
    public async Task CalculatePayoutAsync_WithPush_ReturnsOriginalBet()
    {
        // Arrange
        var service = new BettingService();
        var bet = new Bet(_standardBet, PlayerName);

        // Act
        var result = await service.CalculatePayoutAsync(GameResult.Push, bet);

        // Assert
        Assert.Equal(GameResult.Push, result.GameResult);
        Assert.Equal(Money.Zero, result.PayoutAmount);
        Assert.Equal(_standardBet, result.TotalReturn); // Return original bet
    }

    [Fact]
    public async Task UpdateBankrollAsync_WithPositiveAmount_IncreasesBankroll()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        var addition = Money.FromUsd(50.00m);

        // Act
        await service.UpdateBankrollAsync(PlayerName, addition);

        // Assert
        var newBankroll = await service.GetPlayerBankrollAsync(PlayerName);
        Assert.Equal(Money.FromUsd(150.00m), newBankroll);
    }

    [Fact]
    public async Task UpdateBankrollAsync_WithNegativeAmount_DecreasesBankroll()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        var deduction = Money.FromUsd(-30.00m);

        // Act
        await service.UpdateBankrollAsync(PlayerName, deduction);

        // Assert
        var newBankroll = await service.GetPlayerBankrollAsync(PlayerName);
        Assert.Equal(Money.FromUsd(70.00m), newBankroll);
    }

    [Fact]
    public async Task UpdateBankrollAsync_WithExcessiveNegativeAmount_SetsBankrollToZero()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        var excessiveDeduction = Money.FromUsd(-200.00m);

        // Act
        await service.UpdateBankrollAsync(PlayerName, excessiveDeduction);

        // Assert
        var newBankroll = await service.GetPlayerBankrollAsync(PlayerName);
        Assert.Equal(Money.Zero, newBankroll);
    }

    [Fact]
    public async Task ClearAllBetsAsync_RemovesAllBets()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        await service.SetInitialBankrollAsync("Player2", _defaultBankroll);
        await service.PlaceBetAsync(PlayerName, _standardBet);
        await service.PlaceBetAsync("Player2", _standardBet);

        // Act
        await service.ClearAllBetsAsync();

        // Assert
        var bet1 = await service.GetCurrentBetAsync(PlayerName);
        var bet2 = await service.GetCurrentBetAsync("Player2");
        Assert.Null(bet1);
        Assert.Null(bet2);
    }

    [Fact]
    public async Task ProcessPayoutsAsync_WithMultiplePlayers_ProcessesAllPayouts()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync("Player1", _defaultBankroll);
        await service.SetInitialBankrollAsync("Player2", _defaultBankroll);
        await service.PlaceBetAsync("Player1", _standardBet);
        await service.PlaceBetAsync("Player2", _standardBet);

        var playerResults = new Dictionary<string, GameResult>
        {
            { "Player1", GameResult.Win },
            { "Player2", GameResult.Lose }
        };

        // Act
        var summary = await service.ProcessPayoutsAsync(playerResults);

        // Assert
        Assert.Equal(2, summary.TotalPayouts);
        Assert.Equal(1, summary.WinCount);
        Assert.Equal(1, summary.LossCount);

        // Verify bankroll updates
        var player1Bankroll = await service.GetPlayerBankrollAsync("Player1");
        var player2Bankroll = await service.GetPlayerBankrollAsync("Player2");
        Assert.Equal(Money.FromUsd(110.00m), player1Bankroll); // 90 + 20 (bet + payout)
        Assert.Equal(Money.FromUsd(90.00m), player2Bankroll); // 90 + 0 (lost bet)
    }

    [Fact]
    public async Task ProcessPayoutsAsync_WithNullPlayerResults_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new BettingService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            service.ProcessPayoutsAsync(null!));
    }

    [Fact]
    public void GetAllCurrentBets_ReturnsAllBets()
    {
        // Arrange
        var service = new BettingService();

        // Act
        var bets = service.GetAllCurrentBets();

        // Assert
        Assert.NotNull(bets);
        Assert.Empty(bets);
    }

    [Fact]
    public void GetAllBankrolls_ReturnsAllBankrolls()
    {
        // Arrange
        var service = new BettingService();

        // Act
        var bankrolls = service.GetAllBankrolls();

        // Assert
        Assert.NotNull(bankrolls);
        Assert.Empty(bankrolls);
    }

    [Fact]
    public async Task PlaceBetAsync_TrimsPlayerName()
    {
        // Arrange
        var service = new BettingService();
        var nameWithSpaces = "  TestPlayer  ";
        await service.SetInitialBankrollAsync(nameWithSpaces, _defaultBankroll);

        // Act
        var result = await service.PlaceBetAsync(nameWithSpaces, _standardBet);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TestPlayer", result.Bet!.PlayerName);
    }

    [Fact]
    public async Task ValidateBetAsync_WithWrongCurrency_ReturnsFailure()
    {
        // Arrange
        var service = new BettingService();
        await service.SetInitialBankrollAsync(PlayerName, _defaultBankroll);
        var euroBet = Money.FromCurrency(10.00m, "EUR");

        // Act
        var result = await service.ValidateBetAsync(PlayerName, euroBet);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("currency", result.Message.ToLower());
    }
}