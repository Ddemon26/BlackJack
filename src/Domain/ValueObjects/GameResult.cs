namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the possible outcomes of a blackjack game for a player.
/// </summary>
public enum GameResult
{
    /// <summary>
    /// Player wins the hand
    /// </summary>
    Win,
    
    /// <summary>
    /// Player loses the hand
    /// </summary>
    Lose,
    
    /// <summary>
    /// Tie between player and dealer
    /// </summary>
    Push,
    
    /// <summary>
    /// Player wins with a natural blackjack (21 with 2 cards)
    /// </summary>
    Blackjack
}