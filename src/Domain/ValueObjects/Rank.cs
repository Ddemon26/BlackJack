namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the rank of a playing card.
/// </summary>
public enum Rank
{
    /// <summary>
    /// Ace (can be valued as 1 or 11 in blackjack).
    /// </summary>
    Ace = 1,

    /// <summary>
    /// Two.
    /// </summary>
    Two = 2,

    /// <summary>
    /// Three.
    /// </summary>
    Three = 3,

    /// <summary>
    /// Four.
    /// </summary>
    Four = 4,

    /// <summary>
    /// Five.
    /// </summary>
    Five = 5,

    /// <summary>
    /// Six.
    /// </summary>
    Six = 6,

    /// <summary>
    /// Seven.
    /// </summary>
    Seven = 7,

    /// <summary>
    /// Eight.
    /// </summary>
    Eight = 8,

    /// <summary>
    /// Nine.
    /// </summary>
    Nine = 9,

    /// <summary>
    /// Ten.
    /// </summary>
    Ten = 10,

    /// <summary>
    /// Jack (valued as 10 in blackjack).
    /// </summary>
    Jack = 11,

    /// <summary>
    /// Queen (valued as 10 in blackjack).
    /// </summary>
    Queen = 12,

    /// <summary>
    /// King (valued as 10 in blackjack).
    /// </summary>
    King = 13
}