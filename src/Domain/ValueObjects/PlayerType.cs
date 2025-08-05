namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the type of player in a blackjack game.
/// </summary>
public enum PlayerType
{
    /// <summary>
    /// A human player who makes their own decisions.
    /// </summary>
    Human,

    /// <summary>
    /// The dealer who follows fixed rules (hit on 16, stand on 17).
    /// </summary>
    Dealer
}