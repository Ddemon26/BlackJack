using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using GroupProject.Application.Services;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Application.Services;

public class EnhancedErrorHandlerTests
{
    private readonly Mock<IOutputProvider> _mockOutputProvider;
    private readonly Mock<IGameStatePreserver> _mockStatePreserver;
    private readonly ErrorHandler _errorHandler;

    public EnhancedErrorHandlerTests()
    {
        _mockOutputProvider = new Mock<IOutputProvider>();
        _mockStatePreserver = new Mock<IGameStatePreserver>();
        _errorHandler = new ErrorHandler(_mockOutputProvider.Object, _mockStatePreserver.Object);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithCriticalError_PreservesState()
    {
        // Arrange
        var criticalException = new InvalidGameStateException("Critical game state error");
        const string context = "Test context";

        _mockStatePreserver
            .Setup(x => x.PreserveStateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("state_id_123");

        // Act
        var result = await _errorHandler.HandleExceptionAsync(criticalException, context);

        // Assert
        _mockStatePreserver.Verify(
            x => x.PreserveStateAsync(
                It.Is<string>(id => id.StartsWith("error_recovery_")),
                It.Is<string>(ctx => ctx.Contains("InvalidGameStateException") && ctx.Contains(context))),
            Times.Once);

        Assert.Contains("Game state error", result);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithNonCriticalError_DoesNotPreserveState()
    {
        // Arrange
        var nonCriticalException = new ArgumentException("Non-critical error");

        // Act
        await _errorHandler.HandleExceptionAsync(nonCriticalException);

        // Assert
        _mockStatePreserver.Verify(
            x => x.PreserveStateAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleExceptionAsync_WithStatePreservationFailure_ContinuesHandling()
    {
        // Arrange
        var criticalException = new InvalidGameStateException("Critical error");
        _mockStatePreserver
            .Setup(x => x.PreserveStateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("State preservation failed"));

        // Act & Assert (should not throw)
        var result = await _errorHandler.HandleExceptionAsync(criticalException);
        
        Assert.Contains("Game state error", result);
        _mockOutputProvider.Verify(
            x => x.WriteLineAsync(It.Is<string>(msg => msg.Contains("State preservation failed"))),
            Times.Once);
    }

    [Fact]
    public void IsRecoverableError_WithNewExceptionTypes_ReturnsCorrectValues()
    {
        // Arrange & Act & Assert
        Assert.True(_errorHandler.IsRecoverableError(new InvalidBetException(new Money(10), new Money(5), new Money(100))));
        Assert.True(_errorHandler.IsRecoverableError(new InsufficientFundsException("Player1", new Money(100), new Money(50))));
        Assert.True(_errorHandler.IsRecoverableError(new BettingPhaseException(BettingPhase.Accepting, BettingPhase.Closed)));
        
        var testHand = new Hand();
        testHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        testHand.AddCard(new Card(Suit.Spades, Rank.Seven));
        
        Assert.True(_errorHandler.IsRecoverableError(new InvalidSplitException("Player1", testHand, SplitInvalidReason.NotPair)));
        Assert.True(_errorHandler.IsRecoverableError(new SplitHandLimitException("Player1", 2, 3)));
        Assert.True(_errorHandler.IsRecoverableError(new DoubleDownException("Player1", testHand, DoubleDownInvalidReason.WrongCardCount)));
    }

    [Fact]
    public void IsRecoverableError_WithSessionExceptions_ReturnsBasedOnRecoverableProperty()
    {
        // Arrange
        var recoverableSessionException = new SessionException("session1", "operation", SessionErrorReason.ConcurrentAccess);
        var nonRecoverableSessionException = new SessionException("session1", "operation", SessionErrorReason.SessionNotFound);

        // Act & Assert
        Assert.True(_errorHandler.IsRecoverableError(recoverableSessionException));
        Assert.False(_errorHandler.IsRecoverableError(nonRecoverableSessionException));
    }

    [Fact]
    public void GetUserFriendlyMessage_WithBettingExceptions_IncludesRecoveryGuidance()
    {
        // Arrange
        var invalidBetException = new InvalidBetException(new Money(10), new Money(5), new Money(100));
        var insufficientFundsException = new InsufficientFundsException("Player1", new Money(100), new Money(50));

        // Act
        var betMessage = _errorHandler.GetUserFriendlyMessage(invalidBetException);
        var fundsMessage = _errorHandler.GetUserFriendlyMessage(insufficientFundsException);

        // Assert
        Assert.Contains(invalidBetException.RecoveryGuidance, betMessage);
        Assert.Contains(insufficientFundsException.RecoveryGuidance, fundsMessage);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithSplitExceptions_IncludesRecoveryGuidance()
    {
        // Arrange
        var testHand = new Hand();
        testHand.AddCard(new Card(Suit.Hearts, Rank.Eight));
        testHand.AddCard(new Card(Suit.Spades, Rank.Seven));
        
        var invalidSplitException = new InvalidSplitException("Player1", testHand, SplitInvalidReason.NotPair);
        var splitLimitException = new SplitHandLimitException("Player1", 2, 3);

        // Act
        var splitMessage = _errorHandler.GetUserFriendlyMessage(invalidSplitException);
        var limitMessage = _errorHandler.GetUserFriendlyMessage(splitLimitException);

        // Assert
        Assert.Contains(invalidSplitException.RecoveryGuidance, splitMessage);
        Assert.Contains(splitLimitException.RecoveryGuidance, limitMessage);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithDoubleDownException_IncludesRecoveryGuidance()
    {
        // Arrange
        var testHand = new Hand();
        testHand.AddCard(new Card(Suit.Hearts, Rank.Ten));
        testHand.AddCard(new Card(Suit.Spades, Rank.Ace));
        
        var doubleDownException = new DoubleDownException("Player1", testHand, DoubleDownInvalidReason.WrongCardCount);

        // Act
        var message = _errorHandler.GetUserFriendlyMessage(doubleDownException);

        // Assert
        Assert.Contains(doubleDownException.RecoveryGuidance, message);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithSessionException_IncludesRecoveryGuidance()
    {
        // Arrange
        var sessionException = new SessionException("session1", "operation", SessionErrorReason.ConcurrentAccess);

        // Act
        var message = _errorHandler.GetUserFriendlyMessage(sessionException);

        // Assert
        Assert.Contains("Session error:", message);
        Assert.Contains(sessionException.RecoveryGuidance, message);
    }

    [Fact]
    public async Task TryRecoverFromErrorAsync_WithRecoverableErrorAndAvailableState_ReturnsTrue()
    {
        // Arrange
        var recoverableException = new InvalidBetException(new Money(10), new Money(5), new Money(100));
        var stateIds = new[] { "error_recovery_20231201_120000", "checkpoint_game_start" };
        
        _mockStatePreserver
            .Setup(x => x.GetPreservedStateIdsAsync())
            .ReturnsAsync(stateIds);
        
        _mockStatePreserver
            .Setup(x => x.RestoreStateAsync("error_recovery_20231201_120000"))
            .ReturnsAsync(true);

        // Act
        var result = await _errorHandler.TryRecoverFromErrorAsync(recoverableException);

        // Assert
        Assert.True(result);
        _mockStatePreserver.Verify(x => x.RestoreStateAsync("error_recovery_20231201_120000"), Times.Once);
    }

    [Fact]
    public async Task TryRecoverFromErrorAsync_WithNonRecoverableError_ReturnsFalse()
    {
        // Arrange
        var nonRecoverableException = new OutOfMemoryException("Out of memory");

        // Act
        var result = await _errorHandler.TryRecoverFromErrorAsync(nonRecoverableException);

        // Assert
        Assert.False(result);
        _mockStatePreserver.Verify(x => x.GetPreservedStateIdsAsync(), Times.Never);
    }

    [Fact]
    public async Task TryRecoverFromErrorAsync_WithNoStatePreserver_ReturnsFalse()
    {
        // Arrange
        var errorHandlerWithoutPreserver = new ErrorHandler(_mockOutputProvider.Object);
        var recoverableException = new InvalidBetException(new Money(10), new Money(5), new Money(100));

        // Act
        var result = await errorHandlerWithoutPreserver.TryRecoverFromErrorAsync(recoverableException);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryRecoverFromErrorAsync_WithNoAvailableStates_ReturnsFalse()
    {
        // Arrange
        var recoverableException = new InvalidBetException(new Money(10), new Money(5), new Money(100));
        
        _mockStatePreserver
            .Setup(x => x.GetPreservedStateIdsAsync())
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _errorHandler.TryRecoverFromErrorAsync(recoverableException);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateCheckpointAsync_WithValidParameters_CreatesCheckpoint()
    {
        // Arrange
        const string checkpointName = "game_start";
        const string context = "Before dealing cards";
        const string expectedCheckpointId = "checkpoint_game_start_20231201_120000";

        _mockStatePreserver
            .Setup(x => x.PreserveStateAsync(It.IsAny<string>(), context))
            .ReturnsAsync(expectedCheckpointId);

        // Act
        var result = await _errorHandler.CreateCheckpointAsync(checkpointName, context);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("checkpoint_", result);
        _mockStatePreserver.Verify(
            x => x.PreserveStateAsync(It.Is<string>(id => id.StartsWith("checkpoint_game_start_")), context),
            Times.Once);
    }

    [Fact]
    public async Task CreateCheckpointAsync_WithNoStatePreserver_ReturnsNull()
    {
        // Arrange
        var errorHandlerWithoutPreserver = new ErrorHandler(_mockOutputProvider.Object);

        // Act
        var result = await errorHandlerWithoutPreserver.CreateCheckpointAsync("test");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCheckpointAsync_WithEmptyCheckpointName_ReturnsNull()
    {
        // Act
        var result = await _errorHandler.CreateCheckpointAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CleanupOldRecoveryStatesAsync_WithStatePreserver_CallsClearOldStates()
    {
        // Arrange
        const int expectedCleanedCount = 5;
        var maxAge = TimeSpan.FromHours(12);

        _mockStatePreserver
            .Setup(x => x.ClearOldStatesAsync(maxAge))
            .ReturnsAsync(expectedCleanedCount);

        // Act
        var result = await _errorHandler.CleanupOldRecoveryStatesAsync(maxAge);

        // Assert
        Assert.Equal(expectedCleanedCount, result);
        _mockStatePreserver.Verify(x => x.ClearOldStatesAsync(maxAge), Times.Once);
    }

    [Fact]
    public async Task CleanupOldRecoveryStatesAsync_WithDefaultMaxAge_UsesDefaultValue()
    {
        // Arrange
        const int expectedCleanedCount = 3;

        _mockStatePreserver
            .Setup(x => x.ClearOldStatesAsync(TimeSpan.FromHours(24)))
            .ReturnsAsync(expectedCleanedCount);

        // Act
        var result = await _errorHandler.CleanupOldRecoveryStatesAsync();

        // Assert
        Assert.Equal(expectedCleanedCount, result);
        _mockStatePreserver.Verify(x => x.ClearOldStatesAsync(TimeSpan.FromHours(24)), Times.Once);
    }

    [Fact]
    public async Task CleanupOldRecoveryStatesAsync_WithNoStatePreserver_ReturnsZero()
    {
        // Arrange
        var errorHandlerWithoutPreserver = new ErrorHandler(_mockOutputProvider.Object);

        // Act
        var result = await errorHandlerWithoutPreserver.CleanupOldRecoveryStatesAsync();

        // Assert
        Assert.Equal(0, result);
    }
}