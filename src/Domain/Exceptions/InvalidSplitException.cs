using System;
using GroupProject.Domain.Entities;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a player attempts to split a hand that cannot be split.
/// Provides detailed information about why the split is invalid and recovery guidance.
/// </summary>
public class InvalidSplitException : GameException
{
    /// <summary>
    /// Initializes a new instance of the InvalidSplitException class.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="hand">The hand that cannot be split.</param>
    /// <param name="reason">The reason why the split is invalid.</param>
    public InvalidSplitException(string playerName, Hand hand, SplitInvalidReason reason)
        : base(CreateMessage(playerName ?? throw new ArgumentNullException(nameof(playerName)), 
                           hand ?? throw new ArgumentNullException(nameof(hand)), 
                           reason))
    {
        PlayerName = playerName;
        AttemptedHand = hand;
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidSplitException class with a custom message.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="hand">The hand that cannot be split.</param>
    /// <param name="reason">The reason why the split is invalid.</param>
    /// <param name="message">A custom error message.</param>
    public InvalidSplitException(string playerName, Hand hand, SplitInvalidReason reason, string message)
        : base(message)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        AttemptedHand = hand ?? throw new ArgumentNullException(nameof(hand));
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidSplitException class with a custom message and inner exception.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="hand">The hand that cannot be split.</param>
    /// <param name="reason">The reason why the split is invalid.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InvalidSplitException(string playerName, Hand hand, SplitInvalidReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        AttemptedHand = hand ?? throw new ArgumentNullException(nameof(hand));
        Reason = reason;
    }

    /// <summary>
    /// Gets the name of the player attempting the split.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets the hand that cannot be split.
    /// </summary>
    public Hand AttemptedHand { get; }

    /// <summary>
    /// Gets the reason why the split is invalid.
    /// </summary>
    public SplitInvalidReason Reason { get; }

    /// <summary>
    /// Gets recovery guidance based on the invalid reason.
    /// </summary>
    public string RecoveryGuidance => Reason switch
    {
        SplitInvalidReason.NotPair => 
            "You can only split pairs (two cards of the same rank). Choose Hit, Stand, or Double Down instead.",
        SplitInvalidReason.InsufficientFunds => 
            "You don't have enough funds to split (requires matching your original bet). Consider a different action or add funds.",
        SplitInvalidReason.WrongCardCount => 
            "You can only split your initial two-card hand. This option is not available after taking additional cards.",
        SplitInvalidReason.AlreadySplit => 
            "This hand has already been split. You cannot split the same hand multiple times.",
        SplitInvalidReason.MaxSplitsReached => 
            "You have reached the maximum number of splits allowed. Continue playing your existing hands.",
        SplitInvalidReason.GameState => 
            "Splitting is not allowed at this time. Wait for your turn and ensure the game is in the correct phase.",
        SplitInvalidReason.RuleDisabled => 
            "Splitting is disabled in the current game configuration. Choose from the available actions.",
        SplitInvalidReason.HandComplete => 
            "This hand is already complete. You cannot split a finished hand.",
        _ => "Please check the game rules and try a different action."
    };

    /// <summary>
    /// Creates a standard error message for split validation failures.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="hand">The hand that cannot be split.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(string playerName, Hand hand, SplitInvalidReason reason)
    {
        var handDescription = hand.Cards.Count == 2 
            ? $"hand with {hand.Cards[0].Rank} and {hand.Cards[1].Rank}"
            : $"{hand.Cards.Count}-card hand";

        return reason switch
        {
            SplitInvalidReason.NotPair => 
                $"Player '{playerName}' cannot split {handDescription} - cards must be the same rank.",
            SplitInvalidReason.InsufficientFunds => 
                $"Player '{playerName}' has insufficient funds to split {handDescription}.",
            SplitInvalidReason.WrongCardCount => 
                $"Player '{playerName}' cannot split {handDescription} - only initial two-card hands can be split.",
            SplitInvalidReason.AlreadySplit => 
                $"Player '{playerName}' cannot split {handDescription} - hand has already been split.",
            SplitInvalidReason.MaxSplitsReached => 
                $"Player '{playerName}' cannot split {handDescription} - maximum splits reached.",
            SplitInvalidReason.GameState => 
                $"Player '{playerName}' cannot split {handDescription} - invalid game state.",
            SplitInvalidReason.RuleDisabled => 
                $"Player '{playerName}' cannot split {handDescription} - splitting is disabled.",
            SplitInvalidReason.HandComplete => 
                $"Player '{playerName}' cannot split {handDescription} - hand is already complete.",
            _ => 
                $"Player '{playerName}' cannot split {handDescription} - invalid split attempt."
        };
    }
}

/// <summary>
/// Represents the different reasons why a split operation might be invalid.
/// </summary>
public enum SplitInvalidReason
{
    /// <summary>
    /// The hand does not contain a pair (two cards of the same rank).
    /// </summary>
    NotPair,

    /// <summary>
    /// The player does not have sufficient funds to place the additional bet required for splitting.
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// The hand has more or fewer than two cards (splits only allowed on initial two-card hands).
    /// </summary>
    WrongCardCount,

    /// <summary>
    /// The hand has already been split and cannot be split again.
    /// </summary>
    AlreadySplit,

    /// <summary>
    /// The player has reached the maximum number of allowed splits.
    /// </summary>
    MaxSplitsReached,

    /// <summary>
    /// The game is not in a state that allows splitting (e.g., not the player's turn).
    /// </summary>
    GameState,

    /// <summary>
    /// Splitting is disabled in the current game configuration.
    /// </summary>
    RuleDisabled,

    /// <summary>
    /// The hand is already complete and cannot be split.
    /// </summary>
    HandComplete
}