using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a shoe containing multiple decks of cards, commonly used in casino blackjack games.
/// </summary>
public class Shoe : IShoe
{
    private readonly List<Card> _cards;
    private readonly int _deckCount;
    private readonly IRandomProvider _randomProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Shoe"/> class with the specified number of decks.
    /// </summary>
    /// <param name="deckCount">The number of standard 52-card decks to include in the shoe.</param>
    /// <param name="randomProvider">The random provider to use for shuffling.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when deckCount is less than 1.</exception>
    public Shoe(int deckCount, IRandomProvider randomProvider)
    {
        if (deckCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deckCount), "Deck count must be at least 1.");
        }

        _deckCount = deckCount;
        _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        _cards = new List<Card>();

        Reset();
    }

    /// <summary>
    /// Gets the number of decks in the shoe.
    /// </summary>
    public int DeckCount => _deckCount;

    /// <summary>
    /// Gets the number of cards remaining in the shoe.
    /// </summary>
    public int RemainingCards => _cards.Count;

    /// <summary>
    /// Gets a value indicating whether the shoe is empty.
    /// </summary>
    public bool IsEmpty => _cards.Count == 0;

    /// <summary>
    /// Draws a card from the shoe.
    /// </summary>
    /// <returns>The drawn card.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the shoe is empty.</exception>
    public Card Draw()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Cannot draw from an empty shoe.");
        }

        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    /// <summary>
    /// Shuffles all cards in the shoe using the Fisher-Yates shuffle algorithm.
    /// </summary>
    public void Shuffle()
    {
        _randomProvider.Shuffle(_cards);
    }

    /// <summary>
    /// Resets the shoe to its initial state with all cards from all decks and shuffles them.
    /// </summary>
    public void Reset()
    {
        _cards.Clear();

        // Add all cards from each deck
        for (int i = 0; i < _deckCount; i++)
        {
            _cards.AddRange(CardExtensions.CreateStandardDeck());
        }

        Shuffle();
    }

    /// <summary>
    /// Gets the percentage of cards remaining in the shoe.
    /// </summary>
    /// <returns>A value between 0.0 and 1.0 representing the percentage of cards remaining.</returns>
    public double GetRemainingPercentage()
    {
        int totalCards = _deckCount * 52;
        return totalCards == 0 ? 0.0 : (double)RemainingCards / totalCards;
    }

    /// <summary>
    /// Determines if the shoe needs to be reshuffled based on a penetration threshold.
    /// </summary>
    /// <param name="penetrationThreshold">The threshold (0.0 to 1.0) below which reshuffling is recommended.</param>
    /// <returns>True if the remaining percentage is below the threshold, false otherwise.</returns>
    public bool NeedsReshuffle(double penetrationThreshold = 0.25)
    {
        return GetRemainingPercentage() < penetrationThreshold;
    }

    /// <summary>
    /// Returns a string representation of the shoe showing the number of decks and remaining cards.
    /// </summary>
    /// <returns>A string describing the shoe state.</returns>
    public override string ToString()
    {
        return $"Shoe with {DeckCount} decks, {RemainingCards} cards remaining ({GetRemainingPercentage():P1})";
    }
}