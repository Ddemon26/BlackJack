namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Extension methods and utilities for working with cards.
/// </summary>
public static class CardExtensions
{
    /// <summary>
    /// Creates a standard 52-card deck.
    /// </summary>
    /// <returns>A collection of all 52 cards in a standard deck</returns>
    public static IEnumerable<Card> CreateStandardDeck()
    {
        foreach (var suit in Enum.GetValues<Suit>())
        {
            foreach (var rank in Enum.GetValues<Rank>())
            {
                yield return new Card(suit, rank);
            }
        }
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
    /// </summary>
    /// <param name="cards">The cards to calculate value for</param>
    /// <returns>The optimal blackjack value for the hand</returns>
    public static int GetBlackjackValue(this IEnumerable<Card> cards)
    {
        var cardList = cards.ToList();
        int total = 0;
        int aces = 0;
        
        // First pass: count aces and add other card values
        foreach (var card in cardList)
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