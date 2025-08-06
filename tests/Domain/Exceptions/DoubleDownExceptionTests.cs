using System;
using Xunit;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.Exceptions;

public class DoubleDownExceptionTests
{
    private readonly Hand _testHand;
    private const string TestPlayerName = "TestPlayer";

    public DoubleDownExceptionTests()
    {
        // Create a test hand with two cards
        _testHand = new Hand();
        _testHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        _testHand.AddCard(new Card(Suit.Spades, Rank.Ace));
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, DoubleDownInvalidReason.WrongCardCount);

        // Assert
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(_testHand, exception.AttemptedHand);
        Assert.Equal(DoubleDownInvalidReason.WrongCardCount, exception.Reason);
        Assert.Null(exception.RequiredAmount);
        Assert.Null(exception.AvailableAmount);
        Assert.Null(exception.Shortfall);
        Assert.Contains("cannot double down", exception.Message);
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void Constructor_WithInsufficientFunds_SetsMoneyPropertiesCorrectly()
    {
        // Arrange
        var requiredAmount = new Money(100m);
        var availableAmount = new Money(50m);

        // Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, requiredAmount, availableAmount);

        // Assert
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(_testHand, exception.AttemptedHand);
        Assert.Equal(DoubleDownInvalidReason.InsufficientFunds, exception.Reason);
        Assert.Equal(requiredAmount, exception.RequiredAmount);
        Assert.Equal(availableAmount, exception.AvailableAmount);
        Assert.Equal(new Money(50m), exception.Shortfall);
        Assert.Contains("cannot double down", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPlayerName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DoubleDownException(null!, _testHand, DoubleDownInvalidReason.WrongCardCount));
    }

    [Fact]
    public void Constructor_WithNullHand_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DoubleDownException(TestPlayerName, null!, DoubleDownInvalidReason.WrongCardCount));
    }

    [Theory]
    [InlineData(DoubleDownInvalidReason.WrongCardCount, "You can only double down on your initial two-card hand")]
    [InlineData(DoubleDownInvalidReason.InsufficientFunds, "You don't have enough funds")]
    [InlineData(DoubleDownInvalidReason.HandComplete, "This hand is already complete")]
    [InlineData(DoubleDownInvalidReason.SplitHand, "Double down is not allowed on split hands")]
    [InlineData(DoubleDownInvalidReason.GameState, "Double down is not allowed at this time")]
    [InlineData(DoubleDownInvalidReason.RuleDisabled, "Double down is disabled")]
    [InlineData(DoubleDownInvalidReason.AlreadyDoubled, "You have already doubled down")]
    [InlineData(DoubleDownInvalidReason.HandValue, "Double down is only allowed on certain hand values")]
    public void RecoveryGuidance_ForDifferentReasons_ProvidesAppropriateGuidance(DoubleDownInvalidReason reason, string expectedGuidanceFragment)
    {
        // Arrange
        var exception = new DoubleDownException(TestPlayerName, _testHand, reason);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedGuidanceFragment, guidance);
    }

    [Fact]
    public void RecoveryGuidance_WithInsufficientFundsAndShortfall_IncludesShortfallAmount()
    {
        // Arrange
        var requiredAmount = new Money(100m);
        var availableAmount = new Money(30m);
        var exception = new DoubleDownException(TestPlayerName, _testHand, requiredAmount, availableAmount);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("additional 70.00 USD", guidance);
        Assert.Contains("Consider hitting or standing", guidance);
    }

    [Fact]
    public void RecoveryGuidance_WithInsufficientFundsButNoShortfall_ProvidesGenericGuidance()
    {
        // Arrange
        var exception = new DoubleDownException(TestPlayerName, _testHand, DoubleDownInvalidReason.InsufficientFunds);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("You don't have enough funds", guidance);
        Assert.Contains("Consider hitting or standing", guidance);
    }

    [Fact]
    public void Constructor_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        const string customMessage = "Custom error message";

        // Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, DoubleDownInvalidReason.WrongCardCount, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(_testHand, exception.AttemptedHand);
        Assert.Equal(DoubleDownInvalidReason.WrongCardCount, exception.Reason);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        const string customMessage = "Custom error message";

        // Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, DoubleDownInvalidReason.WrongCardCount, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(DoubleDownInvalidReason.WrongCardCount, "only initial two-card hands allowed")]
    [InlineData(DoubleDownInvalidReason.InsufficientFunds, "insufficient funds")]
    [InlineData(DoubleDownInvalidReason.HandComplete, "completed")]
    [InlineData(DoubleDownInvalidReason.SplitHand, "split")]
    [InlineData(DoubleDownInvalidReason.GameState, "invalid game state")]
    [InlineData(DoubleDownInvalidReason.RuleDisabled, "double down is disabled")]
    [InlineData(DoubleDownInvalidReason.AlreadyDoubled, "already doubled down")]
    [InlineData(DoubleDownInvalidReason.HandValue, "hand value not eligible")]
    public void CreateMessage_ForDifferentReasons_GeneratesAppropriateMessage(DoubleDownInvalidReason reason, string expectedMessageFragment)
    {
        // Arrange & Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, reason);

        // Assert
        Assert.Contains(expectedMessageFragment, exception.Message.ToLowerInvariant());
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void CreateMessage_WithThreeCardHand_IndicatesCardCount()
    {
        // Arrange
        var threeCardHand = new Hand();
        threeCardHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        threeCardHand.AddCard(new Card(Suit.Spades, Rank.Seven));
        threeCardHand.AddCard(new Card(Suit.Clubs, Rank.Six));

        // Act
        var exception = new DoubleDownException(TestPlayerName, threeCardHand, DoubleDownInvalidReason.WrongCardCount);

        // Assert
        Assert.Contains("3-card hand", exception.Message);
    }

    [Fact]
    public void CreateInsufficientFundsMessage_WithMoneyAmounts_ShowsCorrectAmounts()
    {
        // Arrange
        var requiredAmount = new Money(100m);
        var availableAmount = new Money(25m);

        // Act
        var exception = new DoubleDownException(TestPlayerName, _testHand, requiredAmount, availableAmount);

        // Assert
        Assert.Contains("Required: 100.00 USD", exception.Message);
        Assert.Contains("Available: 25.00 USD", exception.Message);
        Assert.Contains("Shortfall: 75.00 USD", exception.Message);
    }

    [Fact]
    public void Shortfall_WithValidAmounts_CalculatesCorrectly()
    {
        // Arrange
        var requiredAmount = new Money(150m);
        var availableAmount = new Money(75m);
        var exception = new DoubleDownException(TestPlayerName, _testHand, requiredAmount, availableAmount);

        // Act
        var shortfall = exception.Shortfall;

        // Assert
        Assert.Equal(new Money(75m), shortfall);
    }

    [Fact]
    public void Shortfall_WithNullAmounts_ReturnsNull()
    {
        // Arrange
        var exception = new DoubleDownException(TestPlayerName, _testHand, DoubleDownInvalidReason.WrongCardCount);

        // Act
        var shortfall = exception.Shortfall;

        // Assert
        Assert.Null(shortfall);
    }
}