using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for a shoe containing multiple decks of cards.
/// </summary>
public interface IShoe
{
    /// <summary>
    /// Draws a card from the shoe.
    /// </summary>
    /// <returns>The drawn card.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the shoe is empty.</exception>
    Card Draw();

    /// <summary>
    /// Shuffles all cards in the shoe.
    /// </summary>
    void Shuffle();

    /// <summary>
    /// Gets the number of cards remaining in the shoe.
    /// </summary>
    int RemainingCards { get; }

    /// <summary>
    /// Gets a value indicating whether the shoe is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the number of decks in the shoe.
    /// </summary>
    int DeckCount { get; }

    /// <summary>
    /// Resets the shoe to its initial state with all cards from all decks.
    /// </summary>
    void Reset();
}