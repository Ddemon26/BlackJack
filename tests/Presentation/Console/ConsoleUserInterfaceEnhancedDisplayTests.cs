using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Presentation.Console;
using Moq;
using Xunit;

namespace GroupProject.Tests.Presentation.Console;

/// <summary>
/// Unit tests for enhanced display features in ConsoleUserInterface.
/// Tests multi-hand display, statistics display, and session summaries.
/// </summary>
public class ConsoleUserInterfaceEnhancedDisplayTests
{
    private readonly Mock<IInputProvider> _mockInputProvider;
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly ConsoleUserInterface _consoleUserInterface;

    public ConsoleUserInterfaceEnhancedDisplayTests()
    {
        _mockInputProvider = new Mock<IInputProvider>();
        _mockOutputProvider = new Mock<IOutputProvider>();
        _consoleUserInterface = new ConsoleUserInterface(_mockInputProvider.Object, _mockOutputProvider.Object);
    }

    [Fact]
    public async Task ShowMultiHandPlayerAsync_SingleHand_DisplaysCorrectly()
    {
        // Arrange
        var player = new Player("TestPlayer", PlayerType.Human, new Money(100m));
        player.PlaceBet(new Money(25m));
        player.Hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.Hand.AddCard(new Card(Suit.Spades, Rank.King));

        var multiHandPlayer = new MultiHandPlayer(player);

        // Act
        await _consoleUserInterface.ShowMultiHandPlayerAsync(multiHandPlayer);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("PLAYER: TestPlayer"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Bankroll: $75.00"))), Times.Once); // 100 - 25 bet
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Hand") && s.Contains("Current"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Bet: $25.00"))), Times.Once);
    }

    [Fact]
    public async Task ShowMultiHandPlayerAsync_MultipleHands_DisplaysAllHands()
    {
        // Arrange
        var player = new Player("TestPlayer", PlayerType.Human, new Money(200m));
        var multiHandPlayer = new MultiHandPlayer(player);

        // Add multiple hands manually (simulating split scenario)
        var hand1 = new Hand();
        hand1.AddCard(new Card(Suit.Hearts, Rank.Eight));
        hand1.AddCard(new Card(Suit.Spades, Rank.Three));
        hand1.MarkAsSplitHand();

        var hand2 = new Hand();
        hand2.AddCard(new Card(Suit.Diamonds, Rank.Eight));
        hand2.AddCard(new Card(Suit.Clubs, Rank.Seven));
        hand2.MarkAsSplitHand();

        var bet1 = new Bet(new Money(25m), "TestPlayer", BetType.Split);
        var bet2 = new Bet(new Money(25m), "TestPlayer", BetType.Split);

        multiHandPlayer.AddHand(hand1, bet1);
        multiHandPlayer.AddHand(hand2, bet2);

        // Act
        await _consoleUserInterface.ShowMultiHandPlayerAsync(multiHandPlayer);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Total Bet: $50.00 (2 hands)"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Hand #1 (Split)"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Hand #2 (Split)"))), Times.Once);
    }

    [Fact]
    public async Task ShowMultiHandPlayerAsync_NullPlayer_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowMultiHandPlayerAsync(null));
    }

    [Fact]
    public async Task ShowPlayerStatisticsAsync_WithSessionStats_DisplaysBothSections()
    {
        // Arrange
        var playerName = "TestPlayer";
        var lifetimeStats = new PlayerStatistics(playerName, 100, 45, 40, 15, 5, 
            new Money(2500m), new Money(250m), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var sessionStats = new PlayerStatistics(playerName, 10, 6, 3, 1, 1, 
            new Money(250m), new Money(50m), DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        // Act
        await _consoleUserInterface.ShowPlayerStatisticsAsync(playerName, lifetimeStats, sessionStats);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("STATISTICS - TESTPLAYER"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("LIFETIME STATISTICS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION STATISTICS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Games Played: 100"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Session Games: 10"))), Times.Once);
    }

    [Fact]
    public async Task ShowPlayerStatisticsAsync_WithoutSessionStats_DisplaysOnlyLifetime()
    {
        // Arrange
        var playerName = "TestPlayer";
        var lifetimeStats = new PlayerStatistics(playerName, 50, 25, 20, 5, 3, 
            new Money(1250m), new Money(125m), DateTime.UtcNow.AddDays(-15), DateTime.UtcNow);

        // Act
        await _consoleUserInterface.ShowPlayerStatisticsAsync(playerName, lifetimeStats);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("LIFETIME STATISTICS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION STATISTICS:"))), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ShowPlayerStatisticsAsync_InvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var stats = new PlayerStatistics("ValidName");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.ShowPlayerStatisticsAsync(playerName, stats));
    }

    [Fact]
    public async Task ShowPlayerStatisticsAsync_NullStatistics_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowPlayerStatisticsAsync("TestPlayer", null));
    }

    [Fact]
    public async Task ShowSessionSummaryAsync_ValidSummary_DisplaysAllSections()
    {
        // Arrange
        var playerStats = new Dictionary<string, PlayerStatistics>
        {
            { "Player1", new PlayerStatistics("Player1", 10, 6, 3, 1, 1, new Money(250m), new Money(50m), DateTime.UtcNow.AddHours(-2), DateTime.UtcNow) },
            { "Player2", new PlayerStatistics("Player2", 10, 4, 5, 1, 0, new Money(250m), new Money(-25m), DateTime.UtcNow.AddHours(-2), DateTime.UtcNow) }
        };

        var finalBankrolls = new Dictionary<string, Money>
        {
            { "Player1", new Money(150m) },
            { "Player2", new Money(75m) }
        };

        var sessionSummary = new SessionSummary(
            "session-123",
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow,
            20,
            playerStats,
            finalBankrolls
        );

        // Act
        await _consoleUserInterface.ShowSessionSummaryAsync(sessionSummary);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION SUMMARY"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION OVERVIEW:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("FINANCIAL SUMMARY:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("PLAYER RESULTS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION HIGHLIGHTS:"))), Times.Once);
    }

    [Fact]
    public async Task ShowSessionSummaryAsync_NullSummary_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowSessionSummaryAsync(null));
    }

    [Fact]
    public async Task ShowCompactSessionSummaryAsync_ValidSummary_DisplaysCompactFormat()
    {
        // Arrange
        var playerStats = new Dictionary<string, PlayerStatistics>
        {
            { "Player1", new PlayerStatistics("Player1", 5, 3, 2, 0, 1, new Money(125m), new Money(25m), DateTime.UtcNow.AddHours(-1), DateTime.UtcNow) }
        };

        var finalBankrolls = new Dictionary<string, Money>
        {
            { "Player1", new Money(125m) }
        };

        var sessionSummary = new SessionSummary(
            "session-456",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            5,
            playerStats,
            finalBankrolls
        );

        // Act
        await _consoleUserInterface.ShowCompactSessionSummaryAsync(sessionSummary);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("SESSION SUMMARY:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Duration:") && s.Contains("Rounds:") && s.Contains("Players:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("🏆 Top Player:"))), Times.Once);
    }

    [Fact]
    public async Task ShowCompactSessionSummaryAsync_NullSummary_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowCompactSessionSummaryAsync(null));
    }

    [Fact]
    public async Task ShowEnhancedGameResultsAsync_WithPayoutSummary_DisplaysAllSections()
    {
        // Arrange
        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.King));
        dealerHand.AddCard(new Card(Suit.Spades, Rank.Seven));

        var playerResults = new Dictionary<string, GameResult>
        {
            { "Player1", GameResult.Win },
            { "Player2", GameResult.Blackjack },
            { "Player3", GameResult.Lose }
        };

        var payouts = new List<PayoutResult>
        {
            new PayoutResult(new Bet(new Money(25m), "Player1"), GameResult.Win, new Money(25m), new Money(50m)),
            new PayoutResult(new Bet(new Money(25m), "Player2"), GameResult.Blackjack, new Money(37.50m), new Money(62.50m))
        };
        var payoutSummary = new PayoutSummary(payouts);

        var gameSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow, payoutSummary);

        // Act
        await _consoleUserInterface.ShowEnhancedGameResultsAsync(gameSummary, payoutSummary);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("ROUND RESULTS"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("DEALER FINAL HAND:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("PLAYER RESULTS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("PAYOUT SUMMARY:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("ROUND SUMMARY:"))), Times.Once);
    }

    [Fact]
    public async Task ShowEnhancedGameResultsAsync_WithoutPayoutSummary_DisplaysBasicSections()
    {
        // Arrange
        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.Queen));
        dealerHand.AddCard(new Card(Suit.Spades, Rank.Ten));

        var playerResults = new Dictionary<string, GameResult>
        {
            { "Player1", GameResult.Push }
        };

        var gameSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act
        await _consoleUserInterface.ShowEnhancedGameResultsAsync(gameSummary);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("ROUND RESULTS"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("PAYOUT SUMMARY:"))), Times.Never);
    }

    [Fact]
    public async Task ShowEnhancedGameResultsAsync_NullResults_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowEnhancedGameResultsAsync(null));
    }

    [Theory]
    [InlineData(GameResult.Win, "🟢", "WIN")]
    [InlineData(GameResult.Blackjack, "⭐", "BLACKJACK WIN")]
    [InlineData(GameResult.Push, "🟡", "PUSH (TIE)")]
    [InlineData(GameResult.Lose, "🔴", "LOSE")]
    public async Task ShowEnhancedGameResultsAsync_DifferentResults_DisplaysCorrectSymbols(
        GameResult gameResult, string expectedSymbol, string expectedText)
    {
        // Arrange
        var dealerHand = new Hand();
        dealerHand.AddCard(new Card(Suit.Hearts, Rank.Jack));
        dealerHand.AddCard(new Card(Suit.Spades, Rank.Nine));

        var playerResults = new Dictionary<string, GameResult>
        {
            { "TestPlayer", gameResult }
        };

        var gameSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act
        await _consoleUserInterface.ShowEnhancedGameResultsAsync(gameSummary);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains(expectedSymbol) && s.Contains("TestPlayer") && s.Contains(expectedText))), Times.Once);
    }
}