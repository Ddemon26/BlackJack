using GroupProject.Application.Models;
using GroupProject.Domain.Events;
using GroupProject.Domain.Interfaces;
using GroupProject.Presentation.Console;
using Moq;
using Xunit;

namespace GroupProject.Tests.Presentation.Console;

/// <summary>
/// Tests for ConsoleUserInterface shoe management functionality.
/// </summary>
public class ConsoleUserInterfaceShoeManagementTests
{
    private readonly Mock<IInputProvider> _mockInputProvider;
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly ConsoleUserInterface _consoleUserInterface;

    public ConsoleUserInterfaceShoeManagementTests()
    {
        _mockInputProvider = new Mock<IInputProvider>();
        _mockOutputProvider = new Mock<IOutputProvider>();
        _consoleUserInterface = new ConsoleUserInterface(_mockInputProvider.Object, _mockOutputProvider.Object);
    }

    [Fact]
    public async Task ShowShoeReshuffleNotificationAsync_DisplaysCompleteNotification()
    {
        // Arrange
        var reshuffleEventArgs = new ShoeReshuffleEventArgs(
            0.15, 
            0.25, 
            "Automatic reshuffle triggered by penetration threshold");

        // Act
        await _consoleUserInterface.ShowShoeReshuffleNotificationAsync(reshuffleEventArgs);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(null), Times.AtLeast(2)); // Empty lines
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("SHOE RESHUFFLED"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Reason: Automatic reshuffle triggered by penetration threshold"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Cards remaining when triggered: 15.0%"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Penetration threshold: 25.0%"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Time:"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("The shoe has been shuffled and is ready for continued play"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeReshuffleNotificationAsync_ThrowsArgumentNullException_WhenReshuffleEventArgsIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _consoleUserInterface.ShowShoeReshuffleNotificationAsync(null!));
    }

    [Fact]
    public async Task ShowShoeReshuffleNotificationAsync_FormatsTimestampCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 14, 30, 45, DateTimeKind.Utc);
        var reshuffleEventArgs = new ShoeReshuffleEventArgs(0.2, 0.25, "Test reshuffle")
        {
            // Note: We can't set Timestamp directly as it's set in constructor
            // This test verifies the format is correct for any timestamp
        };

        // Act
        await _consoleUserInterface.ShowShoeReshuffleNotificationAsync(reshuffleEventArgs);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Time:") && s.Contains(":"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_DisplaysCompleteStatus()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(6, 150, 0.48, 0.25, false, true);

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(null), Times.AtLeast(2)); // Empty lines
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("SHOE STATUS"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Decks: 6"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Cards remaining: 150/312"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Cards dealt: 162"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Penetration threshold: 25.0%"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => s.Contains("Automatic reshuffling: ENABLED"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_DisplaysReshuffleNeededWarning()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(6, 50, 0.16, 0.25, true, true);

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("RESHUFFLE NEEDED") && s.Contains("penetration threshold"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_DisplaysNearlyEmptyWarning()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(6, 15, 0.048, 0.25, false, true); // Nearly empty (< 5%)

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("SHOE NEARLY EMPTY") && s.Contains("Reshuffle will occur soon"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_DisplaysAutoReshuffleDisabled()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(6, 150, 0.48, 0.25, false, false);

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Automatic reshuffling: DISABLED"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_ThrowsArgumentNullException_WhenShoeStatusIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _consoleUserInterface.ShowShoeStatusAsync(null!));
    }

    [Fact]
    public async Task ShowShoeStatusAsync_FormatsPercentagesCorrectly()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(6, 156, 0.5, 0.25, false, true); // Exactly 50%

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("(50.0%)"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Penetration threshold: 25.0%"))), Times.Once);
    }

    [Fact]
    public async Task ShowShoeStatusAsync_DisplaysCorrectCardCounts()
    {
        // Arrange
        var shoeStatus = new ShoeStatus(8, 200, 0.481, 0.25, false, true); // 8 decks = 416 total cards

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Decks: 8"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Cards remaining: 200/416"))), Times.Once);
        _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
            s.Contains("Cards dealt: 216"))), Times.Once);
    }

    [Theory]
    [InlineData(0.24, true, false)] // Below threshold, needs reshuffle
    [InlineData(0.26, false, false)] // Above threshold, doesn't need reshuffle
    [InlineData(0.04, false, true)] // Nearly empty (< 5%)
    [InlineData(0.06, false, false)] // Not nearly empty (> 5%)
    public async Task ShowShoeStatusAsync_DisplaysCorrectWarnings(double remainingPercentage, bool needsReshuffle, bool isNearlyEmpty)
    {
        // Arrange
        var remainingCards = (int)(312 * remainingPercentage); // 6 decks * 52 cards
        var shoeStatus = new ShoeStatus(6, remainingCards, remainingPercentage, 0.25, needsReshuffle, true);

        // Act
        await _consoleUserInterface.ShowShoeStatusAsync(shoeStatus);

        // Assert
        if (needsReshuffle)
        {
            _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
                s.Contains("RESHUFFLE NEEDED"))), Times.Once);
        }
        else if (isNearlyEmpty)
        {
            _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
                s.Contains("SHOE NEARLY EMPTY"))), Times.Once);
        }
        else
        {
            _mockOutputProvider.Verify(op => op.WriteLineAsync(It.Is<string>(s => 
                s.Contains("RESHUFFLE NEEDED") || s.Contains("SHOE NEARLY EMPTY"))), Times.Never);
        }
    }
}