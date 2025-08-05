using System;
using Xunit;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.Exceptions;

/// <summary>
/// Unit tests for betting-related exceptions.
/// Tests exception creation, properties, and recovery guidance.
/// </summary>
public class BettingExceptionTests
{
    private readonly Money _requiredAmount = Money.FromUsd(50.00m);
    private readonly Money _availableAmount = Money.FromUsd(25.00m);
    private readonly Money _minimumBet = Money.FromUsd(5.00m);
    private readonly Money _maximumBet = Money.FromUsd(100.00m);
    private const string PlayerName = "TestPlayer";

    #region InsufficientFundsException Tests

    [Fact]
    public void InsufficientFundsException_WithValidParameters_CreatesException()
    {
        // Arrange & Act
        var exception = new InsufficientFundsException(PlayerName, _requiredAmount, _availableAmount);

        // Assert
        Assert.Equal(PlayerName, exception.PlayerName);
        Assert.Equal(_requiredAmount, exception.RequiredAmount);
        Assert.Equal(_availableAmount, exception.AvailableAmount);
        Assert.Equal(Money.FromUsd(25.00m), exception.Shortfall);
        Assert.Contains("insufficient funds", exception.Message.ToLower());
        Assert.Contains(PlayerName, exception.Message);
        Assert.Contains(_requiredAmount.ToString(), exception.Message);
        Assert.Contains(_availableAmount.ToString(), exception.Message);
    }

    [Fact]
    public void InsufficientFundsException_WithNullPlayerName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new InsufficientFundsException(null!, _requiredAmount, _availableAmount));
    }

    [Fact]
    public void InsufficientFundsException_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        var customMessage = "Custom insufficient funds message";

        // Act
        var exception = new InsufficientFundsException(PlayerName, _requiredAmount, _availableAmount, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(PlayerName, exception.PlayerName);
        Assert.Equal(_requiredAmount, exception.RequiredAmount);
        Assert.Equal(_availableAmount, exception.AvailableAmount);
    }

    [Fact]
    public void InsufficientFundsException_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        var customMessage = "Custom message";

        // Act
        var exception = new InsufficientFundsException(PlayerName, _requiredAmount, _availableAmount, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void InsufficientFundsException_RecoveryGuidance_ProvidesHelpfulGuidance()
    {
        // Arrange
        var exception = new InsufficientFundsException(PlayerName, _requiredAmount, _availableAmount);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("additional", guidance.ToLower());
        Assert.Contains(exception.Shortfall.ToString(), guidance);
        Assert.Contains(_availableAmount.ToString(), guidance);
        Assert.Contains("smaller bet", guidance.ToLower());
    }

    #endregion

    #region InvalidBetException Tests

    [Fact]
    public void InvalidBetException_WithBetBelowMinimum_CreatesException()
    {
        // Arrange
        var lowBet = Money.FromUsd(1.00m);

        // Act
        var exception = new InvalidBetException(lowBet, _minimumBet, _maximumBet);

        // Assert
        Assert.Equal(lowBet, exception.AttemptedBet);
        Assert.Equal(_minimumBet, exception.MinimumBet);
        Assert.Equal(_maximumBet, exception.MaximumBet);
        Assert.Equal(BetViolationType.Amount, exception.ViolationType);
        Assert.Contains("below the minimum", exception.Message.ToLower());
    }

    [Fact]
    public void InvalidBetException_WithBetAboveMaximum_CreatesException()
    {
        // Arrange
        var highBet = Money.FromUsd(200.00m);

        // Act
        var exception = new InvalidBetException(highBet, _minimumBet, _maximumBet);

        // Assert
        Assert.Equal(highBet, exception.AttemptedBet);
        Assert.Equal(_minimumBet, exception.MinimumBet);
        Assert.Equal(_maximumBet, exception.MaximumBet);
        Assert.Equal(BetViolationType.Amount, exception.ViolationType);
        Assert.Contains("exceeds the maximum", exception.Message.ToLower());
    }

    [Fact]
    public void InvalidBetException_WithCustomViolationType_CreatesException()
    {
        // Arrange
        var bet = Money.FromUsd(10.00m);
        var customMessage = "Currency mismatch";

        // Act
        var exception = new InvalidBetException(bet, BetViolationType.Currency, customMessage);

        // Assert
        Assert.Equal(bet, exception.AttemptedBet);
        Assert.Equal(BetViolationType.Currency, exception.ViolationType);
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(Money.Zero, exception.MinimumBet);
        Assert.Equal(Money.Zero, exception.MaximumBet);
    }

    [Fact]
    public void InvalidBetException_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var bet = Money.FromUsd(10.00m);
        var innerException = new ArgumentException("Inner exception");
        var customMessage = "Custom message";

        // Act
        var exception = new InvalidBetException(bet, BetViolationType.Precision, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(BetViolationType.Amount, "at least")]
    [InlineData(BetViolationType.Currency, "correct currency")]
    [InlineData(BetViolationType.Negative, "positive")]
    [InlineData(BetViolationType.Precision, "decimal places")]
    [InlineData(BetViolationType.DuplicateBet, "active bet")]
    [InlineData(BetViolationType.GameState, "betting phase")]
    public void InvalidBetException_RecoveryGuidance_ProvidesAppropriateGuidance(BetViolationType violationType, string expectedContent)
    {
        // Arrange
        var bet = Money.FromUsd(1.00m); // Below minimum for Amount type
        var exception = violationType == BetViolationType.Amount 
            ? new InvalidBetException(bet, _minimumBet, _maximumBet)
            : new InvalidBetException(bet, violationType, "Test message");

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedContent.ToLower(), guidance.ToLower());
    }

    #endregion

    #region BettingPhaseException Tests

    [Fact]
    public void BettingPhaseException_WithValidParameters_CreatesException()
    {
        // Arrange
        var currentPhase = BettingPhase.Closed;
        var requiredPhase = BettingPhase.Accepting;

        // Act
        var exception = new BettingPhaseException(currentPhase, requiredPhase);

        // Assert
        Assert.Equal(currentPhase, exception.CurrentPhase);
        Assert.Equal(requiredPhase, exception.RequiredPhase);
        Assert.Contains("Invalid betting phase", exception.Message);
        Assert.Contains(currentPhase.ToString(), exception.Message);
        Assert.Contains(requiredPhase.ToString(), exception.Message);
    }

    [Fact]
    public void BettingPhaseException_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        var currentPhase = BettingPhase.Processing;
        var requiredPhase = BettingPhase.Accepting;
        var customMessage = "Custom phase error message";

        // Act
        var exception = new BettingPhaseException(currentPhase, requiredPhase, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(currentPhase, exception.CurrentPhase);
        Assert.Equal(requiredPhase, exception.RequiredPhase);
    }

    [Fact]
    public void BettingPhaseException_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var currentPhase = BettingPhase.NotStarted;
        var requiredPhase = BettingPhase.Accepting;
        var innerException = new InvalidOperationException("Inner exception");
        var customMessage = "Custom message";

        // Act
        var exception = new BettingPhaseException(currentPhase, requiredPhase, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(BettingPhase.NotStarted, BettingPhase.Accepting, "wait for the betting phase to begin")]
    [InlineData(BettingPhase.Accepting, BettingPhase.NotStarted, "not available during betting")]
    [InlineData(BettingPhase.Closed, BettingPhase.Accepting, "betting phase has ended")]
    [InlineData(BettingPhase.Processing, BettingPhase.Accepting, "currently being processed")]
    [InlineData(BettingPhase.Processing, BettingPhase.Closed, "wait for bet processing")]
    [InlineData(BettingPhase.Accepting, BettingPhase.Closed, "wait for the betting phase to close")]
    [InlineData(BettingPhase.Accepting, BettingPhase.Processing, "wait for all bets to be placed")]
    public void BettingPhaseException_RecoveryGuidance_ProvidesAppropriateGuidance(
        BettingPhase currentPhase, 
        BettingPhase requiredPhase, 
        string expectedContent)
    {
        // Arrange
        var exception = new BettingPhaseException(currentPhase, requiredPhase);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedContent.ToLower(), guidance.ToLower());
    }

    [Fact]
    public void BettingPhaseException_RecoveryGuidance_WithUnknownPhases_ProvidesGenericGuidance()
    {
        // Arrange
        // Using the same phase for both to trigger the default case
        var exception = new BettingPhaseException(BettingPhase.Accepting, BettingPhase.Accepting);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("appropriate game phase", guidance.ToLower());
    }

    #endregion

    #region BetViolationType Enum Tests

    [Fact]
    public void BetViolationType_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.Amount));
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.Currency));
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.Negative));
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.Precision));
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.DuplicateBet));
        Assert.True(Enum.IsDefined(typeof(BetViolationType), BetViolationType.GameState));
    }

    #endregion

    #region BettingPhase Enum Tests

    [Fact]
    public void BettingPhase_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.True(Enum.IsDefined(typeof(BettingPhase), BettingPhase.NotStarted));
        Assert.True(Enum.IsDefined(typeof(BettingPhase), BettingPhase.Accepting));
        Assert.True(Enum.IsDefined(typeof(BettingPhase), BettingPhase.Closed));
        Assert.True(Enum.IsDefined(typeof(BettingPhase), BettingPhase.Processing));
    }

    #endregion
}