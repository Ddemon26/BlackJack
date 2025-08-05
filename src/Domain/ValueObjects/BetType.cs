namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the different types of bets that can be placed in a blackjack game.
/// </summary>
public enum BetType
{
    /// <summary>
    /// A standard bet placed at the beginning of a hand.
    /// </summary>
    Standard,

    /// <summary>
    /// A double down bet where the player doubles their original bet and receives exactly one more card.
    /// </summary>
    DoubleDown,

    /// <summary>
    /// A split bet placed when a player splits a pair, creating a second hand with an equal bet.
    /// </summary>
    Split
}