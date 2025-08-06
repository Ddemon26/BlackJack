using System;
using Xunit;
using GroupProject.Domain.Exceptions;

namespace GroupProject.Tests.Domain.Exceptions;

public class SplitHandLimitExceptionTests
{
    private const string TestPlayerName = "TestPlayer";

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var exception = new SplitHandLimitException(TestPlayerName, 2, 3);

        // Assert
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(2, exception.CurrentSplits);
        Assert.Equal(3, exception.MaximumSplits);
        Assert.Equal(3, exception.CurrentHandCount); // CurrentSplits + 1
        Assert.Equal(4, exception.MaximumHandCount); // MaximumSplits + 1
        Assert.Contains("split limit", exception.Message);
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPlayerName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SplitHandLimitException(null!, 1, 2));
    }

    [Theory]
    [InlineData(0, 0, "Splitting is not allowed in this game")]
    [InlineData(1, 1, "You can only split once per round")]
    [InlineData(3, 3, "You have reached the maximum of 3 splits")]
    public void RecoveryGuidance_ForDifferentLimits_ProvidesAppropriateGuidance(int currentSplits, int maxSplits, string expectedGuidanceFragment)
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, currentSplits, maxSplits);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedGuidanceFragment, guidance);
    }

    [Fact]
    public void RecoveryGuidance_WithZeroMaxSplits_IndicatesSplittingDisabled()
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, 0, 0);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("Splitting is not allowed", guidance);
        Assert.Contains("Choose Hit, Stand, or Double Down", guidance);
    }

    [Fact]
    public void RecoveryGuidance_WithOneSplit_IndicatesOncePerRound()
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, 1, 1);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("You can only split once per round", guidance);
        Assert.Contains("You already have 2 hands", guidance);
        Assert.Contains("Continue playing your existing hands", guidance);
    }

    [Fact]
    public void RecoveryGuidance_WithMultipleSplits_ShowsCorrectCounts()
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, 2, 3);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("maximum of 3 splits", guidance);
        Assert.Contains("4 hands total", guidance);
        Assert.Contains("Continue playing your existing hands", guidance);
    }

    [Fact]
    public void Constructor_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        const string customMessage = "Custom error message";

        // Act
        var exception = new SplitHandLimitException(TestPlayerName, 1, 2, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(1, exception.CurrentSplits);
        Assert.Equal(2, exception.MaximumSplits);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        const string customMessage = "Custom error message";

        // Act
        var exception = new SplitHandLimitException(TestPlayerName, 1, 2, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(0, 0, "splitting is disabled")]
    [InlineData(1, 1, "Current hands: 2, Maximum allowed: 2")]
    [InlineData(2, 3, "Current hands: 3, Maximum allowed: 4")]
    public void CreateMessage_ForDifferentScenarios_GeneratesAppropriateMessage(int currentSplits, int maxSplits, string expectedMessageFragment)
    {
        // Arrange & Act
        var exception = new SplitHandLimitException(TestPlayerName, currentSplits, maxSplits);

        // Assert
        Assert.Contains(expectedMessageFragment, exception.Message);
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void CurrentHandCount_CalculatesCorrectly()
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, 2, 3);

        // Act & Assert
        Assert.Equal(3, exception.CurrentHandCount); // 2 splits + 1 original hand
    }

    [Fact]
    public void MaximumHandCount_CalculatesCorrectly()
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, 2, 3);

        // Act & Assert
        Assert.Equal(4, exception.MaximumHandCount); // 3 max splits + 1 original hand
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public void HandCounts_AreConsistentWithSplitCounts(int splits, int expectedHandCount)
    {
        // Arrange
        var exception = new SplitHandLimitException(TestPlayerName, splits, splits + 1);

        // Act & Assert
        Assert.Equal(expectedHandCount, exception.CurrentHandCount);
        Assert.Equal(expectedHandCount + 1, exception.MaximumHandCount);
    }
}