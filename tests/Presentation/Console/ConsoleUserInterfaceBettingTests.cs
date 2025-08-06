using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Presentation.Console;
using Moq;
using Xunit;

namespace GroupProject.Tests.Presentation.Console;

/// <summary>
/// Unit tests for betting interface components in ConsoleUserInterface.
/// Tests betting prompts, bankroll display, and betting validation feedback.
/// </summary>
public class ConsoleUserInterfaceBettingTests
{
    private readonly Mock<IInputProvider> _mockInputProvider;
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly ConsoleUserInterface _consoleUserInterface;

    public ConsoleUserInterfaceBettingTests()
    {
        _mockInputProvider = new Mock<IInputProvider>();
        _mockOutputProvider = new Mock<IOutputProvider>();
        _consoleUserInterface = new ConsoleUserInterface(_mockInputProvider.Object, _mockOutputProvider.Object);
    }

    [Fact]
    public async Task GetBetAmountAsync_ValidInput_ReturnsCorrectAmount()
    {
        // Arrange
        var playerName = "TestPlayer";
        var minBet = new Money(5m);
        var maxBet = new Money(100m);
        var availableFunds = new Money(200m);
        var expectedBet = new Money(25m);

        _mockInputProvider.Setup(x => x.GetBetAmountAsync(
            "Enter your bet amount", minBet, maxBet, availableFunds))
            .ReturnsAsync(expectedBet);

        // Act
        var result = await _consoleUserInterface.GetBetAmountAsync(playerName, minBet, maxBet, availableFunds);

        // Assert
        Assert.Equal(expectedBet, result);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s != null && s.Contains("BETTING TIME"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s != null && s.Contains(playerName.ToUpper()))), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetBetAmountAsync_InvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var minBet = new Money(5m);
        var maxBet = new Money(100m);
        var availableFunds = new Money(200m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.GetBetAmountAsync(playerName, minBet, maxBet, availableFunds));
    }

    [Fact]
    public async Task ShowBankrollInfoAsync_WithCurrentBet_DisplaysAllInformation()
    {
        // Arrange
        var playerName = "TestPlayer";
        var bankroll = new Money(150m);
        var currentBet = new Bet(new Money(25m), playerName);
        var minBet = new Money(5m);
        var maxBet = new Money(100m);

        // Act
        await _consoleUserInterface.ShowBankrollInfoAsync(playerName, bankroll, currentBet, minBet, maxBet);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains(playerName) && s.Contains("$150.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Current Bet") && s.Contains("$25.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Betting Limits") && s.Contains("$5.00") && s.Contains("$100.00"))), Times.Once);
    }

    [Fact]
    public async Task ShowBankrollInfoAsync_WithoutCurrentBet_DisplaysBasicInformation()
    {
        // Arrange
        var playerName = "TestPlayer";
        var bankroll = new Money(150m);

        // Act
        await _consoleUserInterface.ShowBankrollInfoAsync(playerName, bankroll);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains(playerName) && s.Contains("$150.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Current Bet"))), Times.Never);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Betting Limits"))), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ShowBankrollInfoAsync_InvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var bankroll = new Money(150m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.ShowBankrollInfoAsync(playerName, bankroll));
    }

    [Fact]
    public async Task ShowBetConfirmationAsync_StandardBet_DisplaysConfirmation()
    {
        // Arrange
        var playerName = "TestPlayer";
        var bet = new Bet(new Money(25m), playerName);

        // Act
        await _consoleUserInterface.ShowBetConfirmationAsync(playerName, bet);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("✅") && s.Contains(playerName) && s.Contains("$25.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Bet Type"))), Times.Never);
    }

    [Fact]
    public async Task ShowBetConfirmationAsync_DoubleDownBet_DisplaysBetType()
    {
        // Arrange
        var playerName = "TestPlayer";
        var bet = new Bet(new Money(50m), playerName, BetType.DoubleDown);

        // Act
        await _consoleUserInterface.ShowBetConfirmationAsync(playerName, bet);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("✅") && s.Contains(playerName) && s.Contains("$50.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Bet Type") && s.Contains("DoubleDown"))), Times.Once);
    }

    [Fact]
    public async Task ShowBetConfirmationAsync_NullBet_ThrowsArgumentNullException()
    {
        // Arrange
        var playerName = "TestPlayer";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowBetConfirmationAsync(playerName, null));
    }

    [Fact]
    public async Task ShowBetValidationErrorAsync_ValidInput_DisplaysErrorMessage()
    {
        // Arrange
        var playerName = "TestPlayer";
        var attemptedBet = new Money(150m);
        var reason = "Exceeds maximum bet limit";
        var availableFunds = new Money(200m);

        // Act
        await _consoleUserInterface.ShowBetValidationErrorAsync(playerName, attemptedBet, reason, availableFunds);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("❌") && s.Contains(playerName) && s.Contains("$150.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Reason") && s.Contains(reason))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Available funds") && s.Contains("$200.00"))), Times.Once);
    }

    [Theory]
    [InlineData("", "Valid reason")]
    [InlineData("TestPlayer", "")]
    [InlineData("TestPlayer", null)]
    public async Task ShowBetValidationErrorAsync_InvalidInput_ThrowsArgumentException(string playerName, string reason)
    {
        // Arrange
        var attemptedBet = new Money(150m);
        var availableFunds = new Money(200m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.ShowBetValidationErrorAsync(playerName, attemptedBet, reason, availableFunds));
    }

    [Fact]
    public async Task GetInitialBankrollAsync_ValidInput_ReturnsCorrectAmount()
    {
        // Arrange
        var playerName = "TestPlayer";
        var defaultAmount = new Money(100m);
        var minAmount = new Money(50m);
        var maxAmount = new Money(500m);
        var expectedBankroll = new Money(200m);

        _mockInputProvider.Setup(x => x.GetInitialBankrollAsync(
            playerName, defaultAmount, minAmount, maxAmount))
            .ReturnsAsync(expectedBankroll);

        // Act
        var result = await _consoleUserInterface.GetInitialBankrollAsync(playerName, defaultAmount, minAmount, maxAmount);

        // Assert
        Assert.Equal(expectedBankroll, result);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s != null && s.Contains("BANKROLL SETUP"))), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetInitialBankrollAsync_InvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var defaultAmount = new Money(100m);
        var minAmount = new Money(50m);
        var maxAmount = new Money(500m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.GetInitialBankrollAsync(playerName, defaultAmount, minAmount, maxAmount));
    }

    [Fact]
    public async Task ShowBettingRoundSummaryAsync_WithBets_DisplaysAllBets()
    {
        // Arrange
        var playerBets = new Dictionary<string, Bet>
        {
            { "Player1", new Bet(new Money(25m), "Player1") },
            { "Player2", new Bet(new Money(50m), "Player2", BetType.DoubleDown) }
        };
        var totalPot = new Money(75m);

        // Act
        await _consoleUserInterface.ShowBettingRoundSummaryAsync(playerBets, totalPot);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s != null && s.Contains("BETTING ROUND COMPLETE"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Player1") && s.Contains("$25.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Player2") && s.Contains("$50.00") && s.Contains("DoubleDown"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("TOTAL POT") && s.Contains("$75.00"))), Times.Once);
    }

    [Fact]
    public async Task ShowBettingRoundSummaryAsync_NoBets_DisplaysNoBetsMessage()
    {
        // Arrange
        var playerBets = new Dictionary<string, Bet>();
        var totalPot = new Money(0m);

        // Act
        await _consoleUserInterface.ShowBettingRoundSummaryAsync(playerBets, totalPot);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => s != null && s.Contains("No bets placed"))), Times.Once);
    }

    [Fact]
    public async Task ShowBettingRoundSummaryAsync_NullPlayerBets_ThrowsArgumentNullException()
    {
        // Arrange
        var totalPot = new Money(0m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowBettingRoundSummaryAsync(null, totalPot));
    }

    [Theory]
    [InlineData(GameResult.Win, "✅", "WIN")]
    [InlineData(GameResult.Blackjack, "🎉", "BLACKJACK!")]
    [InlineData(GameResult.Push, "🤝", "PUSH")]
    [InlineData(GameResult.Lose, "❌", "LOSE")]
    public async Task ShowPayoutInfoAsync_DifferentResults_DisplaysCorrectSymbolAndText(
        GameResult gameResult, string expectedSymbol, string expectedText)
    {
        // Arrange
        var playerName = "TestPlayer";
        var originalBet = new Money(25m);
        var payout = new Money(25m);
        var totalReturn = new Money(50m);

        // Act
        await _consoleUserInterface.ShowPayoutInfoAsync(playerName, originalBet, payout, totalReturn, gameResult);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains(expectedSymbol) && s.Contains(playerName) && s.Contains(expectedText))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Original Bet") && s.Contains("$25.00"))), Times.Once);
    }

    [Fact]
    public async Task ShowPayoutInfoAsync_BlackjackResult_DisplaysBlackjackMessage()
    {
        // Arrange
        var playerName = "TestPlayer";
        var originalBet = new Money(25m);
        var payout = new Money(37.50m);
        var totalReturn = new Money(62.50m);

        // Act
        await _consoleUserInterface.ShowPayoutInfoAsync(playerName, originalBet, payout, totalReturn, GameResult.Blackjack);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("🎰 Blackjack pays 3:2!"))), Times.Once);
    }

    [Fact]
    public async Task ShowPayoutInfoAsync_PushResult_DisplaysReturnedMessage()
    {
        // Arrange
        var playerName = "TestPlayer";
        var originalBet = new Money(25m);
        var payout = new Money(0m);
        var totalReturn = new Money(25m);

        // Act
        await _consoleUserInterface.ShowPayoutInfoAsync(playerName, originalBet, payout, totalReturn, GameResult.Push);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Returned") && s.Contains("bet returned"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Payout"))), Times.Never);
    }

    [Fact]
    public async Task ShowPayoutInfoAsync_LoseResult_DisplaysLostMessage()
    {
        // Arrange
        var playerName = "TestPlayer";
        var originalBet = new Money(25m);
        var payout = new Money(0m);
        var totalReturn = new Money(0m);

        // Act
        await _consoleUserInterface.ShowPayoutInfoAsync(playerName, originalBet, payout, totalReturn, GameResult.Lose);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Lost") && s.Contains("$25.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Payout"))), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ShowPayoutInfoAsync_InvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var originalBet = new Money(25m);
        var payout = new Money(25m);
        var totalReturn = new Money(50m);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consoleUserInterface.ShowPayoutInfoAsync(playerName, originalBet, payout, totalReturn, GameResult.Win));
    }
}