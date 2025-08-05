using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Application.Models;
using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application;

/// <summary>
/// Tests for betting functionality in GameService.
/// </summary>
public class GameServiceBettingTests
{
    private readonly Mock<IShoe> _mockShoe;
    private readonly Mock<IGameRules> _mockGameRules;
    private readonly Mock<IBettingService> _mockBettingService;
    private readonly GameService _gameService;

    public GameServiceBettingTests()
    {
        _mockShoe = new Mock<IShoe>();
        _mockGameRules = new Mock<IGameRules>();
        _mockBettingService = new Mock<IBettingService>();

        // Setup default shoe behavior
        _mockShoe.Setup(s => s.RemainingCards).Returns(52);
        _mockShoe.Setup(s => s.IsEmpty).Returns(false);

        // Setup default betting service behavior
        _mockBettingService.Setup(bs => bs.MinimumBet).Returns(Money.FromUsd(5.00m));
        _mockBettingService.Setup(bs => bs.MaximumBet).Returns(Money.FromUsd(500.00m));
        _mockBettingService.Setup(bs => bs.BlackjackMultiplier).Returns(1.5m);

        _gameService = new GameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
    }

    [Fact]
    public async Task ProcessBettingRoundAsync_WhenNotInBettingPhase_ReturnsFailure()
    {
        // Arrange - game is in setup phase by default

        // Act
        var result = await _gameService.ProcessBettingRoundAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Game must be in Betting phase", result.Message);
    }

    [Fact]
    public async Task ProcessBettingRoundAsync_WhenNoPlayers_ReturnsFailure()
    {
        // Arrange - try to start game with empty player list (this will fail)
        try
        {
            _gameService.StartNewGame(new List<string>());
        }
        catch (ArgumentException)
        {
            // Expected - StartNewGame should fail with empty list
        }

        // Act
        var result = await _gameService.ProcessBettingRoundAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.Message.Contains("No players") || result.Message.Contains("Game must be in Betting phase"));
    }

    [Fact]
    public async Task ProcessBettingRoundAsync_WhenPlayersHaveSufficientFunds_ReturnsSuccess()
    {
        // Arrange
        var playerNames = new[] { "Player1", "Player2" };
        var minimumBet = Money.FromUsd(5.00m);
        var sufficientBankroll = Money.FromUsd(100.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
            .ReturnsAsync(sufficientBankroll);
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player2"))
            .ReturnsAsync(sufficientBankroll);

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.ProcessBettingRoundAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("Betting round validation completed successfully", result.Message);
        
        // Verify game phase advanced to InitialDeal
        var gameState = _gameService.GetCurrentGameState();
        Assert.Equal(GamePhase.InitialDeal, gameState?.CurrentPhase);
    }

    [Fact]
    public async Task ProcessBettingRoundAsync_WhenPlayerHasInsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1", "Player2" };
        var minimumBet = Money.FromUsd(5.00m);
        var sufficientBankroll = Money.FromUsd(100.00m);
        var insufficientBankroll = Money.FromUsd(2.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
            .ReturnsAsync(sufficientBankroll);
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player2"))
            .ReturnsAsync(insufficientBankroll);

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.ProcessBettingRoundAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Players with insufficient funds", result.Message);
        Assert.Contains("Player2", result.Message);
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenNotInBettingPhase_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);

        // Act
        var result = await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Game must be in Betting phase", result.Message);
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenPlayerNotFound_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.PlacePlayerBetAsync("NonExistentPlayer", betAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found in the current game", result.Message);
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenValidBet_ReturnsSuccess()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);
        var bet = new Bet(betAmount, "Player1");
        var initialBankroll = Money.FromUsd(100.00m);
        var updatedBankroll = Money.FromUsd(90.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
            .ReturnsAsync(initialBankroll);
        _mockBettingService.Setup(bs => bs.HasSufficientFundsAsync("Player1", betAmount))
            .ReturnsAsync(true);
        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet validation successful"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet));

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.Message}");
        Assert.Equal(bet, result.Bet);

        // Verify betting state was updated
        var bettingState = _gameService.GetCurrentBettingState();
        Assert.NotNull(bettingState);
        Assert.True(bettingState.HasPlayerBet("Player1"));
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenPlayerAlreadyHasBet_ReturnsFailure()
    {
        // Arrange - Use two players so betting doesn't complete after first bet
        var playerNames = new[] { "Player1", "Player2" };
        var betAmount = Money.FromUsd(10.00m);
        var bet = new Bet(betAmount, "Player1");
        var initialBankroll = Money.FromUsd(100.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync(It.IsAny<string>()))
            .ReturnsAsync(initialBankroll);
        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet validation successful"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet));

        _gameService.StartNewGame(playerNames);

        // Place first bet
        await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Act - try to place second bet for same player
        var result = await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already placed a bet", result.Message);
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenBettingServiceFails_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);

        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Failure("Insufficient funds"));

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Insufficient funds", result.Message);
    }

    [Fact]
    public void AllPlayersHaveBets_WhenNoBettingState_ReturnsFalse()
    {
        // Act
        var result = _gameService.AllPlayersHaveBets();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AllPlayersHaveBets_WhenAllPlayersHaveBets_ReturnsTrue()
    {
        // Arrange
        var playerNames = new[] { "Player1", "Player2" };
        var betAmount = Money.FromUsd(10.00m);
        var bet1 = new Bet(betAmount, "Player1");
        var bet2 = new Bet(betAmount, "Player2");
        var initialBankroll = Money.FromUsd(100.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync(It.IsAny<string>()))
            .ReturnsAsync(initialBankroll);
        _mockBettingService.Setup(bs => bs.ValidateBetAsync(It.IsAny<string>(), betAmount))
            .ReturnsAsync(BettingResult.Success("Bet validation successful"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet1));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player2", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet2));

        _gameService.StartNewGame(playerNames);

        // Place bets for both players
        await _gameService.PlacePlayerBetAsync("Player1", betAmount);
        await _gameService.PlacePlayerBetAsync("Player2", betAmount);

        // Act
        var result = _gameService.AllPlayersHaveBets();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AllPlayersHaveBets_WhenSomePlayersHaveBets_ReturnsFalse()
    {
        // Arrange
        var playerNames = new[] { "Player1", "Player2" };
        var betAmount = Money.FromUsd(10.00m);
        var bet1 = new Bet(betAmount, "Player1");
        var initialBankroll = Money.FromUsd(100.00m);

        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync(It.IsAny<string>()))
            .ReturnsAsync(initialBankroll);
        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet validation successful"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet1));

        _gameService.StartNewGame(playerNames);

        // Place bet for only one player
        await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Act
        var result = _gameService.AllPlayersHaveBets();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCurrentBettingState_WhenNoBettingRound_ReturnsNull()
    {
        // Act
        var result = _gameService.GetCurrentBettingState();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentBettingState_WhenBettingRoundActive_ReturnsBettingState()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        _gameService.StartNewGame(playerNames);

        // Act
        var result = _gameService.GetCurrentBettingState();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BettingPhase.WaitingForBets, result.CurrentPhase);
    }

    [Fact]
    public async Task ProcessBettingRoundAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.ProcessBettingRoundAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Error processing betting round", result.Message);
        Assert.Contains("Database error", result.Message);
    }

    [Fact]
    public async Task PlacePlayerBetAsync_WhenExceptionOccurs_ReturnsFailure()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);

        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ThrowsAsync(new InvalidOperationException("Network error"));

        _gameService.StartNewGame(playerNames);

        // Act
        var result = await _gameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Error placing bet", result.Message);
        Assert.Contains("Network error", result.Message);
    }
}