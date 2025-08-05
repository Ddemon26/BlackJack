using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Presentation.Console;
using Moq;
using Xunit;

namespace GroupProject.Tests.Presentation;

/// <summary>
/// Unit tests for the ConsoleUserInterface class.
/// </summary>
public class ConsoleUserInterfaceTests
{
    private readonly Mock<IInputProvider> _mockInputProvider;
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly ConsoleUserInterface _userInterface;

    public ConsoleUserInterfaceTests()
    {
        _mockInputProvider = new Mock<IInputProvider>();
        _mockOutputProvider = new Mock<IOutputProvider>();
        _userInterface = new ConsoleUserInterface(_mockInputProvider.Object, _mockOutputProvider.Object);
    }

    [Fact]
    public void Constructor_WithNullInputProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsoleUserInterface(null!, _mockOutputProvider.Object));
    }

    [Fact]
    public void Constructor_WithNullOutputProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsoleUserInterface(_mockInputProvider.Object, null!));
    }

    [Fact]
    public async Task ShowWelcomeMessageAsync_CallsOutputProviderCorrectly()
    {
        // Act
        await _userInterface.ShowWelcomeMessageAsync();

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.IsAny<string>()), Times.AtLeast(8));
        _mockOutputProvider.Verify(x => x.WriteLineAsync(null), Times.AtLeast(2));
    }

    [Fact]
    public async Task ShowErrorMessageAsync_WithValidMessage_DisplaysFormattedError()
    {
        // Arrange
        const string errorMessage = "Test error message";

        // Act
        await _userInterface.ShowErrorMessageAsync(errorMessage);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync($"❌ ERROR: {errorMessage}"), Times.Once);
    }

    [Fact]
    public async Task ShowErrorMessageAsync_WithNullMessage_DoesNotCallOutputProvider()
    {
        // Act
        await _userInterface.ShowErrorMessageAsync(null!);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ShowMessageAsync_WithValidMessage_DisplaysFormattedMessage()
    {
        // Arrange
        const string message = "Test message";

        // Act
        await _userInterface.ShowMessageAsync(message);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync($"ℹ️  {message}"), Times.Once);
    }

    [Fact]
    public async Task ClearDisplayAsync_CallsOutputProviderClear()
    {
        // Act
        await _userInterface.ClearDisplayAsync();

        // Assert
        _mockOutputProvider.Verify(x => x.ClearAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPlayerCountAsync_CallsInputProviderCorrectly()
    {
        // Arrange
        const int expectedCount = 3;
        _mockInputProvider.Setup(x => x.GetIntegerInputAsync(It.IsAny<string>(), 1, 4))
                         .ReturnsAsync(expectedCount);

        // Act
        var result = await _userInterface.GetPlayerCountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
        _mockInputProvider.Verify(x => x.GetIntegerInputAsync("How many players will be playing?", 1, 4), Times.Once);
    }

    [Fact]
    public async Task GetPlayerActionAsync_WithValidParameters_CallsInputProviderCorrectly()
    {
        // Arrange
        const string playerName = "TestPlayer";
        var validActions = new[] { PlayerAction.Hit, PlayerAction.Stand };
        const PlayerAction expectedAction = PlayerAction.Hit;

        _mockInputProvider.Setup(x => x.GetPlayerActionAsync(playerName, validActions))
                         .ReturnsAsync(expectedAction);

        // Act
        var result = await _userInterface.GetPlayerActionAsync(playerName, validActions);

        // Assert
        Assert.Equal(expectedAction, result);
        _mockInputProvider.Verify(x => x.GetPlayerActionAsync(playerName, validActions), Times.Once);
    }

    [Fact]
    public async Task GetPlayerActionAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Arrange
        var validActions = new[] { PlayerAction.Hit, PlayerAction.Stand };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _userInterface.GetPlayerActionAsync(null!, validActions));
    }

    [Fact]
    public async Task GetPlayerActionAsync_WithEmptyValidActions_ThrowsArgumentException()
    {
        // Arrange
        const string playerName = "TestPlayer";
        var emptyActions = Array.Empty<PlayerAction>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _userInterface.GetPlayerActionAsync(playerName, emptyActions));
    }

    [Fact]
    public async Task ShouldPlayAnotherRoundAsync_CallsInputProviderCorrectly()
    {
        // Arrange
        const bool expectedResult = true;
        _mockInputProvider.Setup(x => x.GetConfirmationAsync("Would you like to play another round?"))
                         .ReturnsAsync(expectedResult);

        // Act
        var result = await _userInterface.ShouldPlayAnotherRoundAsync();

        // Assert
        Assert.Equal(expectedResult, result);
        _mockInputProvider.Verify(x => x.GetConfirmationAsync("Would you like to play another round?"), Times.Once);
    }

    [Fact]
    public async Task ShowPlayerHandAsync_WithNullPlayer_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _userInterface.ShowPlayerHandAsync(null!));
    }

    [Fact]
    public async Task ShowPlayerHandAsync_WithValidPlayer_DisplaysPlayerInformation()
    {
        // Arrange
        var player = new Player("TestPlayer", PlayerType.Human);
        player.AddCard(new Card(Suit.Hearts, Rank.Ace));
        player.AddCard(new Card(Suit.Spades, Rank.King));

        // Act
        await _userInterface.ShowPlayerHandAsync(player);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync("PLAYER: TestPlayer"), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(null), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteAsync("  Cards: "), Times.Once);
    }

    [Fact]
    public async Task ShowGameStateAsync_WithNullGameState_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _userInterface.ShowGameStateAsync(null!));
    }

    [Fact]
    public async Task ShowGameResultsAsync_WithNullResults_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _userInterface.ShowGameResultsAsync(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPlayerNamesAsync_WithInvalidCount_ThrowsArgumentException(int count)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _userInterface.GetPlayerNamesAsync(count));
    }
}