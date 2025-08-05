using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a single hand belonging to a player, including the hand and its associated bet.
/// Used to support multiple hands per player in split scenarios.
/// </summary>
public class PlayerHand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerHand"/> class.
    /// </summary>
    /// <param name="hand">The hand of cards.</param>
    /// <param name="bet">The bet associated with this hand.</param>
    /// <param name="isActive">Whether this hand is currently active for play.</param>
    /// <exception cref="ArgumentNullException">Thrown when hand or bet is null.</exception>
    public PlayerHand(Hand hand, Bet bet, bool isActive = true)
    {
        Hand = hand ?? throw new ArgumentNullException(nameof(hand));
        Bet = bet ?? throw new ArgumentNullException(nameof(bet));
        IsActive = isActive;
        IsComplete = false;
    }

    /// <summary>
    /// Gets the hand of cards.
    /// </summary>
    public Hand Hand { get; }

    /// <summary>
    /// Gets the bet associated with this hand.
    /// </summary>
    public Bet Bet { get; }

    /// <summary>
    /// Gets a value indicating whether this hand is currently active for play.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this hand is complete (no more actions can be taken).
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this hand is busted.
    /// </summary>
    public bool IsBusted => Hand.IsBusted();

    /// <summary>
    /// Gets a value indicating whether this hand is blackjack.
    /// </summary>
    public bool IsBlackjack => Hand.IsBlackjack();

    /// <summary>
    /// Gets a value indicating whether this hand is a split hand.
    /// </summary>
    public bool IsSplitHand => Hand.IsSplitHand;

    /// <summary>
    /// Gets the current value of this hand.
    /// </summary>
    public int HandValue => Hand.GetValue();

    /// <summary>
    /// Gets the number of cards in this hand.
    /// </summary>
    public int CardCount => Hand.CardCount;

    /// <summary>
    /// Marks this hand as complete (no more actions can be taken).
    /// </summary>
    public void MarkAsComplete()
    {
        IsComplete = true;
        IsActive = false;
    }

    /// <summary>
    /// Marks this hand as inactive but not necessarily complete.
    /// </summary>
    public void MarkAsInactive()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates this hand for play.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the hand is already complete.</exception>
    public void Reactivate()
    {
        if (IsComplete)
        {
            throw new InvalidOperationException("Cannot reactivate a complete hand.");
        }

        IsActive = true;
    }

    /// <summary>
    /// Determines if this hand can receive more cards.
    /// </summary>
    /// <returns>True if more cards can be added, false otherwise.</returns>
    public bool CanReceiveMoreCards()
    {
        if (IsComplete || !IsActive)
        {
            return false;
        }

        return Hand.CanReceiveMoreCards();
    }

    /// <summary>
    /// Adds a card to this hand.
    /// </summary>
    /// <param name="card">The card to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when the hand cannot receive more cards.</exception>
    public void AddCard(Card card)
    {
        if (!CanReceiveMoreCards())
        {
            throw new InvalidOperationException("This hand cannot receive more cards.");
        }

        Hand.AddCard(card);

        // Auto-complete hand if it's busted, blackjack, or split Aces with 2 cards
        if (Hand.IsBusted() || Hand.IsBlackjack() || 
            (Hand.IsSplitHand && Hand.CardCount == 2 && Hand.Cards[0].Rank == Rank.Ace))
        {
            MarkAsComplete();
        }
    }

    /// <summary>
    /// Determines if this hand can be split.
    /// </summary>
    /// <returns>True if the hand can be split, false otherwise.</returns>
    public bool CanSplit()
    {
        if (!IsActive || IsComplete || Hand.CardCount != 2)
        {
            return false;
        }

        var cards = Hand.Cards;
        return cards[0].Rank == cards[1].Rank;
    }

    /// <summary>
    /// Determines if this hand can be doubled down.
    /// </summary>
    /// <returns>True if the hand can be doubled down, false otherwise.</returns>
    public bool CanDoubleDown()
    {
        if (!IsActive || IsComplete || Hand.CardCount != 2)
        {
            return false;
        }

        return !Hand.IsBusted() && !Hand.IsBlackjack();
    }

    /// <summary>
    /// Returns a string representation of this player hand.
    /// </summary>
    /// <returns>A formatted string showing the hand and bet details.</returns>
    public override string ToString()
    {
        var status = IsComplete ? "Complete" : (IsActive ? "Active" : "Inactive");
        var splitText = IsSplitHand ? " (Split)" : "";
        return $"Hand: {Hand} (Value: {HandValue}), Bet: {Bet.Amount}, Status: {status}{splitText}";
    }

    /// <summary>
    /// Determines equality based on hand and bet.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not PlayerHand other)
            return false;

        return Hand.Equals(other.Hand) && Bet.Equals(other.Bet);
    }

    /// <summary>
    /// Gets the hash code based on hand and bet.
    /// </summary>
    /// <returns>The hash code for this player hand.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Hand, Bet);
    }
}