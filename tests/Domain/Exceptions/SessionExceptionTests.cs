using System;
using Xunit;
using GroupProject.Domain.Exceptions;

namespace GroupProject.Tests.Domain.Exceptions;

public class SessionExceptionTests
{
    private const string TestSessionId = "session_123";
    private const string TestOperation = "StartNewRound";

    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.SessionNotFound);

        // Assert
        Assert.Equal(TestSessionId, exception.SessionId);
        Assert.Equal(TestOperation, exception.Operation);
        Assert.Equal(SessionErrorReason.SessionNotFound, exception.Reason);
        Assert.Contains("not found", exception.Message);
        Assert.Contains(TestSessionId, exception.Message);
        Assert.Contains(TestOperation, exception.Message);
    }

    [Fact]
    public void Constructor_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionException(TestSessionId, null!, SessionErrorReason.SessionNotFound));
    }

    [Fact]
    public void Constructor_WithNullSessionId_AllowsNullSessionId()
    {
        // Arrange & Act
        var exception = new SessionException(null, TestOperation, SessionErrorReason.SessionNotFound);

        // Assert
        Assert.Null(exception.SessionId);
        Assert.Equal(TestOperation, exception.Operation);
        Assert.Contains("Session not found", exception.Message);
    }

    [Theory]
    [InlineData(SessionErrorReason.SessionNotFound, "The session could not be found")]
    [InlineData(SessionErrorReason.SessionExpired, "Your session has expired")]
    [InlineData(SessionErrorReason.SessionCorrupted, "The session data is corrupted")]
    [InlineData(SessionErrorReason.InvalidState, "The session is in an invalid state")]
    [InlineData(SessionErrorReason.ConcurrentAccess, "Another operation is currently modifying")]
    [InlineData(SessionErrorReason.PersistenceFailure, "Failed to save session data")]
    [InlineData(SessionErrorReason.ConfigurationError, "Session configuration is invalid")]
    [InlineData(SessionErrorReason.PlayerNotInSession, "The specified player is not part")]
    [InlineData(SessionErrorReason.SessionFull, "The session is full")]
    [InlineData(SessionErrorReason.InsufficientPlayers, "The session does not have enough players")]
    public void RecoveryGuidance_ForDifferentReasons_ProvidesAppropriateGuidance(SessionErrorReason reason, string expectedGuidanceFragment)
    {
        // Arrange
        var exception = new SessionException(TestSessionId, TestOperation, reason);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains(expectedGuidanceFragment, guidance);
    }

    [Theory]
    [InlineData(SessionErrorReason.ConcurrentAccess, true)]
    [InlineData(SessionErrorReason.InvalidState, true)]
    [InlineData(SessionErrorReason.PersistenceFailure, true)]
    [InlineData(SessionErrorReason.PlayerNotInSession, true)]
    [InlineData(SessionErrorReason.InsufficientPlayers, true)]
    [InlineData(SessionErrorReason.SessionFull, false)]
    [InlineData(SessionErrorReason.SessionNotFound, false)]
    [InlineData(SessionErrorReason.SessionExpired, false)]
    [InlineData(SessionErrorReason.SessionCorrupted, false)]
    [InlineData(SessionErrorReason.ConfigurationError, false)]
    public void IsRecoverable_ForDifferentReasons_ReturnsCorrectValue(SessionErrorReason reason, bool expectedRecoverable)
    {
        // Arrange
        var exception = new SessionException(TestSessionId, TestOperation, reason);

        // Act & Assert
        Assert.Equal(expectedRecoverable, exception.IsRecoverable);
    }

    [Fact]
    public void Constructor_WithCustomMessage_UsesCustomMessage()
    {
        // Arrange
        const string customMessage = "Custom error message";

        // Act
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.SessionNotFound, customMessage);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(TestSessionId, exception.SessionId);
        Assert.Equal(TestOperation, exception.Operation);
        Assert.Equal(SessionErrorReason.SessionNotFound, exception.Reason);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner exception");
        const string customMessage = "Custom error message";

        // Act
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.SessionNotFound, customMessage, innerException);

        // Assert
        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(SessionErrorReason.SessionNotFound, "not found")]
    [InlineData(SessionErrorReason.SessionExpired, "has expired")]
    [InlineData(SessionErrorReason.SessionCorrupted, "is corrupted")]
    [InlineData(SessionErrorReason.InvalidState, "invalid state")]
    [InlineData(SessionErrorReason.ConcurrentAccess, "being modified")]
    [InlineData(SessionErrorReason.PersistenceFailure, "Failed to persist")]
    [InlineData(SessionErrorReason.ConfigurationError, "invalid configuration")]
    [InlineData(SessionErrorReason.PlayerNotInSession, "Player not found")]
    [InlineData(SessionErrorReason.SessionFull, "is full")]
    [InlineData(SessionErrorReason.InsufficientPlayers, "insufficient players")]
    public void CreateMessage_ForDifferentReasons_GeneratesAppropriateMessage(SessionErrorReason reason, string expectedMessageFragment)
    {
        // Arrange & Act
        var exception = new SessionException(TestSessionId, TestOperation, reason);

        // Assert
        Assert.Contains(expectedMessageFragment, exception.Message);
        Assert.Contains(TestOperation, exception.Message);
    }

    [Fact]
    public void CreateMessage_WithNullSessionId_UsesGenericSessionReference()
    {
        // Arrange & Act
        var exception = new SessionException(null, TestOperation, SessionErrorReason.SessionNotFound);

        // Assert
        Assert.Contains("Session not found", exception.Message);
        Assert.Contains(TestOperation, exception.Message);
        Assert.DoesNotContain("Session 'null'", exception.Message);
    }

    [Fact]
    public void CreateMessage_WithValidSessionId_IncludesSessionIdInMessage()
    {
        // Arrange & Act
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.SessionNotFound);

        // Assert
        Assert.Contains($"Session '{TestSessionId}'", exception.Message);
        Assert.Contains(TestOperation, exception.Message);
    }

    [Fact]
    public void RecoveryGuidance_ForConcurrentAccess_SuggestsRetry()
    {
        // Arrange
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.ConcurrentAccess);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("wait a moment and try again", guidance);
    }

    [Fact]
    public void RecoveryGuidance_ForPersistenceFailure_MentionsStorage()
    {
        // Arrange
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.PersistenceFailure);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("Check disk space and permissions", guidance);
    }

    [Fact]
    public void RecoveryGuidance_ForSessionFull_SuggestsNewSession()
    {
        // Arrange
        var exception = new SessionException(TestSessionId, TestOperation, SessionErrorReason.SessionFull);

        // Act
        var guidance = exception.RecoveryGuidance;

        // Assert
        Assert.Contains("start a new session", guidance);
    }
}