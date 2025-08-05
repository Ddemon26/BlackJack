using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a hand of cards in a blackjack game with proper value calculation and game state checking.
/// Optimized for performance with caching and reduced allocations.
/// </summary>
public class Hand
{
    private readonly List<Card> _cards = new();
    private int _cachedValue = -1;
    private bool _valueCacheValid = false;
    private bool _isSplitHand = false;
    private bool _isComplete = false;

    /// <summary>
    /// Gets the cards in this hand as a read-only collection.
    /// </summary>
    public IReadOnlyList<Card> Cards => _cards.AsReadOnly();

    /// <summary>
    /// Gets the number of cards in this hand.
    /// </summary>
    public int CardCount => _cards.Count;

    /// <summary>
    /// Gets a value indicating whether this hand is a result of a split operation.
    /// </summary>
    public bool IsSplitHand => _isSplitHand;

    /// <summary>
    /// Gets a value indicating whether this hand is complete (no more cards can be added).
    /// </summary>
    public bool IsComplete => _isComplete;

    /// <summary>
    /// Adds a card to this hand.
    /// </summary>
    /// <param name="card">The card to add.</param>
    public void AddCard(Card card)
    {
        _cards.Add(card);
        InvalidateCache();
    }

    /// <summary>
    /// Clears all cards from this hand.
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
        _isSplitHand = false;
        _isComplete = false;
        InvalidateCache();
    }

    /// <summary>
    /// Marks this hand as a split hand.
    /// </summary>
    public void MarkAsSplitHand()
    {
        _isSplitHand = true;
    }

    /// <summary>
    /// Marks this hand as complete (no more cards can be added).
    /// </summary>
    public void MarkAsComplete()
    {
        _isComplete = true;
    }

    /// <summary>
    /// Determines if this hand can receive more cards.
    /// </summary>
    /// <returns>True if more cards can be added, false otherwise.</returns>
    public bool CanReceiveMoreCards()
    {
        if (_isComplete || IsBusted() || IsBlackjack())
        {
            return false;
        }

        // Split Aces can only receive one additional card
        if (_isSplitHand && _cards.Count == 2 && _cards[0].Rank == Rank.Ace)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the total value of this hand, handling Aces appropriately.
    /// Aces are valued at 11 unless that would cause a bust, in which case they are valued at 1.
    /// Uses caching to avoid recalculation when hand hasn't changed.
    /// </summary>
    /// <returns>The total value of the hand.</returns>
    public int GetValue()
    {
        if (_valueCacheValid)
            return _cachedValue;

        int total = 0;
        int aceCount = 0;

        // First pass: count all cards and track aces
        foreach (var card in _cards)
        {
            if (card.Rank == Rank.Ace)
            {
                aceCount++;
                total += 11; // Start with Ace as 11
            }
            else if (card.Rank >= Rank.Ten)
            {
                total += 10; // Face cards and 10s are worth 10
            }
            else
            {
                total += (int)card.Rank; // Number cards are worth their face value
            }
        }

        // Second pass: convert Aces from 11 to 1 if needed to avoid bust
        while (total > 21 && aceCount > 0)
        {
            total -= 10; // Convert one Ace from 11 to 1
            aceCount--;
        }

        _cachedValue = total;
        _valueCacheValid = true;
        return total;
    }

    /// <summary>
    /// Determines if this hand is busted (value over 21).
    /// </summary>
    /// <returns>True if the hand value is over 21, false otherwise.</returns>
    public bool IsBusted()
    {
        return GetValue() > 21;
    }

    /// <summary>
    /// Determines if this hand is a blackjack (21 with exactly 2 cards).
    /// </summary>
    /// <returns>True if this is a blackjack, false otherwise.</returns>
    public bool IsBlackjack()
    {
        return _cards.Count == 2 && GetValue() == 21;
    }

    /// <summary>
    /// Determines if this hand is a soft hand (contains an Ace valued at 11).
    /// A blackjack (natural 21 with 2 cards) is not considered soft since you cannot hit.
    /// </summary>
    /// <returns>True if this is a soft hand, false otherwise.</returns>
    public bool IsSoft()
    {
        // Blackjack is not considered soft since you can't hit
        if (IsBlackjack())
            return false;

        int total = 0;
        bool hasUsableAce = false;

        foreach (var card in _cards)
        {
            if (card.Rank == Rank.Ace)
            {
                if (total + 11 <= 21)
                {
                    hasUsableAce = true;
                    total += 11;
                }
                else
                {
                    total += 1;
                }
            }
            else if (card.Rank >= Rank.Ten)
            {
                total += 10;
            }
            else
            {
                total += (int)card.Rank;
            }
        }

        return hasUsableAce && total <= 21;
    }

    /// <summary>
    /// Returns a string representation of this hand showing all cards.
    /// </summary>
    /// <returns>A formatted string showing all cards in the hand.</returns>
    public override string ToString()
    {
        if (_cards.Count == 0)
            return "Empty hand";

        var sb = StringBuilderPool.Get();
        try
        {
            var first = true;
            foreach (var card in _cards)
            {
                if (!first)
                    sb.Append(", ");
                
                sb.Append(card.ToString());
                first = false;
            }

            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Invalidates the cached hand value, forcing recalculation on next access.
    /// </summary>
    private void InvalidateCache()
    {
        _valueCacheValid = false;
    }
}