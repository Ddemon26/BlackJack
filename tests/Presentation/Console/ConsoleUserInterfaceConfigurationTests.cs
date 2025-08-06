using System;
using System.Threading.Tasks;
using GroupProject.Application.Models;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Presentation.Console;
using Moq;
using Xunit;

namespace GroupProject.Tests.Presentation.Console;

/// <summary>
/// Unit tests for configuration interface components in ConsoleUserInterface.
/// Tests configuration menu display, settings modification, and validation.
/// </summary>
public class ConsoleUserInterfaceConfigurationTests
{
    private readonly Mock<IInputProvider> _mockInputProvider;
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly ConsoleUserInterface _consoleUserInterface;

    public ConsoleUserInterfaceConfigurationTests()
    {
        _mockInputProvider = new Mock<IInputProvider>();
        _mockOutputProvider = new Mock<IOutputProvider>();
        _consoleUserInterface = new ConsoleUserInterface(_mockInputProvider.Object, _mockOutputProvider.Object);
    }

    [Fact]
    public async Task ShowCurrentConfigurationAsync_ValidConfig_DisplaysAllSettings()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 6,
            MinPlayers = 1,
            MaxPlayers = 4,
            AllowDoubleDown = true,
            AllowSplit = true,
            MinimumBet = new Money(10m),
            MaximumBet = new Money(500m),
            DefaultBankroll = new Money(1000m)
        };

        // Act
        await _consoleUserInterface.ShowCurrentConfigurationAsync(config);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("CURRENT CONFIGURATION"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("GAME RULES:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("BETTING SETTINGS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("DISPLAY SETTINGS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("ADVANCED SETTINGS:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Decks in Shoe: 6"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("$10.00"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ShowCurrentConfigurationAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowCurrentConfigurationAsync(null));
    }

    [Fact]
    public async Task ShowConfigurationMenuAsync_SaveAndExit_ReturnsUpdatedConfig()
    {
        // Arrange
        var originalConfig = new GameConfiguration();
        
        // Setup mock to simulate user selecting "Save and Exit" (option 6)
        _mockInputProvider.Setup(x => x.GetIntegerInputAsync("Select an option", 1, 7))
            .ReturnsAsync(6);

        // Act
        var result = await _consoleUserInterface.ShowConfigurationMenuAsync(originalConfig);

        // Assert
        Assert.NotNull(result);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("CONFIGURATION MENU:"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Configuration saved successfully"))), Times.Once);
    }

    [Fact]
    public async Task ShowConfigurationMenuAsync_Cancel_ReturnsNull()
    {
        // Arrange
        var originalConfig = new GameConfiguration();
        
        // Setup mock to simulate user selecting "Cancel" (option 7) and confirming
        _mockInputProvider.SetupSequence(x => x.GetIntegerInputAsync("Select an option", 1, 7))
            .ReturnsAsync(7);
        _mockInputProvider.Setup(x => x.GetConfirmationAsync("Are you sure you want to discard all changes?"))
            .ReturnsAsync(true);

        // Act
        var result = await _consoleUserInterface.ShowConfigurationMenuAsync(originalConfig);

        // Assert
        Assert.Null(result);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Configuration changes discarded"))), Times.Once);
    }

    [Fact]
    public async Task ShowConfigurationMenuAsync_ResetToDefaults_ResetsConfiguration()
    {
        // Arrange
        var originalConfig = new GameConfiguration
        {
            NumberOfDecks = 8,
            MinimumBet = new Money(25m)
        };
        
        // Setup mock to simulate user selecting "Reset to Defaults" (option 5) then "Save and Exit" (option 6)
        _mockInputProvider.SetupSequence(x => x.GetIntegerInputAsync("Select an option", 1, 7))
            .ReturnsAsync(5)
            .ReturnsAsync(6);

        // Act
        var result = await _consoleUserInterface.ShowConfigurationMenuAsync(originalConfig);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.NumberOfDecks); // Default value
        Assert.Equal(new Money(5m), result.MinimumBet); // Default value
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Configuration reset to defaults"))), Times.Once);
    }

    [Fact]
    public async Task ShowConfigurationMenuAsync_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consoleUserInterface.ShowConfigurationMenuAsync(null));
    }

    [Fact]
    public async Task ShowConfigurationMenuAsync_InvalidConfiguration_ShowsErrors()
    {
        // Arrange
        var invalidConfig = new GameConfiguration
        {
            MinimumBet = new Money(100m),
            MaximumBet = new Money(50m) // Invalid: min > max
        };
        
        // Setup mock to simulate user trying to save invalid config, then canceling
        _mockInputProvider.SetupSequence(x => x.GetIntegerInputAsync("Select an option", 1, 7))
            .ReturnsAsync(6) // Try to save
            .ReturnsAsync(7); // Cancel
        _mockInputProvider.Setup(x => x.GetConfirmationAsync("Are you sure you want to discard all changes?"))
            .ReturnsAsync(true);

        // Act
        var result = await _consoleUserInterface.ShowConfigurationMenuAsync(invalidConfig);

        // Assert
        Assert.Null(result);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("CONFIGURATION ERRORS:"))), Times.Once);
    }

    [Theory]
    [InlineData(CardDisplayFormat.Symbols, "Symbols")]
    [InlineData(CardDisplayFormat.Text, "Text")]
    [InlineData(CardDisplayFormat.Abbreviated, "Abbreviated")]
    public async Task ShowCurrentConfigurationAsync_DifferentCardFormats_DisplaysCorrectFormat(
        CardDisplayFormat format, string expectedText)
    {
        // Arrange
        var config = new GameConfiguration
        {
            CardDisplayFormat = format
        };

        // Act
        await _consoleUserInterface.ShowCurrentConfigurationAsync(config);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains($"Card Format: {expectedText}"))), Times.Once);
    }

    [Fact]
    public async Task ShowCurrentConfigurationAsync_BooleanSettings_DisplaysCorrectStatus()
    {
        // Arrange
        var config = new GameConfiguration
        {
            AllowDoubleDown = true,
            AllowSplit = false,
            AutoReshuffleEnabled = true,
            ShowDetailedStatistics = false
        };

        // Act
        await _consoleUserInterface.ShowCurrentConfigurationAsync(config);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Double Down: Enabled"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Split Pairs: Disabled"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Auto Reshuffle: Enabled"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("Detailed Statistics: Disabled"))), Times.Once);
    }

    [Fact]
    public async Task ShowCurrentConfigurationAsync_MoneyValues_DisplaysFormattedAmounts()
    {
        // Arrange
        var config = new GameConfiguration
        {
            MinimumBet = new Money(5.50m),
            MaximumBet = new Money(1000m),
            DefaultBankroll = new Money(2500.75m)
        };

        // Act
        await _consoleUserInterface.ShowCurrentConfigurationAsync(config);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("$5.50"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("$1000.00"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("$2500.75"))), Times.Once);
    }

    [Fact]
    public async Task ShowCurrentConfigurationAsync_PercentageValues_DisplaysFormattedPercentages()
    {
        // Arrange
        var config = new GameConfiguration
        {
            PenetrationThreshold = 0.25,
            BlackjackPayout = 1.5
        };

        // Act
        await _consoleUserInterface.ShowCurrentConfigurationAsync(config);

        // Assert
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("25.0%"))), Times.Once);
        _mockOutputProvider.Verify(x => x.WriteLineAsync(It.Is<string>(s => 
            s != null && s.Contains("1.5:1"))), Times.Once);
    }
}