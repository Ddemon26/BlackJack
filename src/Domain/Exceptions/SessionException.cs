using System;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when session-related operations fail.
/// Provides information about session state and recovery guidance.
/// </summary>
public class SessionException : GameException
{
    /// <summary>
    /// Initializes a new instance of the SessionException class.
    /// </summary>
    /// <param name="sessionId">The ID of the session that encountered the error.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    public SessionException(string sessionId, string operation, SessionErrorReason reason)
        : base(CreateMessage(sessionId, operation, reason))
    {
        SessionId = sessionId;
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the SessionException class with a custom message.
    /// </summary>
    /// <param name="sessionId">The ID of the session that encountered the error.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="message">A custom error message.</param>
    public SessionException(string sessionId, string operation, SessionErrorReason reason, string message)
        : base(message)
    {
        SessionId = sessionId;
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the SessionException class with a custom message and inner exception.
    /// </summary>
    /// <param name="sessionId">The ID of the session that encountered the error.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public SessionException(string sessionId, string operation, SessionErrorReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        SessionId = sessionId;
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Reason = reason;
    }

    /// <summary>
    /// Gets the ID of the session that encountered the error.
    /// </summary>
    public string? SessionId { get; }

    /// <summary>
    /// Gets the operation that failed.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets the reason for the session error.
    /// </summary>
    public SessionErrorReason Reason { get; }

    /// <summary>
    /// Gets recovery guidance based on the error reason.
    /// </summary>
    public string RecoveryGuidance => Reason switch
    {
        SessionErrorReason.SessionNotFound => 
            "The session could not be found. Please start a new game session.",
        SessionErrorReason.SessionExpired => 
            "Your session has expired. Please start a new game session to continue playing.",
        SessionErrorReason.SessionCorrupted => 
            "The session data is corrupted. Please start a new game session. Your statistics may have been preserved.",
        SessionErrorReason.InvalidState => 
            "The session is in an invalid state. Try restarting the current round or start a new session.",
        SessionErrorReason.ConcurrentAccess => 
            "Another operation is currently modifying the session. Please wait a moment and try again.",
        SessionErrorReason.PersistenceFailure => 
            "Failed to save session data. Your progress may not be saved. Check disk space and permissions.",
        SessionErrorReason.ConfigurationError => 
            "Session configuration is invalid. Please check your game settings and try again.",
        SessionErrorReason.PlayerNotInSession => 
            "The specified player is not part of this session. Please check the player name or start a new session.",
        SessionErrorReason.SessionFull => 
            "The session is full and cannot accept more players. Please start a new session or wait for a player to leave.",
        SessionErrorReason.InsufficientPlayers => 
            "The session does not have enough players to continue. Please add more players or start a new session.",
        _ => "Please try the operation again or start a new session if the problem persists."
    };

    /// <summary>
    /// Gets a value indicating whether the error is recoverable without starting a new session.
    /// </summary>
    public bool IsRecoverable => Reason switch
    {
        SessionErrorReason.ConcurrentAccess => true,
        SessionErrorReason.InvalidState => true,
        SessionErrorReason.PersistenceFailure => true,
        SessionErrorReason.PlayerNotInSession => true,
        SessionErrorReason.SessionFull => false,
        SessionErrorReason.InsufficientPlayers => true,
        _ => false
    };

    /// <summary>
    /// Creates a standard error message for session errors.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="operation">The failed operation.</param>
    /// <param name="reason">The error reason.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(string? sessionId, string operation, SessionErrorReason reason)
    {
        var sessionInfo = string.IsNullOrEmpty(sessionId) ? "Session" : $"Session '{sessionId}'";
        
        return reason switch
        {
            SessionErrorReason.SessionNotFound => 
                $"{sessionInfo} not found during {operation}.",
            SessionErrorReason.SessionExpired => 
                $"{sessionInfo} has expired during {operation}.",
            SessionErrorReason.SessionCorrupted => 
                $"{sessionInfo} data is corrupted during {operation}.",
            SessionErrorReason.InvalidState => 
                $"{sessionInfo} is in an invalid state for {operation}.",
            SessionErrorReason.ConcurrentAccess => 
                $"{sessionInfo} is being modified by another operation during {operation}.",
            SessionErrorReason.PersistenceFailure => 
                $"Failed to persist {sessionInfo} during {operation}.",
            SessionErrorReason.ConfigurationError => 
                $"{sessionInfo} has invalid configuration for {operation}.",
            SessionErrorReason.PlayerNotInSession => 
                $"Player not found in {sessionInfo} during {operation}.",
            SessionErrorReason.SessionFull => 
                $"{sessionInfo} is full and cannot perform {operation}.",
            SessionErrorReason.InsufficientPlayers => 
                $"{sessionInfo} has insufficient players for {operation}.",
            _ => 
                $"{sessionInfo} encountered an error during {operation}."
        };
    }
}

/// <summary>
/// Represents the different types of session errors that can occur.
/// </summary>
public enum SessionErrorReason
{
    /// <summary>
    /// The session could not be found.
    /// </summary>
    SessionNotFound,

    /// <summary>
    /// The session has expired and is no longer valid.
    /// </summary>
    SessionExpired,

    /// <summary>
    /// The session data is corrupted and cannot be used.
    /// </summary>
    SessionCorrupted,

    /// <summary>
    /// The session is in an invalid state for the requested operation.
    /// </summary>
    InvalidState,

    /// <summary>
    /// Multiple operations are trying to access the session concurrently.
    /// </summary>
    ConcurrentAccess,

    /// <summary>
    /// Failed to save or load session data.
    /// </summary>
    PersistenceFailure,

    /// <summary>
    /// The session configuration is invalid.
    /// </summary>
    ConfigurationError,

    /// <summary>
    /// The specified player is not part of the session.
    /// </summary>
    PlayerNotInSession,

    /// <summary>
    /// The session has reached its maximum player capacity.
    /// </summary>
    SessionFull,

    /// <summary>
    /// The session does not have enough players to continue.
    /// </summary>
    InsufficientPlayers
}