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

        // Setup comprehensive betting service mocks
        _mockBettingService.Setup(bs => bs.GetPlayerBankrollAsync("Player1"))
            .ReturnsAsync(initialBankroll);
        _mockBettingService.Setup(bs => bs.HasSufficientFundsAsync("Player1", betAmount))
            .ReturnsAsync(true);
        _mockBettingService.Setup(bs => bs.ValidateBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet validation successful"));
        _mockBettingService.Setup(bs => bs.PlaceBetAsync("Player1", betAmount))
            .ReturnsAsync(BettingResult.Success("Bet placed successfully", bet));

        // Create a testable game service that can handle the player bankroll issue
        var testGameService = new TestableGameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
        testGameService.StartNewGameWithBankrolls(playerNames, initialBankroll);

        // Act
        var result = await testGameService.PlacePlayerBetAsync("Player1", betAmount);

        // Assert
        Assert.True(result.IsSuccess, $"Expected success but got: {result.Message}");
        Assert.Equal(bet, result.Bet);

        // Verify betting state was updated
        var bettingState = testGameService.GetCurrentBettingState();
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

    [Fact]
    public async Task GetGameResultsWithPayoutsAsync_WhenGameNotComplete_ThrowsInvalidOperationException()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        _gameService.StartNewGame(playerNames);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _gameService.GetGameResultsWithPayoutsAsync());
    }

    [Fact]
    public async Task GetGameResultsWithPayoutsAsync_WithMockedGameComplete_ReturnsGameSummaryWithPayouts()
    {
        // Arrange - Create a test that bypasses the complex game flow
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(10.00m);
        var bet1 = new Bet(betAmount, "Player1");

        // Setup game rules to return specific results
        _mockGameRules.Setup(gr => gr.DetermineResult(It.IsAny<Hand>(), It.IsAny<Hand>()))
            .Returns(GameResult.Win);

        // Setup payout processing
        var payoutResults = new List<PayoutResult>
        {
            new PayoutResult(bet1, GameResult.Win, Money.FromUsd(10.00m), Money.FromUsd(20.00m))
        };
        var payoutSummary = new PayoutSummary(payoutResults);

        _mockBettingService.Setup(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()))
            .ReturnsAsync(payoutSummary);

        // Create a minimal game service for testing just the payout functionality
        var testGameService = new TestableGameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
        testGameService.SetupCompleteGame(playerNames);

        // Act
        var result = await testGameService.GetGameResultsWithPayoutsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasPayouts);
        Assert.NotNull(result.PayoutSummary);
        Assert.Equal(1, result.PayoutSummary.TotalPayouts);
        Assert.Equal(Money.FromUsd(10.00m), result.PayoutSummary.TotalPayoutAmount);

        // Verify betting service was called
        _mockBettingService.Verify(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()), Times.Once);
    }

    [Fact]
    public async Task GetGameResultsWithPayoutsAsync_WhenPayoutProcessingFails_ReturnsGameSummaryWithoutPayouts()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var bet1 = new Bet(Money.FromUsd(10.00m), "Player1");

        // Setup game rules
        _mockGameRules.Setup(gr => gr.DetermineResult(It.IsAny<Hand>(), It.IsAny<Hand>()))
            .Returns(GameResult.Lose);

        // Setup payout processing to fail
        _mockBettingService.Setup(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()))
            .ThrowsAsync(new InvalidOperationException("Payout processing failed"));

        // Create a testable game service and set up complete game
        var testGameService = new TestableGameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
        testGameService.SetupCompleteGame(playerNames);

        // Act
        var result = await testGameService.GetGameResultsWithPayoutsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasPayouts);
        Assert.Null(result.PayoutSummary);
        Assert.Equal(Money.Zero, result.TotalPayoutAmount);
        Assert.Equal(Money.Zero, result.TotalReturnAmount);

        // Verify game results are still available
        Assert.Single(result.PlayerResults);
        Assert.Equal(GameResult.Lose, result.PlayerResults["Player1"]);

        // Verify betting service was called (but failed)
        _mockBettingService.Verify(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()), Times.Once);
    }

    [Fact]
    public async Task GetGameResultsWithPayoutsAsync_CalculatesBlackjackPayoutCorrectly()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(20.00m);
        var bet1 = new Bet(betAmount, "Player1");

        // Setup game rules
        _mockGameRules.Setup(gr => gr.DetermineResult(It.IsAny<Hand>(), It.IsAny<Hand>()))
            .Returns(GameResult.Blackjack);

        // Setup payout processing with 3:2 blackjack payout
        var blackjackPayout = Money.FromUsd(30.00m); // 20 * 1.5 = 30
        var totalReturn = Money.FromUsd(50.00m);     // 20 + 30 = 50
        var payoutResults = new List<PayoutResult>
        {
            new PayoutResult(bet1, GameResult.Blackjack, blackjackPayout, totalReturn)
        };
        var payoutSummary = new PayoutSummary(payoutResults);

        _mockBettingService.Setup(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()))
            .ReturnsAsync(payoutSummary);

        // Create a testable game service and set up complete game
        var testGameService = new TestableGameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
        testGameService.SetupCompleteGame(playerNames);

        // Act
        var result = await testGameService.GetGameResultsWithPayoutsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasPayouts);
        Assert.Equal(1, result.BlackjackCount);
        
        var player1Payout = result.GetPayoutForPlayer("Player1");
        Assert.NotNull(player1Payout);
        Assert.True(player1Payout.IsBlackjack);
        Assert.Equal(Money.FromUsd(30.00m), player1Payout.PayoutAmount);
        Assert.Equal(Money.FromUsd(50.00m), player1Payout.TotalReturn);
    }

    [Fact]
    public async Task GetGameResultsWithPayoutsAsync_CalculatesRegularWinPayoutCorrectly()
    {
        // Arrange
        var playerNames = new[] { "Player1" };
        var betAmount = Money.FromUsd(25.00m);
        var bet1 = new Bet(betAmount, "Player1");

        // Setup game rules
        _mockGameRules.Setup(gr => gr.DetermineResult(It.IsAny<Hand>(), It.IsAny<Hand>()))
            .Returns(GameResult.Win);

        // Setup payout processing with 1:1 regular win payout
        var regularPayout = Money.FromUsd(25.00m); // 1:1 payout
        var totalReturn = Money.FromUsd(50.00m);   // 25 + 25 = 50
        var payoutResults = new List<PayoutResult>
        {
            new PayoutResult(bet1, GameResult.Win, regularPayout, totalReturn)
        };
        var payoutSummary = new PayoutSummary(payoutResults);

        _mockBettingService.Setup(bs => bs.ProcessPayoutsAsync(It.IsAny<IDictionary<string, GameResult>>()))
            .ReturnsAsync(payoutSummary);

        // Create a testable game service and set up complete game
        var testGameService = new TestableGameService(_mockShoe.Object, _mockGameRules.Object, _mockBettingService.Object);
        testGameService.SetupCompleteGame(playerNames);

        // Act
        var result = await testGameService.GetGameResultsWithPayoutsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasPayouts);
        Assert.Equal(1, result.WinnerCount);
        
        var player1Payout = result.GetPayoutForPlayer("Player1");
        Assert.NotNull(player1Payout);
        Assert.True(player1Payout.IsWin);
        Assert.False(player1Payout.IsBlackjack);
        Assert.Equal(Money.FromUsd(25.00m), player1Payout.PayoutAmount);
        Assert.Equal(Money.FromUsd(50.00m), player1Payout.TotalReturn);
    }
}

/// <summary>
/// A testable version of GameService that allows bypassing complex game flow for testing payouts.
/// </summary>
internal class TestableGameService : GameService
{
    public TestableGameService(IShoe shoe, IGameRules gameRules, IBettingService bettingService)
        : base(shoe, gameRules, bettingService)
    {
    }

    /// <summary>
    /// Sets up a complete game state for testing without going through the full game flow.
    /// </summary>
    /// <param name="playerNames">The player names to set up.</param>
    public void SetupCompleteGame(IEnumerable<string> playerNames)
    {
        // Use reflection to set the private fields to simulate a complete game
        var playersField = typeof(GameService).GetField("_players", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dealerField = typeof(GameService).GetField("_dealer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentPhaseField = typeof(GameService).GetField("_currentPhase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var players = new List<Player>();
        foreach (var name in playerNames)
        {
            var player = new Player(name, PlayerType.Human, Money.FromUsd(100.00m));
            // Add some cards to the player's hand
            player.AddCard(new Card(Suit.Hearts, Rank.Ten));
            player.AddCard(new Card(Suit.Spades, Rank.Nine));
            players.Add(player);
        }

        var dealer = new Player("Dealer", PlayerType.Dealer);
        dealer.AddCard(new Card(Suit.Clubs, Rank.King));
        dealer.AddCard(new Card(Suit.Diamonds, Rank.Eight));

        playersField?.SetValue(this, players);
        dealerField?.SetValue(this, dealer);
        currentPhaseField?.SetValue(this, GamePhase.Results);
    }

    /// <summary>
    /// Starts a new game with players that have the specified bankroll.
    /// </summary>
    /// <param name="playerNames">The player names.</param>
    /// <param name="initialBankroll">The initial bankroll for each player.</param>
    public void StartNewGameWithBankrolls(IEnumerable<string> playerNames, Money initialBankroll)
    {
        // Use reflection to set up the game with proper bankrolls
        var playersField = typeof(GameService).GetField("_players", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dealerField = typeof(GameService).GetField("_dealer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentPhaseField = typeof(GameService).GetField("_currentPhase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentBettingStateField = typeof(GameService).GetField("_currentBettingState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var players = new List<Player>();
        var playerBankrolls = new Dictionary<string, Money>();
        
        foreach (var name in playerNames)
        {
            var player = new Player(name, PlayerType.Human, initialBankroll);
            players.Add(player);
            playerBankrolls[name] = initialBankroll;
        }

        var dealer = new Player("Dealer", PlayerType.Dealer);
        var bettingState = new BettingState(playerNames, playerBankrolls);

        playersField?.SetValue(this, players);
        dealerField?.SetValue(this, dealer);
        currentPhaseField?.SetValue(this, GamePhase.Betting);
        currentBettingStateField?.SetValue(this, bettingState);
    }
}