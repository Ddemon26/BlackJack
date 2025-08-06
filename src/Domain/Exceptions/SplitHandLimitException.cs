using System;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a player attempts to split beyond the maximum allowed number of splits.
/// Provides information about current and maximum split limits for recovery guidance.
/// </summary>
public class SplitHandLimitException : GameException
{
    /// <summary>
    /// Initializes a new instance of the SplitHandLimitException class.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="currentSplits">The current number of splits the player has.</param>
    /// <param name="maximumSplits">The maximum number of splits allowed.</param>
    public SplitHandLimitException(string playerName, int currentSplits, int maximumSplits)
        : base(CreateMessage(playerName, currentSplits, maximumSplits))
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        CurrentSplits = currentSplits;
        MaximumSplits = maximumSplits;
    }

    /// <summary>
    /// Initializes a new instance of the SplitHandLimitException class with a custom message.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="currentSplits">The current number of splits the player has.</param>
    /// <param name="maximumSplits">The maximum number of splits allowed.</param>
    /// <param name="message">A custom error message.</param>
    public SplitHandLimitException(string playerName, int currentSplits, int maximumSplits, string message)
        : base(message)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        CurrentSplits = currentSplits;
        MaximumSplits = maximumSplits;
    }

    /// <summary>
    /// Initializes a new instance of the SplitHandLimitException class with a custom message and inner exception.
    /// </summary>
    /// <param name="playerName">The name of the player attempting the split.</param>
    /// <param name="currentSplits">The current number of splits the player has.</param>
    /// <param name="maximumSplits">The maximum number of splits allowed.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public SplitHandLimitException(string playerName, int currentSplits, int maximumSplits, string message, Exception innerException)
        : base(message, innerException)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        CurrentSplits = currentSplits;
        MaximumSplits = maximumSplits;
    }

    /// <summary>
    /// Gets the name of the player attempting the split.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets the current number of splits the player has.
    /// </summary>
    public int CurrentSplits { get; }

    /// <summary>
    /// Gets the maximum number of splits allowed.
    /// </summary>
    public int MaximumSplits { get; }

    /// <summary>
    /// Gets the number of hands the player currently has (splits + 1).
    /// </summary>
    public int CurrentHandCount => CurrentSplits + 1;

    /// <summary>
    /// Gets the maximum number of hands allowed (max splits + 1).
    /// </summary>
    public int MaximumHandCount => MaximumSplits + 1;

    /// <summary>
    /// Gets recovery guidance for the player.
    /// </summary>
    public string RecoveryGuidance => MaximumSplits switch
    {
        0 => "Splitting is not allowed in this game. Choose Hit, Stand, or Double Down instead.",
        1 => $"You can only split once per round. You already have {CurrentHandCount} hands. Continue playing your existing hands.",
        _ => $"You have reached the maximum of {MaximumSplits} splits ({MaximumHandCount} hands total). Continue playing your existing hands."
    };

    /// <summary>
    /// Creates a standard error message for split limit violations.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="currentSplits">The current number of splits.</param>
    /// <param name="maximumSplits">The maximum number of splits allowed.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(string playerName, int currentSplits, int maximumSplits)
    {
        if (maximumSplits == 0)
        {
            return $"Player '{playerName}' cannot split - splitting is disabled in this game.";
        }

        var currentHands = currentSplits + 1;
        var maxHands = maximumSplits + 1;

        return $"Player '{playerName}' has reached the split limit. Current hands: {currentHands}, Maximum allowed: {maxHands}.";
    }
}