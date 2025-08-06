using System;
using Xunit;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.Exceptions;

public class InvalidSplitExceptionTests
{
    private readonly Hand _testHand;
    private const string TestPlayerName = "TestPlayer";

    public InvalidSplitExceptionTests()
    {
        // Create a test hand with two cards
        _testHand = new Hand();
        _testHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        _testHand.AddCard(new Card(Suit.Spades, Rank.Seven));
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var exception = new InvalidSplitException(TestPlayerName, _testHand, SplitInvalidReason.NotPair);

        // Assert
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(_testHand, exception.AttemptedHand);
        Assert.Equal(SplitInvalidReason.NotPair, exception.Reason);
        Assert.Contains("cannot split", exception.Message);
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPlayerName_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new InvalidSplitException(null!, _testHand, SplitInvalidReason.NotPair));
    }

    [Fact]
    public void Constructor_WithNullHand_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new InvalidSplitException(TestPlayerName, null!, SplitInvalidReason.NotPair));
    }

    [Theory]
    [InlineData(SplitInvalidReason.NotPair, "You can only split pairs")]
    [InlineData(SplitInvalidReason.InsufficientFunds, "You don't have enough funds")]
    [InlineData(SplitInvalidReason.WrongCardCount, "You can only split your initial two-card hand")]
    [InlineData(SplitInvalidReason.AlreadySplit, "This hand has already been split")]
    [InlineData(SplitInvalidReason.MaxSplitsReached, "You have reached the maximum number of splits")]
    [InlineData(SplitInvalidReason.GameState, "Splitting is not allowed at this time")]
    [InlineData(SplitInvalidReason.RuleDisabled, "Splitting is disabled")]
    [InlineData(SplitInvalidReason.HandComplete, "This hand is already complete")]
    public void RecoveryGuidance_ForDifferentReasons_ProvidesAppropriateGuidance(SplitInvalidReason reason, string expectedGuidanceFragment)
    {
        // Arrange
        var exception = new InvalidSplitException(TestPlayerName, _testHand, reason);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedGuidanceFragment, guidance);
    }

    [Fact]
    public void Constructor_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        const string customMessage = "Custom error message";

        // Act
        var exception = new InvalidSplitException(TestPlayerName, _testHand, SplitInvalidReason.NotPair, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(TestPlayerName, exception.PlayerName);
        Assert.Equal(_testHand, exception.AttemptedHand);
        Assert.Equal(SplitInvalidReason.NotPair, exception.Reason);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        const string customMessage = "Custom error message";

        // Act
        var exception = new InvalidSplitException(TestPlayerName, _testHand, SplitInvalidReason.NotPair, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(SplitInvalidReason.NotPair, "cards must be the same rank")]
    [InlineData(SplitInvalidReason.InsufficientFunds, "insufficient funds")]
    [InlineData(SplitInvalidReason.WrongCardCount, "only initial two-card hands")]
    [InlineData(SplitInvalidReason.AlreadySplit, "already been split")]
    [InlineData(SplitInvalidReason.MaxSplitsReached, "maximum splits reached")]
    [InlineData(SplitInvalidReason.GameState, "invalid game state")]
    [InlineData(SplitInvalidReason.RuleDisabled, "splitting is disabled")]
    [InlineData(SplitInvalidReason.HandComplete, "already complete")]
    public void CreateMessage_ForDifferentReasons_GeneratesAppropriateMessage(SplitInvalidReason reason, string expectedMessageFragment)
    {
        // Arrange & Act
        var exception = new InvalidSplitException(TestPlayerName, _testHand, reason);

        // Assert
        Assert.Contains(expectedMessageFragment, exception.Message.ToLowerInvariant());
        Assert.Contains(TestPlayerName, exception.Message);
    }

    [Fact]
    public void CreateMessage_WithThreeCardHand_DescribesHandCorrectly()
    {
        // Arrange
        var threeCardHand = new Hand();
        threeCardHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        threeCardHand.AddCard(new Card(Suit.Spades, Rank.Seven));
        threeCardHand.AddCard(new Card(Suit.Clubs, Rank.Six));

        // Act
        var exception = new InvalidSplitException(TestPlayerName, threeCardHand, SplitInvalidReason.WrongCardCount);

        // Assert
        Assert.Contains("3-card hand", exception.Message);
    }

    [Fact]
    public void CreateMessage_WithTwoCardHand_DescribesCardsCorrectly()
    {
        // Arrange
        var pairHand = new Hand();
        pairHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        pairHand.AddCard(new Card(Suit.Spades, Rank.Eight));

        // Act
        var exception = new InvalidSplitException(TestPlayerName, pairHand, SplitInvalidReason.InsufficientFunds);

        // Assert
        Assert.Contains("hand with Eight and Eight", exception.Message);
    }
}