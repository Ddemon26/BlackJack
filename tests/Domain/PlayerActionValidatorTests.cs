using GroupProject.Domain.Entities;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain;

/// <summary>
/// Unit tests for the PlayerActionValidator class.
/// </summary>
public class PlayerActionValidatorTests
{
    private readonly PlayerActionValidator _validator;
    private readonly GameRules _gameRules;

    public PlayerActionValidatorTests()
    {
        _gameRules = new GameRules();
        _validator = new PlayerActionValidator(_gameRules);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullGameRules_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PlayerActionValidator(null!));
    }

    #endregion

    #region ValidateAction Tests

    [Fact]
    public void ValidateAction_WithNullHand_ReturnsInvalid()
    {
        // Act
        var result = _validator.ValidateAction(PlayerAction.Hit, null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Hand cannot be null.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAction_WithBustedHand_ReturnsInvalid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _validator.ValidateAction(PlayerAction.Hit, hand);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Cannot take action on a busted hand.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAction_HitOnValidHand_ReturnsValid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _validator.ValidateAction(PlayerAction.Hit, hand);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAction_StandOnValidHand_ReturnsValid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _validator.ValidateAction(PlayerAction.Stand, hand);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAction_DoubleDownOnTwoCards_ReturnsValid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15, 2 cards

        // Act
        var result = _validator.ValidateAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAction_DoubleDownOnThreeCards_ReturnsInvalid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Five), new Card(Suit.Spades, Rank.Five), new Card(Suit.Diamonds, Rank.Five)); // 15, 3 cards

        // Act
        var result = _validator.ValidateAction(PlayerAction.DoubleDown, hand);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Can only double down on initial two cards.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAction_SplitOnPair_ReturnsValid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight)); // Pair of 8s

        // Act
        var result = _validator.ValidateAction(PlayerAction.Split, hand);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAction_SplitOnNonPair_ReturnsInvalid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Nine)); // Not a pair

        // Act
        var result = _validator.ValidateAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Can only split pairs of the same rank.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateAction_SplitOnThreeCards_ReturnsInvalid()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight), new Card(Suit.Diamonds, Rank.Five)); // 3 cards

        // Act
        var result = _validator.ValidateAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Can only split with exactly two cards.", result.ErrorMessage);
    }

    #endregion

    #region GetValidActions Tests

    [Fact]
    public void GetValidActions_WithNullHand_ReturnsEmpty()
    {
        // Act
        var result = _validator.GetValidActions(null!);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetValidActions_WithBustedHand_ReturnsEmpty()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25

        // Act
        var result = _validator.GetValidActions(hand);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetValidActions_WithTwoCardHand_ReturnsHitStandDoubleDown()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15, 2 cards

        // Act
        var result = _validator.GetValidActions(hand).ToList();

        // Assert
        Assert.Contains(PlayerAction.Hit, result);
        Assert.Contains(PlayerAction.Stand, result);
        Assert.Contains(PlayerAction.DoubleDown, result);
        Assert.DoesNotContain(PlayerAction.Split, result);
    }

    [Fact]
    public void GetValidActions_WithPair_ReturnsAllActions()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight)); // Pair of 8s

        // Act
        var result = _validator.GetValidActions(hand).ToList();

        // Assert
        Assert.Contains(PlayerAction.Hit, result);
        Assert.Contains(PlayerAction.Stand, result);
        Assert.Contains(PlayerAction.DoubleDown, result);
        Assert.Contains(PlayerAction.Split, result);
    }

    [Fact]
    public void GetValidActions_WithThreeCards_ReturnsHitStandOnly()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Five), new Card(Suit.Spades, Rank.Five), new Card(Suit.Diamonds, Rank.Five)); // 15, 3 cards

        // Act
        var result = _validator.GetValidActions(hand).ToList();

        // Assert
        Assert.Contains(PlayerAction.Hit, result);
        Assert.Contains(PlayerAction.Stand, result);
        Assert.DoesNotContain(PlayerAction.DoubleDown, result);
        Assert.DoesNotContain(PlayerAction.Split, result);
    }

    #endregion

    #region ProcessAction Tests

    [Fact]
    public void ProcessAction_HitWithValidCard_ReturnsSuccess()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15
        var card = new Card(Suit.Diamonds, Rank.Three); // Adding 3 for total of 18

        // Act
        var result = _validator.ProcessAction(PlayerAction.Hit, hand, card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(3, result.UpdatedHand.CardCount);
        Assert.Equal(18, result.UpdatedHand.GetValue());
        Assert.True(result.ShouldContinueTurn);
        Assert.False(result.IsBusted);
    }

    [Fact]
    public void ProcessAction_HitCausingBust_ReturnsSuccessButEndsTurn()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19
        var card = new Card(Suit.Diamonds, Rank.Five); // Adding 5 for total of 24 (bust)

        // Act
        var result = _validator.ProcessAction(PlayerAction.Hit, hand, card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(3, result.UpdatedHand.CardCount);
        Assert.Equal(24, result.UpdatedHand.GetValue());
        Assert.False(result.ShouldContinueTurn);
        Assert.True(result.IsBusted);
    }

    [Fact]
    public void ProcessAction_HitWithoutCard_ReturnsFailure()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _validator.ProcessAction(PlayerAction.Hit, hand, null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card is required for hit action.", result.ErrorMessage);
    }

    [Fact]
    public void ProcessAction_Stand_ReturnsSuccessAndEndsTurn()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Nine)); // 19

        // Act
        var result = _validator.ProcessAction(PlayerAction.Stand, hand);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(2, result.UpdatedHand.CardCount);
        Assert.Equal(19, result.UpdatedHand.GetValue());
        Assert.False(result.ShouldContinueTurn);
        Assert.False(result.IsBusted);
    }

    [Fact]
    public void ProcessAction_DoubleDownWithCard_ReturnsSuccessAndEndsTurn()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15
        var card = new Card(Suit.Diamonds, Rank.Four); // Adding 4 for total of 19

        // Act
        var result = _validator.ProcessAction(PlayerAction.DoubleDown, hand, card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(3, result.UpdatedHand.CardCount);
        Assert.Equal(19, result.UpdatedHand.GetValue());
        Assert.False(result.ShouldContinueTurn);
        Assert.False(result.IsBusted);
    }

    [Fact]
    public void ProcessAction_DoubleDownWithoutCard_ReturnsFailure()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ten), new Card(Suit.Spades, Rank.Five)); // 15

        // Act
        var result = _validator.ProcessAction(PlayerAction.DoubleDown, hand, null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card is required for double down action.", result.ErrorMessage);
    }

    [Fact]
    public void ProcessAction_Split_ReturnsFailure()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Eight), new Card(Suit.Spades, Rank.Eight)); // Pair of 8s

        // Act
        var result = _validator.ProcessAction(PlayerAction.Split, hand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Split action is not yet implemented.", result.ErrorMessage);
    }

    [Fact]
    public void ProcessAction_InvalidAction_ReturnsFailure()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.King), new Card(Suit.Spades, Rank.Queen), new Card(Suit.Diamonds, Rank.Five)); // 25 (busted)

        // Act
        var result = _validator.ProcessAction(PlayerAction.Hit, hand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot take action on a busted hand.", result.ErrorMessage);
    }

    [Fact]
    public void ProcessAction_HitResultingInBlackjack_EndsTurn()
    {
        // Arrange
        var hand = CreateHand(new Card(Suit.Hearts, Rank.Ace)); // 11
        var card = new Card(Suit.Spades, Rank.King); // Adding King for blackjack

        // Act
        var result = _validator.ProcessAction(PlayerAction.Hit, hand, card);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedHand);
        Assert.Equal(2, result.UpdatedHand.CardCount);
        Assert.Equal(21, result.UpdatedHand.GetValue());
        Assert.False(result.ShouldContinueTurn);
        Assert.False(result.IsBusted);
        Assert.True(result.IsBlackjack);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a hand with the specified cards for testing.
    /// </summary>
    /// <param name="cards">The cards to add to the hand.</param>
    /// <returns>A hand containing the specified cards.</returns>
    private static Hand CreateHand(params Card[] cards)
    {
        var hand = new Hand();
        foreach (var card in cards)
        {
            hand.AddCard(card);
        }
        return hand;
    }

    #endregion
}