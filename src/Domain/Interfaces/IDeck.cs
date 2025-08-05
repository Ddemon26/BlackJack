using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for a deck of playing cards.
/// </summary>
public interface IDeck
{
    /// <summary>
    /// Draws a card from the deck.
    /// </summary>
    /// <returns>The drawn card.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the deck is empty.</exception>
    Card Draw();

    /// <summary>
    /// Shuffles the deck using a randomization algorithm.
    /// </summary>
    void Shuffle();

    /// <summary>
    /// Gets the number of cards remaining in the deck.
    /// </summary>
    int RemainingCards { get; }

    /// <summary>
    /// Gets a value indicating whether the deck is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Resets the deck to its initial state with all cards.
    /// </summary>
    void Reset();
}