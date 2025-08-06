using System;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a player attempts to double down but the action is not allowed.
/// Provides detailed information about why the double down is invalid and recovery guidance.
/// </summary>
public class DoubleDownException : GameException
{
    /// <summary>
    /// Initializes a new instance of the DoubleDownException class.
    /// </summary>
    /// <param name="playerName">The name of the player attempting to double down.</param>
    /// <param name="hand">The hand for which double down was attempted.</param>
    /// <param name="reason">The reason why the double down is invalid.</param>
    public DoubleDownException(string playerName, Hand hand, DoubleDownInvalidReason reason)
        : base(CreateMessage(playerName ?? throw new ArgumentNullException(nameof(playerName)), 
                           hand ?? throw new ArgumentNullException(nameof(hand)), 
                           reason))
    {
        PlayerName = playerName;
        AttemptedHand = hand;
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the DoubleDownException class with insufficient funds information.
    /// </summary>
    /// <param name="playerName">The name of the player attempting to double down.</param>
    /// <param name="hand">The hand for which double down was attempted.</param>
    /// <param name="requiredAmount">The amount required to double down.</param>
    /// <param name="availableAmount">The amount available in the player's bankroll.</param>
    public DoubleDownException(string playerName, Hand hand, Money requiredAmount, Money availableAmount)
        : base(CreateInsufficientFundsMessage(playerName ?? throw new ArgumentNullException(nameof(playerName)), 
                                            requiredAmount, availableAmount))
    {
        PlayerName = playerName;
        AttemptedHand = hand ?? throw new ArgumentNullException(nameof(hand));
        Reason = DoubleDownInvalidReason.InsufficientFunds;
        RequiredAmount = requiredAmount;
        AvailableAmount = availableAmount;
    }

    /// <summary>
    /// Initializes a new instance of the DoubleDownException class with a custom message.
    /// </summary>
    /// <param name="playerName">The name of the player attempting to double down.</param>
    /// <param name="hand">The hand for which double down was attempted.</param>
    /// <param name="reason">The reason why the double down is invalid.</param>
    /// <param name="message">A custom error message.</param>
    public DoubleDownException(string playerName, Hand hand, DoubleDownInvalidReason reason, string message)
        : base(message)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        AttemptedHand = hand ?? throw new ArgumentNullException(nameof(hand));
        Reason = reason;
    }

    /// <summary>
    /// Initializes a new instance of the DoubleDownException class with a custom message and inner exception.
    /// </summary>
    /// <param name="playerName">The name of the player attempting to double down.</param>
    /// <param name="hand">The hand for which double down was attempted.</param>
    /// <param name="reason">The reason why the double down is invalid.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public DoubleDownException(string playerName, Hand hand, DoubleDownInvalidReason reason, string message, Exception innerException)
        : base(message, innerException)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        AttemptedHand = hand ?? throw new ArgumentNullException(nameof(hand));
        Reason = reason;
    }

    /// <summary>
    /// Gets the name of the player attempting to double down.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets the hand for which double down was attempted.
    /// </summary>
    public Hand AttemptedHand { get; }

    /// <summary>
    /// Gets the reason why the double down is invalid.
    /// </summary>
    public DoubleDownInvalidReason Reason { get; }

    /// <summary>
    /// Gets the amount required to double down (if applicable).
    /// </summary>
    public Money? RequiredAmount { get; }

    /// <summary>
    /// Gets the amount available in the player's bankroll (if applicable).
    /// </summary>
    public Money? AvailableAmount { get; }

    /// <summary>
    /// Gets the shortfall amount for insufficient funds scenarios.
    /// </summary>
    public Money? Shortfall => RequiredAmount.HasValue && AvailableAmount.HasValue 
        ? RequiredAmount.Value - AvailableAmount.Value 
        : null;

    /// <summary>
    /// Gets recovery guidance based on the invalid reason.
    /// </summary>
    public string RecoveryGuidance => Reason switch
    {
        DoubleDownInvalidReason.WrongCardCount => 
            "You can only double down on your initial two-card hand. Choose Hit or Stand instead.",
        DoubleDownInvalidReason.InsufficientFunds when Shortfall.HasValue => 
            $"You need an additional {Shortfall.Value} to double down. Consider hitting or standing, or add funds to your bankroll.",
        DoubleDownInvalidReason.InsufficientFunds => 
            "You don't have enough funds to double down. Consider hitting or standing instead.",
        DoubleDownInvalidReason.HandComplete => 
            "This hand is already complete. You cannot double down on a finished hand.",
        DoubleDownInvalidReason.SplitHand => 
            "Double down is not allowed on split hands in this game configuration. Choose Hit or Stand instead.",
        DoubleDownInvalidReason.GameState => 
            "Double down is not allowed at this time. Wait for your turn and ensure the game is in the correct phase.",
        DoubleDownInvalidReason.RuleDisabled => 
            "Double down is disabled in the current game configuration. Choose Hit or Stand instead.",
        DoubleDownInvalidReason.AlreadyDoubled => 
            "You have already doubled down on this hand. Wait for the dealer to complete the round.",
        DoubleDownInvalidReason.HandValue => 
            "Double down is only allowed on certain hand values (typically 9, 10, or 11). Choose Hit or Stand instead.",
        _ => "Please check the game rules and choose Hit or Stand instead."
    };

    /// <summary>
    /// Creates a standard error message for double down validation failures.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="hand">The hand that cannot be doubled down.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(string playerName, Hand hand, DoubleDownInvalidReason reason)
    {
        var handDescription = $"hand with value {hand.GetValue()}";

        return reason switch
        {
            DoubleDownInvalidReason.WrongCardCount => 
                $"Player '{playerName}' cannot double down on {hand.Cards.Count}-card hand - only initial two-card hands allowed.",
            DoubleDownInvalidReason.InsufficientFunds => 
                $"Player '{playerName}' has insufficient funds to double down on {handDescription}.",
            DoubleDownInvalidReason.HandComplete => 
                $"Player '{playerName}' cannot double down on completed {handDescription}.",
            DoubleDownInvalidReason.SplitHand => 
                $"Player '{playerName}' cannot double down on split {handDescription} - rule disabled for split hands.",
            DoubleDownInvalidReason.GameState => 
                $"Player '{playerName}' cannot double down on {handDescription} - invalid game state.",
            DoubleDownInvalidReason.RuleDisabled => 
                $"Player '{playerName}' cannot double down on {handDescription} - double down is disabled.",
            DoubleDownInvalidReason.AlreadyDoubled => 
                $"Player '{playerName}' has already doubled down on {handDescription}.",
            DoubleDownInvalidReason.HandValue => 
                $"Player '{playerName}' cannot double down on {handDescription} - hand value not eligible.",
            _ => 
                $"Player '{playerName}' cannot double down on {handDescription} - invalid double down attempt."
        };
    }

    /// <summary>
    /// Creates an error message specifically for insufficient funds scenarios.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="requiredAmount">The required amount.</param>
    /// <param name="availableAmount">The available amount.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateInsufficientFundsMessage(string playerName, Money requiredAmount, Money availableAmount)
    {
        var shortfall = requiredAmount - availableAmount;
        return $"Player '{playerName}' cannot double down. Required: {requiredAmount}, Available: {availableAmount}, Shortfall: {shortfall}.";
    }
}

/// <summary>
/// Represents the different reasons why a double down operation might be invalid.
/// </summary>
public enum DoubleDownInvalidReason
{
    /// <summary>
    /// The hand has more or fewer than two cards (double down only allowed on initial two-card hands).
    /// </summary>
    WrongCardCount,

    /// <summary>
    /// The player does not have sufficient funds to double the bet.
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// The hand is already complete and cannot be doubled down.
    /// </summary>
    HandComplete,

    /// <summary>
    /// Double down is not allowed on split hands in the current game configuration.
    /// </summary>
    SplitHand,

    /// <summary>
    /// The game is not in a state that allows double down (e.g., not the player's turn).
    /// </summary>
    GameState,

    /// <summary>
    /// Double down is disabled in the current game configuration.
    /// </summary>
    RuleDisabled,

    /// <summary>
    /// The player has already doubled down on this hand.
    /// </summary>
    AlreadyDoubled,

    /// <summary>
    /// The hand value is not eligible for double down (some games restrict to 9, 10, 11).
    /// </summary>
    HandValue
}