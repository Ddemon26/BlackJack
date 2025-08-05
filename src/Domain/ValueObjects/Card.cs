namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents a playing card with a suit and rank.
/// </summary>
public readonly record struct Card(Suit Suit, Rank Rank)
{
    /// <summary>
    /// Returns a string representation of this card.
    /// </summary>
    /// <returns>A formatted string showing the rank and suit of the card.</returns>
    public override string ToString()
    {
        string rankName = Rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => ((int)Rank).ToString()
        };

        string suitName = Suit switch
        {
            Suit.Spades => "Spades",
            Suit.Hearts => "Hearts",
            Suit.Diamonds => "Diamonds",
            Suit.Clubs => "Clubs",
            _ => Suit.ToString()
        };

        return $"{rankName} of {suitName}";
    }
}

