using System;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a betting operation is attempted during an inappropriate game phase.
/// Provides information about the current phase and required phase for recovery guidance.
/// </summary>
public class BettingPhaseException : GameException
{
    /// <summary>
    /// Initializes a new instance of the BettingPhaseException class.
    /// </summary>
    /// <param name="currentPhase">The current betting phase.</param>
    /// <param name="requiredPhase">The required betting phase for the operation.</param>
    public BettingPhaseException(BettingPhase currentPhase, BettingPhase requiredPhase)
        : base(CreateMessage(currentPhase, requiredPhase))
    {
        CurrentPhase = currentPhase;
        RequiredPhase = requiredPhase;
    }

    /// <summary>
    /// Initializes a new instance of the BettingPhaseException class with a custom message.
    /// </summary>
    /// <param name="currentPhase">The current betting phase.</param>
    /// <param name="requiredPhase">The required betting phase for the operation.</param>
    /// <param name="message">A custom error message.</param>
    public BettingPhaseException(BettingPhase currentPhase, BettingPhase requiredPhase, string message)
        : base(message)
    {
        CurrentPhase = currentPhase;
        RequiredPhase = requiredPhase;
    }

    /// <summary>
    /// Initializes a new instance of the BettingPhaseException class with a custom message and inner exception.
    /// </summary>
    /// <param name="currentPhase">The current betting phase.</param>
    /// <param name="requiredPhase">The required betting phase for the operation.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public BettingPhaseException(BettingPhase currentPhase, BettingPhase requiredPhase, string message, Exception innerException)
        : base(message, innerException)
    {
        CurrentPhase = currentPhase;
        RequiredPhase = requiredPhase;
    }

    /// <summary>
    /// Gets the current betting phase.
    /// </summary>
    public BettingPhase CurrentPhase { get; }

    /// <summary>
    /// Gets the required betting phase for the operation.
    /// </summary>
    public BettingPhase RequiredPhase { get; }

    /// <summary>
    /// Gets recovery guidance based on the phase mismatch.
    /// </summary>
    public string RecoveryGuidance => (CurrentPhase, RequiredPhase) switch
    {
        (BettingPhase.NotStarted, BettingPhase.Accepting) => 
            "Please wait for the betting phase to begin before placing bets.",
        (BettingPhase.Accepting, BettingPhase.NotStarted) => 
            "Betting is currently in progress. This operation is not available during betting.",
        (BettingPhase.Closed, BettingPhase.Accepting) => 
            "The betting phase has ended. Please wait for the next round to place bets.",
        (BettingPhase.Processing, BettingPhase.Accepting) => 
            "Bets are currently being processed. Please wait for the next round.",
        (BettingPhase.Processing, BettingPhase.Closed) => 
            "Please wait for bet processing to complete.",
        (BettingPhase.Accepting, BettingPhase.Closed) => 
            "Please wait for the betting phase to close before proceeding.",
        (BettingPhase.Accepting, BettingPhase.Processing) => 
            "Please wait for all bets to be placed before processing.",
        _ => "Please wait for the appropriate game phase before attempting this operation."
    };

    /// <summary>
    /// Creates a standard error message for betting phase violations.
    /// </summary>
    /// <param name="currentPhase">The current phase.</param>
    /// <param name="requiredPhase">The required phase.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(BettingPhase currentPhase, BettingPhase requiredPhase)
    {
        return $"Invalid betting phase. Current phase: {currentPhase}, Required phase: {requiredPhase}.";
    }
}

/// <summary>
/// Represents the different phases of the betting process in a blackjack game.
/// </summary>
public enum BettingPhase
{
    /// <summary>
    /// Betting has not started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Betting is currently accepting bets from players.
    /// </summary>
    Accepting,

    /// <summary>
    /// Betting phase is closed, no more bets can be placed.
    /// </summary>
    Closed,

    /// <summary>
    /// Bets are being processed and payouts calculated.
    /// </summary>
    Processing
}