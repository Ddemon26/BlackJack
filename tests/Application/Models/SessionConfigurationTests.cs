using GroupProject.Application.Models;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Application.Models;

/// <summary>
/// Unit tests for the SessionConfiguration model.
/// </summary>
public class SessionConfigurationTests
{
    private readonly List<string> _defaultPlayerNames;
    private readonly Money _defaultBankroll;
    private readonly Money _defaultMinBet;
    private readonly Money _defaultMaxBet;
    private readonly GameConfiguration _defaultGameRules;

    public SessionConfigurationTests()
    {
        _defaultPlayerNames = new List<string> { "Alice", "Bob", "Charlie" };
        _defaultBankroll = new Money(1000m);
        _defaultMinBet = new Money(10m);
        _defaultMaxBet = new Money(500m);
        _defaultGameRules = new GameConfiguration
        {
            NumberOfDecks = 6,
            AllowDoubleDown = true,
            AllowSplit = true
        };
    }

    [Fact]
    public void DefaultConstructor_CreatesValidConfiguration()
    {
        // Act
        var config = new SessionConfiguration();

        // Assert
        Assert.NotNull(config.PlayerNames);
        Assert.Empty(config.PlayerNames);
        Assert.Equal(new Money(1000m), config.DefaultBankroll);
        Assert.Equal(new Money(10m), config.MinimumBet);
        Assert.Equal(new Money(500m), config.MaximumBet);
        Assert.NotNull(config.GameRules);
        Assert.True(config.EnableStatistics);
        Assert.True(config.AllowRebuy);
        Assert.Equal(new Money(1000m), config.MaxRebuyAmount);
        Assert.Equal(3, config.MaxRebuysPerSession);
        Assert.Equal(120, config.SessionTimeoutMinutes);
        Assert.Equal(TimeSpan.FromMinutes(5), config.AutoSaveInterval);
    }

    [Fact]
    public void ParameterizedConstructor_WithValidParameters_CreatesConfiguration()
    {
        // Act
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Assert
        Assert.Equal(3, config.PlayerNames.Count);
        Assert.Contains("Alice", config.PlayerNames);
        Assert.Contains("Bob", config.PlayerNames);
        Assert.Contains("Charlie", config.PlayerNames);
        Assert.Equal(_defaultBankroll, config.DefaultBankroll);
        Assert.Equal(_defaultMinBet, config.MinimumBet);
        Assert.Equal(_defaultMaxBet, config.MaximumBet);
        Assert.Equal(_defaultGameRules, config.GameRules);
    }

    [Fact]
    public void ParameterizedConstructor_WithNullPlayerNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionConfiguration(null!, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules));
    }

    [Fact]
    public void ParameterizedConstructor_WithEmptyPlayerNames_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SessionConfiguration(new List<string>(), _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules));
    }

    [Fact]
    public void ParameterizedConstructor_WithNullGameRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, null!));
    }

    [Fact]
    public void ParameterizedConstructor_WithNegativeBankroll_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var negativeBankroll = new Money(-100m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new SessionConfiguration(_defaultPlayerNames, negativeBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules));
    }

    [Fact]
    public void ParameterizedConstructor_WithInvalidBetAmounts_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var zeroBet = Money.Zero;
        var invalidMaxBet = new Money(5m); // Less than min bet

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, zeroBet, _defaultMaxBet, _defaultGameRules));

        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, invalidMaxBet, _defaultGameRules));
    }

    [Fact]
    public void ParameterizedConstructor_WithDuplicatePlayerNames_RemovesDuplicates()
    {
        // Arrange
        var duplicateNames = new List<string> { "Alice", "Bob", "alice", "Charlie", "BOB" };

        // Act
        var config = new SessionConfiguration(duplicateNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Assert
        Assert.Equal(3, config.PlayerNames.Count);
        Assert.Contains("Alice", config.PlayerNames);
        Assert.Contains("Bob", config.PlayerNames);
        Assert.Contains("Charlie", config.PlayerNames);
    }

    [Fact]
    public void GetMinimumBetForPlayer_WithoutCustomLimits_ReturnsDefaultMinimum()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act
        var minBet = config.GetMinimumBetForPlayer("Alice");

        // Assert
        Assert.Equal(_defaultMinBet, minBet);
    }

    [Fact]
    public void GetMaximumBetForPlayer_WithoutCustomLimits_ReturnsDefaultMaximum()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act
        var maxBet = config.GetMaximumBetForPlayer("Alice");

        // Assert
        Assert.Equal(_defaultMaxBet, maxBet);
    }

    [Fact]
    public void SetPlayerBettingLimits_WithValidLimits_SetsCustomLimits()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);
        var customMin = new Money(20m);
        var customMax = new Money(200m);

        // Act
        config.SetPlayerBettingLimits("Alice", customMin, customMax);

        // Assert
        Assert.Equal(customMin, config.GetMinimumBetForPlayer("Alice"));
        Assert.Equal(customMax, config.GetMaximumBetForPlayer("Alice"));
        Assert.Equal(_defaultMinBet, config.GetMinimumBetForPlayer("Bob")); // Other players unchanged
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPlayerBettingLimits_WithInvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            config.SetPlayerBettingLimits(playerName, new Money(20m), new Money(200m)));
    }

    [Fact]
    public void SetPlayerBettingLimits_WithInvalidAmounts_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            config.SetPlayerBettingLimits("Alice", Money.Zero, new Money(200m)));

        Assert.Throws<ArgumentOutOfRangeException>(() => 
            config.SetPlayerBettingLimits("Alice", new Money(200m), new Money(100m))); // Max < Min
    }

    [Fact]
    public void RemovePlayerBettingLimits_WithExistingLimits_RemovesCustomLimits()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);
        config.SetPlayerBettingLimits("Alice", new Money(20m), new Money(200m));

        // Act
        config.RemovePlayerBettingLimits("Alice");

        // Assert
        Assert.Equal(_defaultMinBet, config.GetMinimumBetForPlayer("Alice"));
        Assert.Equal(_defaultMaxBet, config.GetMaximumBetForPlayer("Alice"));
    }

    [Fact]
    public void RemovePlayerBettingLimits_WithNonExistentLimits_DoesNotThrow()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert (should not throw)
        config.RemovePlayerBettingLimits("Alice");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemovePlayerBettingLimits_WithInvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.RemovePlayerBettingLimits(playerName));
    }

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act
        var validationResults = config.Validate();

        // Assert
        Assert.Empty(validationResults);
        Assert.True(config.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyPlayerNames_ReturnsValidationError()
    {
        // Arrange
        var config = new SessionConfiguration();

        // Act
        var validationResults = config.Validate().ToList();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.False(config.IsValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("At least one player name"));
    }

    [Fact]
    public void Validate_WithInvalidBetAmounts_ReturnsValidationErrors()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);
        config.MinimumBet = new Money(600m); // Greater than max bet
        config.MaximumBet = new Money(500m);

        // Act
        var validationResults = config.Validate().ToList();

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.False(config.IsValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage!.Contains("Maximum bet must be greater than minimum bet"));
    }

    [Fact]
    public void Clone_CreatesIdenticalCopy()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules)
        {
            SessionName = "Test Session",
            EnableStatistics = false,
            AllowRebuy = false
        };
        config.SetPlayerBettingLimits("Alice", new Money(20m), new Money(200m));

        // Act
        var clone = config.Clone();

        // Assert
        Assert.Equal(config.PlayerNames, clone.PlayerNames);
        Assert.Equal(config.DefaultBankroll, clone.DefaultBankroll);
        Assert.Equal(config.MinimumBet, clone.MinimumBet);
        Assert.Equal(config.MaximumBet, clone.MaximumBet);
        Assert.Equal(config.SessionName, clone.SessionName);
        Assert.Equal(config.EnableStatistics, clone.EnableStatistics);
        Assert.Equal(config.AllowRebuy, clone.AllowRebuy);
        Assert.Equal(config.GetMinimumBetForPlayer("Alice"), clone.GetMinimumBetForPlayer("Alice"));
        Assert.Equal(config.GetMaximumBetForPlayer("Alice"), clone.GetMaximumBetForPlayer("Alice"));
        
        // Ensure it's a deep copy
        Assert.NotSame(config, clone);
        Assert.NotSame(config.PlayerNames, clone.PlayerNames);
        Assert.NotSame(config.GameRules, clone.GameRules);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules)
        {
            SessionName = "Test Session"
        };

        // Act
        var result = config.ToString();

        // Assert
        Assert.Contains("Test Session", result);
        Assert.Contains("3 players", result);
        Assert.Contains(_defaultBankroll.ToString(), result);
        Assert.Contains(_defaultMinBet.ToString(), result);
        Assert.Contains(_defaultMaxBet.ToString(), result);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var config1 = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);
        var config2 = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.True(config1.Equals(config2));
        Assert.Equal(config1.GetHashCode(), config2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var config1 = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);
        var config2 = new SessionConfiguration(new List<string> { "Different" }, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.False(config1.Equals(config2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.False(config.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var config = new SessionConfiguration(_defaultPlayerNames, _defaultBankroll, _defaultMinBet, _defaultMaxBet, _defaultGameRules);

        // Act & Assert
        Assert.False(config.Equals("not a config"));
    }
}