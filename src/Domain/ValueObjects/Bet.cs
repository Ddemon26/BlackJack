using System;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents a bet placed by a player in a blackjack game.
/// Encapsulates betting information including amount, type, timestamp, and settlement logic.
/// </summary>
/// <remarks>
/// This value object handles bet lifecycle management, payout calculations, and validation.
/// It supports different bet types and provides methods for calculating payouts based on game results.
/// </remarks>
public class Bet
{
    /// <summary>
    /// Initializes a new instance of the Bet class.
    /// </summary>
    /// <param name="amount">The amount of the bet.</param>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="type">The type of bet (defaults to Standard).</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    public Bet(Money amount, string playerName, BetType type = BetType.Standard)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bet amount must be positive.");

        Amount = amount;
        PlayerName = playerName.Trim();
        Type = type;
        PlacedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Gets the amount of the bet.
    /// </summary>
    public Money Amount { get; }

    /// <summary>
    /// Gets the type of bet.
    /// </summary>
    public BetType Type { get; }

    /// <summary>
    /// Gets the timestamp when the bet was placed.
    /// </summary>
    public DateTime PlacedAt { get; }

    /// <summary>
    /// Gets the name of the player who placed the bet.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets a value indicating whether the bet is still active (not yet settled).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the bet has been settled.
    /// </summary>
    public bool IsSettled => !IsActive;

    /// <summary>
    /// Settles the bet, marking it as no longer active.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the bet is already settled.</exception>
    public void Settle()
    {
        if (IsSettled)
            throw new InvalidOperationException("Bet has already been settled.");

        IsActive = false;
    }

    /// <summary>
    /// Calculates the payout for this bet based on the game result.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="blackjackMultiplier">The multiplier for blackjack payouts (typically 1.5 for 3:2 odds).</param>
    /// <returns>The payout amount. Returns zero for losses and pushes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when blackjackMultiplier is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the bet is already settled.</exception>
    public Money CalculatePayout(GameResult result, decimal blackjackMultiplier = 1.5m)
    {
        if (blackjackMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(blackjackMultiplier), 
                "Blackjack multiplier must be positive.");

        if (IsSettled)
            throw new InvalidOperationException("Cannot calculate payout for a settled bet.");

        return result switch
        {
            GameResult.Win => Amount, // 1:1 payout - player gets their bet back plus equal winnings
            GameResult.Blackjack => Amount * blackjackMultiplier, // 3:2 payout - player gets bet back plus 1.5x winnings
            GameResult.Push => Money.Zero, // No payout for ties
            GameResult.Lose => Money.Zero, // No payout for losses
            _ => throw new ArgumentException($"Unknown game result: {result}", nameof(result))
        };
    }

    /// <summary>
    /// Calculates the total return (original bet plus payout) for this bet based on the game result.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="blackjackMultiplier">The multiplier for blackjack payouts (typically 1.5 for 3:2 odds).</param>
    /// <returns>The total return amount. For losses, returns zero. For pushes, returns the original bet amount.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when blackjackMultiplier is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the bet is already settled.</exception>
    public Money CalculateTotalReturn(GameResult result, decimal blackjackMultiplier = 1.5m)
    {
        if (IsSettled)
            throw new InvalidOperationException("Cannot calculate total return for a settled bet.");

        return result switch
        {
            GameResult.Win => Amount + CalculatePayout(result, blackjackMultiplier), // Original bet + payout
            GameResult.Blackjack => Amount + CalculatePayout(result, blackjackMultiplier), // Original bet + blackjack payout
            GameResult.Push => Amount, // Return original bet
            GameResult.Lose => Money.Zero, // Lose everything
            _ => throw new ArgumentException($"Unknown game result: {result}", nameof(result))
        };
    }

    /// <summary>
    /// Creates a double down bet based on this bet.
    /// </summary>
    /// <returns>A new bet with double the amount for double down scenarios.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the bet is already settled or is not a standard bet.</exception>
    public Bet CreateDoubleDownBet()
    {
        if (IsSettled)
            throw new InvalidOperationException("Cannot create double down bet from a settled bet.");

        if (Type != BetType.Standard)
            throw new InvalidOperationException("Can only create double down bet from a standard bet.");

        return new Bet(Amount * 2, PlayerName, BetType.DoubleDown);
    }

    /// <summary>
    /// Creates a split bet based on this bet.
    /// </summary>
    /// <returns>A new bet with the same amount for split scenarios.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the bet is already settled or is not a standard bet.</exception>
    public Bet CreateSplitBet()
    {
        if (IsSettled)
            throw new InvalidOperationException("Cannot create split bet from a settled bet.");

        if (Type != BetType.Standard)
            throw new InvalidOperationException("Can only create split bet from a standard bet.");

        return new Bet(Amount, PlayerName, BetType.Split);
    }

    /// <summary>
    /// Returns a string representation of the bet.
    /// </summary>
    /// <returns>A formatted string showing the bet details.</returns>
    public override string ToString()
    {
        var status = IsActive ? "Active" : "Settled";
        var typeText = Type == BetType.Standard ? "" : $" ({Type})";
        return $"{PlayerName}: {Amount}{typeText} - {status}";
    }

    /// <summary>
    /// Determines equality based on amount, player name, type, and placement time.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Bet other)
            return false;

        return Amount.Equals(other.Amount) &&
               PlayerName.Equals(other.PlayerName, StringComparison.OrdinalIgnoreCase) &&
               Type == other.Type &&
               PlacedAt.Equals(other.PlacedAt);
    }

    /// <summary>
    /// Gets the hash code based on bet properties.
    /// </summary>
    /// <returns>The hash code for this bet.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, PlayerName.ToLowerInvariant(), Type, PlacedAt);
    }
}