using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a player in a blackjack game, including both human players and the dealer.
/// </summary>
public class Player
{
    private readonly Hand _hand;

    /// <summary>
    /// Initializes a new instance of the <see cref="Player"/> class.
    /// </summary>
    /// <param name="name">The name of the player.</param>
    /// <param name="playerType">The type of player (Human or Dealer).</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public Player(string name, PlayerType playerType = PlayerType.Human)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name cannot be null or whitespace.", nameof(name));
        }

        Name = name.Trim();
        Type = playerType;
        _hand = new Hand();
    }

    /// <summary>
    /// Gets the name of the player.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of player (Human or Dealer).
    /// </summary>
    public PlayerType Type { get; }

    /// <summary>
    /// Gets the player's current hand.
    /// </summary>
    public Hand Hand => _hand;

    /// <summary>
    /// Gets a value indicating whether this player is the dealer.
    /// </summary>
    public bool IsDealer => Type == PlayerType.Dealer;

    /// <summary>
    /// Gets a value indicating whether this player is a human player.
    /// </summary>
    public bool IsHuman => Type == PlayerType.Human;

    /// <summary>
    /// Adds a card to the player's hand.
    /// </summary>
    /// <param name="card">The card to add.</param>
    public void AddCard(Card card)
    {
        _hand.AddCard(card);
    }

    /// <summary>
    /// Clears the player's hand for a new game.
    /// </summary>
    public void ClearHand()
    {
        _hand.Clear();
    }

    /// <summary>
    /// Gets the current value of the player's hand.
    /// </summary>
    /// <returns>The total value of the hand.</returns>
    public int GetHandValue()
    {
        return _hand.GetValue();
    }

    /// <summary>
    /// Determines if the player's hand is busted (over 21).
    /// </summary>
    /// <returns>True if the hand is busted, false otherwise.</returns>
    public bool IsBusted()
    {
        return _hand.IsBusted();
    }

    /// <summary>
    /// Determines if the player has blackjack (21 with exactly 2 cards).
    /// </summary>
    /// <returns>True if the player has blackjack, false otherwise.</returns>
    public bool HasBlackjack()
    {
        return _hand.IsBlackjack();
    }

    /// <summary>
    /// Determines if the player's hand is soft (contains an Ace valued at 11).
    /// </summary>
    /// <returns>True if the hand is soft, false otherwise.</returns>
    public bool HasSoftHand()
    {
        return _hand.IsSoft();
    }

    /// <summary>
    /// Gets the number of cards in the player's hand.
    /// </summary>
    /// <returns>The number of cards in the hand.</returns>
    public int GetCardCount()
    {
        return _hand.CardCount;
    }

    /// <summary>
    /// Returns a string representation of the player showing their name, type, and hand.
    /// </summary>
    /// <returns>A formatted string representation of the player.</returns>
    public override string ToString()
    {
        var typeString = IsDealer ? "Dealer" : "Player";
        return $"{typeString} {Name}: {_hand} (Value: {GetHandValue()})";
    }

    /// <summary>
    /// Returns a string representation of the player with their hand hidden (for dealer's hole card).
    /// </summary>
    /// <param name="hideFirstCard">If true, hides the first card in the hand.</param>
    /// <returns>A formatted string with the specified card hidden.</returns>
    public string ToStringWithHiddenCard(bool hideFirstCard = true)
    {
        if (!hideFirstCard || _hand.CardCount == 0)
        {
            return ToString();
        }

        var typeString = IsDealer ? "Dealer" : "Player";
        var cards = _hand.Cards.ToList();
        
        if (cards.Count == 0)
        {
            return $"{typeString} {Name}: Empty hand";
        }

        var visibleCards = hideFirstCard ? cards.Skip(1) : cards.Take(cards.Count - 1);
        var hiddenCardText = hideFirstCard ? "[Hidden]" : "[Hidden]";
        
        if (hideFirstCard)
        {
            var visibleCardsText = visibleCards.Any() ? string.Join(", ", visibleCards) : "";
            var displayText = visibleCardsText.Length > 0 ? $"{hiddenCardText}, {visibleCardsText}" : hiddenCardText;
            return $"{typeString} {Name}: {displayText}";
        }
        else
        {
            var visibleCardsText = visibleCards.Any() ? string.Join(", ", visibleCards) : "";
            var displayText = visibleCardsText.Length > 0 ? $"{visibleCardsText}, {hiddenCardText}" : hiddenCardText;
            return $"{typeString} {Name}: {displayText}";
        }
    }

    /// <summary>
    /// Determines equality based on name and type.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Player other)
            return false;

        return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Type == other.Type;
    }

    /// <summary>
    /// Gets the hash code based on name and type.
    /// </summary>
    /// <returns>The hash code for this player.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name.ToLowerInvariant(), Type);
    }
}