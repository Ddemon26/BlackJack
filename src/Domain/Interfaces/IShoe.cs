using GroupProject.Domain.Events;
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

    /// <summary>
    /// Gets the percentage of cards remaining in the shoe.
    /// </summary>
    /// <returns>A value between 0.0 and 1.0 representing the percentage of cards remaining.</returns>
    double GetRemainingPercentage();

    /// <summary>
    /// Determines if the shoe needs to be reshuffled based on a penetration threshold.
    /// </summary>
    /// <param name="penetrationThreshold">The threshold (0.0 to 1.0) below which reshuffling is recommended.</param>
    /// <returns>True if the remaining percentage is below the threshold, false otherwise.</returns>
    bool NeedsReshuffle(double penetrationThreshold = 0.25);

    /// <summary>
    /// Event raised when the shoe needs to be reshuffled.
    /// </summary>
    event EventHandler<ShoeReshuffleEventArgs>? ReshuffleNeeded;

    /// <summary>
    /// Event raised when the shoe has been reshuffled.
    /// </summary>
    event EventHandler<ShoeReshuffleEventArgs>? Reshuffled;
}