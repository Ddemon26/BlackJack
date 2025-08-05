using GroupProject.Application.Models;
using Xunit;

namespace GroupProject.Tests.Application;

/// <summary>
/// Tests for the GameConfiguration class.
/// </summary>
public class GameConfigurationTests
{
    [Fact]
    public void DefaultConfiguration_IsValid()
    {
        // Arrange & Act
        var config = new GameConfiguration();

        // Assert
        Assert.True(config.IsValid);
        Assert.Empty(config.Validate());
    }

    [Fact]
    public void DefaultConfiguration_HasExpectedValues()
    {
        // Arrange & Act
        var config = new GameConfiguration();

        // Assert
        Assert.Equal(6, config.NumberOfDecks);
        Assert.Equal(4, config.MaxPlayers);
        Assert.Equal(1, config.MinPlayers);
        Assert.True(config.AllowDoubleDown);
        Assert.False(config.AllowSplit);
        Assert.False(config.AllowSurrender);
        Assert.False(config.AllowInsurance);
        Assert.Equal(0.25, config.PenetrationThreshold);
        Assert.Equal(1.5, config.BlackjackPayout);
        Assert.False(config.DealerHitsOnSoft17);
        Assert.Equal(20, config.PlayerNameMaxLength);
    }

    [Theory]
    [InlineData(0, false)] // Too low
    [InlineData(1, true)]  // Valid minimum
    [InlineData(6, true)]  // Valid default
    [InlineData(8, true)]  // Valid maximum
    [InlineData(9, false)] // Too high
    public void NumberOfDecks_Validation(int numberOfDecks, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { NumberOfDecks = numberOfDecks };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0, false)] // Too low
    [InlineData(1, true)]  // Valid minimum
    [InlineData(4, true)]  // Valid default
    [InlineData(7, true)]  // Valid maximum
    [InlineData(8, false)] // Too high
    public void MaxPlayers_Validation(int maxPlayers, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { MaxPlayers = maxPlayers };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0, false)] // Too low
    [InlineData(1, true)]  // Valid minimum
    [InlineData(4, true)]  // Valid (same as default max)
    [InlineData(7, false)] // Valid by range but fails because > MaxPlayers (4)
    [InlineData(8, false)] // Too high
    public void MinPlayers_Validation(int minPlayers, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { MinPlayers = minPlayers };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0.05, false)] // Too low
    [InlineData(0.1, true)]   // Valid minimum
    [InlineData(0.25, true)]  // Valid default
    [InlineData(0.8, true)]   // Valid but close to limit
    [InlineData(0.9, false)]  // Valid by range but fails custom validation for multi-deck
    [InlineData(0.95, false)] // Too high
    public void PenetrationThreshold_Validation(double penetrationThreshold, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { PenetrationThreshold = penetrationThreshold };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0.5, false)] // Too low
    [InlineData(1.0, true)]  // Valid minimum
    [InlineData(1.5, true)]  // Valid default
    [InlineData(2.0, true)]  // Valid maximum
    [InlineData(2.5, false)] // Too high
    public void BlackjackPayout_Validation(double blackjackPayout, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { BlackjackPayout = blackjackPayout };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(2, false)] // Too low
    [InlineData(3, true)]  // Valid minimum
    [InlineData(20, true)] // Valid default
    [InlineData(50, true)] // Valid maximum
    [InlineData(51, false)] // Too high
    public void PlayerNameMaxLength_Validation(int playerNameMaxLength, bool expectedValid)
    {
        // Arrange
        var config = new GameConfiguration { PlayerNameMaxLength = playerNameMaxLength };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void MinPlayersGreaterThanMaxPlayers_IsInvalid()
    {
        // Arrange
        var config = new GameConfiguration 
        { 
            MinPlayers = 5, 
            MaxPlayers = 3 
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.False(config.IsValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Minimum players cannot be greater than maximum players"));
    }

    [Fact]
    public void MinPlayersEqualToMaxPlayers_IsValid()
    {
        // Arrange
        var config = new GameConfiguration 
        { 
            MinPlayers = 7, 
            MaxPlayers = 7 
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void HighPenetrationWithMultipleDecks_GeneratesWarning()
    {
        // Arrange
        var config = new GameConfiguration 
        { 
            NumberOfDecks = 6, 
            PenetrationThreshold = 0.85 
        };

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.False(config.IsValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Penetration threshold should be lower for multi-deck games"));
    }

    [Fact]
    public void Clone_CreatesExactCopy()
    {
        // Arrange
        var original = new GameConfiguration
        {
            NumberOfDecks = 4,
            MaxPlayers = 6,
            MinPlayers = 2,
            AllowDoubleDown = false,
            AllowSplit = true,
            AllowSurrender = true,
            AllowInsurance = true,
            PenetrationThreshold = 0.3,
            BlackjackPayout = 1.2,
            DealerHitsOnSoft17 = true,
            PlayerNameMaxLength = 15
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.NumberOfDecks, clone.NumberOfDecks);
        Assert.Equal(original.MaxPlayers, clone.MaxPlayers);
        Assert.Equal(original.MinPlayers, clone.MinPlayers);
        Assert.Equal(original.AllowDoubleDown, clone.AllowDoubleDown);
        Assert.Equal(original.AllowSplit, clone.AllowSplit);
        Assert.Equal(original.AllowSurrender, clone.AllowSurrender);
        Assert.Equal(original.AllowInsurance, clone.AllowInsurance);
        Assert.Equal(original.PenetrationThreshold, clone.PenetrationThreshold);
        Assert.Equal(original.BlackjackPayout, clone.BlackjackPayout);
        Assert.Equal(original.DealerHitsOnSoft17, clone.DealerHitsOnSoft17);
        Assert.Equal(original.PlayerNameMaxLength, clone.PlayerNameMaxLength);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 4,
            MinPlayers = 2,
            MaxPlayers = 6,
            AllowDoubleDown = true,
            AllowSplit = false,
            PenetrationThreshold = 0.3,
            BlackjackPayout = 1.5
        };

        // Act
        var result = config.ToString();

        // Assert
        Assert.Contains("4 decks", result);
        Assert.Contains("2-6 players", result);
        Assert.Contains("DoubleDown: True", result);
        Assert.Contains("Split: False", result);
        Assert.Contains("30.0%", result);
        Assert.Contains("1.5:1", result);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var config = new GameConfiguration
        {
            NumberOfDecks = 0,      // Invalid
            MaxPlayers = 0,         // Invalid
            MinPlayers = 8,         // Invalid and greater than max
            PenetrationThreshold = 1.5, // Invalid
            BlackjackPayout = 0.5,  // Invalid
            PlayerNameMaxLength = 1 // Invalid
        };

        // Act
        var validationResults = config.Validate().ToList();

        // Assert
        Assert.False(config.IsValid);
        Assert.True(validationResults.Count >= 6); // Should have at least 6 validation errors
        
        // Check that we have errors for each invalid property
        var errorMessages = validationResults.Select(vr => vr.ErrorMessage).ToList();
        Assert.Contains(errorMessages, msg => msg!.Contains("Number of decks"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Maximum players"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Minimum players"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Penetration threshold"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Blackjack payout"));
        Assert.Contains(errorMessages, msg => msg!.Contains("Player name max length"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanProperties_CanBeSetToAnyValue(bool value)
    {
        // Arrange & Act
        var config = new GameConfiguration
        {
            AllowDoubleDown = value,
            AllowSplit = value,
            AllowSurrender = value,
            AllowInsurance = value,
            DealerHitsOnSoft17 = value
        };

        // Assert
        Assert.Equal(value, config.AllowDoubleDown);
        Assert.Equal(value, config.AllowSplit);
        Assert.Equal(value, config.AllowSurrender);
        Assert.Equal(value, config.AllowInsurance);
        Assert.Equal(value, config.DealerHitsOnSoft17);
        Assert.True(config.IsValid); // Boolean values should not affect validity
    }
}