using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a standard 52-card deck of playing cards.
/// </summary>
public class Deck : IDeck
{
    private readonly List<Card> _cards;
    private readonly List<Card> _originalCards;
    private readonly IRandomProvider _randomProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Deck"/> class with a standard 52-card deck.
    /// </summary>
    /// <param name="randomProvider">The random provider to use for shuffling.</param>
    public Deck(IRandomProvider randomProvider)
    {
        _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        _originalCards = CardExtensions.CreateStandardDeck().ToList();
        _cards = new List<Card>(_originalCards);
        Shuffle(); // Shuffle the deck upon creation
    }

    /// <summary>
    /// Gets the number of cards remaining in the deck.
    /// </summary>
    public int RemainingCards => _cards.Count;

    /// <summary>
    /// Gets a value indicating whether the deck is empty.
    /// </summary>
    public bool IsEmpty => _cards.Count == 0;

    /// <summary>
    /// Draws a card from the top of the deck.
    /// </summary>
    /// <returns>The drawn card.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the deck is empty.</exception>
    public Card Draw()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Cannot draw from an empty deck.");
        }

        var card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    /// <summary>
    /// Shuffles the deck using the Fisher-Yates shuffle algorithm.
    /// </summary>
    public void Shuffle()
    {
        _randomProvider.Shuffle(_cards);
    }

    /// <summary>
    /// Resets the deck to its initial state with all 52 cards and shuffles it.
    /// Optimized to reuse existing capacity when possible.
    /// </summary>
    public void Reset()
    {
        _cards.Clear();
        
        // Ensure capacity to avoid reallocations
        if (_cards.Capacity < _originalCards.Count)
        {
            _cards.Capacity = _originalCards.Count;
        }
        
        _cards.AddRange(_originalCards);
        Shuffle();
    }

    /// <summary>
    /// Returns a string representation of the deck showing the number of remaining cards.
    /// </summary>
    /// <returns>A string describing the deck state.</returns>
    public override string ToString()
    {
        return $"Deck with {RemainingCards} cards remaining";
    }
}