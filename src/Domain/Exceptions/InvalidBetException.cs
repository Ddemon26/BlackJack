using System;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Exceptions;

/// <summary>
/// Exception thrown when a player attempts to place an invalid bet.
/// Provides detailed information about bet validation failures and recovery guidance.
/// </summary>
public class InvalidBetException : GameException
{
    /// <summary>
    /// Initializes a new instance of the InvalidBetException class for bet amount violations.
    /// </summary>
    /// <param name="attemptedBet">The bet amount that was attempted.</param>
    /// <param name="minimumBet">The minimum allowed bet amount.</param>
    /// <param name="maximumBet">The maximum allowed bet amount.</param>
    public InvalidBetException(Money attemptedBet, Money minimumBet, Money maximumBet)
        : base(CreateAmountMessage(attemptedBet, minimumBet, maximumBet))
    {
        AttemptedBet = attemptedBet;
        MinimumBet = minimumBet;
        MaximumBet = maximumBet;
        ViolationType = BetViolationType.Amount;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidBetException class for general bet violations.
    /// </summary>
    /// <param name="attemptedBet">The bet amount that was attempted.</param>
    /// <param name="violationType">The type of bet violation.</param>
    /// <param name="message">A custom error message.</param>
    public InvalidBetException(Money attemptedBet, BetViolationType violationType, string message)
        : base(message)
    {
        AttemptedBet = attemptedBet;
        ViolationType = violationType;
        MinimumBet = Money.Zero;
        MaximumBet = Money.Zero;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidBetException class with a custom message and inner exception.
    /// </summary>
    /// <param name="attemptedBet">The bet amount that was attempted.</param>
    /// <param name="violationType">The type of bet violation.</param>
    /// <param name="message">A custom error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public InvalidBetException(Money attemptedBet, BetViolationType violationType, string message, Exception innerException)
        : base(message, innerException)
    {
        AttemptedBet = attemptedBet;
        ViolationType = violationType;
        MinimumBet = Money.Zero;
        MaximumBet = Money.Zero;
    }

    /// <summary>
    /// Gets the bet amount that was attempted.
    /// </summary>
    public Money AttemptedBet { get; }

    /// <summary>
    /// Gets the minimum allowed bet amount.
    /// </summary>
    public Money MinimumBet { get; }

    /// <summary>
    /// Gets the maximum allowed bet amount.
    /// </summary>
    public Money MaximumBet { get; }

    /// <summary>
    /// Gets the type of bet violation.
    /// </summary>
    public BetViolationType ViolationType { get; }

    /// <summary>
    /// Gets recovery guidance based on the violation type.
    /// </summary>
    public string RecoveryGuidance => ViolationType switch
    {
        BetViolationType.Amount when AttemptedBet < MinimumBet => 
            $"Please place a bet of at least {MinimumBet}.",
        BetViolationType.Amount when AttemptedBet > MaximumBet => 
            $"Please place a bet of no more than {MaximumBet}.",
        BetViolationType.Currency => 
            "Please place your bet in the correct currency for this table.",
        BetViolationType.Negative => 
            "Bet amounts must be positive. Please enter a valid bet amount.",
        BetViolationType.Precision => 
            "Bet amounts cannot have more than 2 decimal places.",
        BetViolationType.DuplicateBet => 
            "You already have an active bet. Wait for the current round to complete before placing another bet.",
        BetViolationType.GameState => 
            "Bets cannot be placed at this time. Please wait for the betting phase.",
        _ => "Please check your bet amount and try again."
    };

    /// <summary>
    /// Creates a standard error message for bet amount violations.
    /// </summary>
    /// <param name="attemptedBet">The attempted bet amount.</param>
    /// <param name="minimumBet">The minimum bet amount.</param>
    /// <param name="maximumBet">The maximum bet amount.</param>
    /// <returns>A formatted error message.</returns>
    private static string CreateAmountMessage(Money attemptedBet, Money minimumBet, Money maximumBet)
    {
        if (attemptedBet < minimumBet)
            return $"Bet amount {attemptedBet} is below the minimum bet of {minimumBet}.";
        
        if (attemptedBet > maximumBet)
            return $"Bet amount {attemptedBet} exceeds the maximum bet of {maximumBet}.";
        
        return $"Invalid bet amount {attemptedBet}. Allowed range: {minimumBet} - {maximumBet}.";
    }
}

/// <summary>
/// Represents the different types of bet validation violations.
/// </summary>
public enum BetViolationType
{
    /// <summary>
    /// Bet amount is outside the allowed range.
    /// </summary>
    Amount,

    /// <summary>
    /// Bet currency does not match the table currency.
    /// </summary>
    Currency,

    /// <summary>
    /// Bet amount is negative or zero.
    /// </summary>
    Negative,

    /// <summary>
    /// Bet amount has too many decimal places.
    /// </summary>
    Precision,

    /// <summary>
    /// Player already has an active bet.
    /// </summary>
    DuplicateBet,

    /// <summary>
    /// Game is not in a state that allows betting.
    /// </summary>
    GameState
}