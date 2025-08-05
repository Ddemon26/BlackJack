namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Extension methods and utilities for working with cards.
/// Optimized with caching for frequently used operations.
/// </summary>
public static class CardExtensions
{
    // Cache the standard deck to avoid repeated enumeration
    private static readonly Card[] _standardDeck = CreateInitialStandardDeck();

    /// <summary>
    /// Creates a standard 52-card deck.
    /// Uses cached array for optimal performance.
    /// </summary>
    /// <returns>A collection of all 52 cards in a standard deck</returns>
    public static IEnumerable<Card> CreateStandardDeck()
    {
        return _standardDeck;
    }

    /// <summary>
    /// Creates a standard 52-card deck as an array for maximum performance.
    /// </summary>
    /// <returns>An array of all 52 cards in a standard deck</returns>
    public static Card[] CreateStandardDeckArray()
    {
        return (Card[])_standardDeck.Clone();
    }

    /// <summary>
    /// Internal method to create the initial standard deck array.
    /// </summary>
    /// <returns>An array of all 52 cards in a standard deck</returns>
    private static Card[] CreateInitialStandardDeck()
    {
        var cards = new Card[52];
        var index = 0;
        
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                cards[index++] = new Card(suit, rank);
            }
        }
        
        return cards;
    }
    
    /// <summary>
    /// Validates a collection of cards to ensure they are all valid.
    /// </summary>
    /// <param name="cards">The cards to validate</param>
    /// <returns>True if all cards are valid, false otherwise</returns>
    public static bool AreAllValid(this IEnumerable<Card> cards)
    {
        return cards.All(card => Enum.IsDefined(card.Suit) && Enum.IsDefined(card.Rank));
    }
    
    /// <summary>
    /// Gets the total blackjack value of a collection of cards.
    /// Properly handles Ace values (1 or 11) to get the best possible hand value.
    /// Optimized to avoid unnecessary allocations.
    /// </summary>
    /// <param name="cards">The cards to calculate value for</param>
    /// <returns>The optimal blackjack value for the hand</returns>
    public static int GetBlackjackValue(this IEnumerable<Card> cards)
    {
        int total = 0;
        int aces = 0;
        
        // Single pass: count aces and add other card values
        foreach (var card in cards)
        {
            if (card.Rank == Rank.Ace)
            {
                aces++;
                total += 11; // Start with Ace as 11
            }
            else if (card.Rank >= Rank.Jack) // Face cards
            {
                total += 10;
            }
            else
            {
                total += (int)card.Rank;
            }
        }
        
        // Adjust for Aces: convert from 11 to 1 as needed
        while (total > 21 && aces > 0)
        {
            total -= 10; // Convert one Ace from 11 to 1
            aces--;
        }
        
        return total;
    }
    
    /// <summary>
    /// Formats a collection of cards for display.
    /// </summary>
    /// <param name="cards">The cards to format</param>
    /// <param name="separator">The separator to use between cards</param>
    /// <returns>A formatted string representation of the cards</returns>
    public static string FormatCards(this IEnumerable<Card> cards, string separator = ", ")
    {
        return string.Join(separator, cards.Select(card => card.ToString()));
    }
    
    /// <summary>
    /// Formats a collection of cards for detailed display.
    /// </summary>
    /// <param name="cards">The cards to format</param>
    /// <param name="separator">The separator to use between cards</param>
    /// <returns>A detailed formatted string representation of the cards</returns>
    public static string FormatCardsDetailed(this IEnumerable<Card> cards, string separator = ", ")
    {
        return string.Join(separator, cards.Select(card => card.ToString()));
    }
}