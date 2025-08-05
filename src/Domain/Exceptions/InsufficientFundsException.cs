using System;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a player attempts to place a bet but has insufficient funds in their bankroll.
/// Provides detailed information about the required amount and available funds for recovery guidance.
/// </summary>
public class InsufficientFundsException : GameException
{
    /// <summary>
    /// Initializes a new instance of the InsufficientFundsException class.
    /// </summary>
    /// <param name="playerName">The name of the player with insufficient funds.</param>
    /// <param name="requiredAmount">The amount required for the bet.</param>
    /// <param name="availableAmount">The amount currently available in the player's bankroll.</param>
    public InsufficientFundsException(string playerName, Money requiredAmount, Money availableAmount)
        : base(CreateMessage(playerName, requiredAmount, availableAmount))
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        RequiredAmount = requiredAmount;
        AvailableAmount = availableAmount;
    }

    /// <summary>
    /// Initializes a new instance of the InsufficientFundsException class with a custom message.
    /// </summary>
    /// <param name="playerName">The name of the player with insufficient funds.</param>
    /// <param name="requiredAmount">The amount required for the bet.</param>
    /// <param name="availableAmount">The amount currently available in the player's bankroll.</param>
    /// <param name="message">A custom error message.</param>
    public InsufficientFundsException(string playerName, Money requiredAmount, Money availableAmount, string message)
        : base(message)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        RequiredAmount = requiredAmount;
        AvailableAmount = availableAmount;
    }

    /// <summary>
    /// Initializes a new instance of the InsufficientFundsException class with a custom message and inner exception.
    /// </summary>
    /// <param name="playerName">The name of the player with insufficient funds.</param>
    /// <param name="requiredAmount">The amount required for the bet.</param>
    /// <param name="availableAmount">The amount currently available in the player's bankroll.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InsufficientFundsException(string playerName, Money requiredAmount, Money availableAmount, string message, Exception innerException)
        : base(message, innerException)
    {
        PlayerName = playerName ?? throw new ArgumentNullException(nameof(playerName));
        RequiredAmount = requiredAmount;
        AvailableAmount = availableAmount;
    }

    /// <summary>
    /// Gets the name of the player with insufficient funds.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets the amount required for the bet.
    /// </summary>
    public Money RequiredAmount { get; }

    /// <summary>
    /// Gets the amount currently available in the player's bankroll.
    /// </summary>
    public Money AvailableAmount { get; }

    /// <summary>
    /// Gets the shortfall amount (required - available).
    /// </summary>
    public Money Shortfall => RequiredAmount - AvailableAmount;

    /// <summary>
    /// Gets recovery guidance for the player.
    /// </summary>
    public string RecoveryGuidance => 
        $"You need an additional {Shortfall} to place this bet. " +
        $"Consider placing a smaller bet (maximum: {AvailableAmount}) or adding funds to your bankroll.";

    /// <summary>
    /// Creates a standard error message for insufficient funds.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="requiredAmount">The required amount.</param>
    /// <param name="availableAmount">The available amount.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateMessage(string playerName, Money requiredAmount, Money availableAmount)
    {
        return $"Player '{playerName}' has insufficient funds. Required: {requiredAmount}, Available: {availableAmount}, Shortfall: {requiredAmount - availableAmount}.";
    }
}