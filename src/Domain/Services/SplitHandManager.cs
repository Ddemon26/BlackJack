using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Services;

/// <summary>
/// Manages split hand operations and state for blackjack games.
/// Handles the creation, validation, and management of split hands.
/// </summary>
public class SplitHandManager
{
    /// <summary>
    /// Determines if a hand can be split.
    /// </summary>
    /// <param name="hand">The hand to check for split eligibility.</param>
    /// <returns>True if the hand can be split, false otherwise.</returns>
    public bool CanSplit(Hand hand)
    {
        // Must have exactly 2 cards
        if (hand.CardCount != 2)
        {
            return false;
        }

        // Cards must have the same rank
        var cards = hand.Cards;
        return cards[0].Rank == cards[1].Rank;
    }

    /// <summary>
    /// Splits a hand into two separate hands.
    /// </summary>
    /// <param name="originalHand">The original hand to split.</param>
    /// <returns>A tuple containing the two split hands.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the hand cannot be split.</exception>
    public (Hand firstHand, Hand secondHand) SplitHand(Hand originalHand)
    {
        if (!CanSplit(originalHand))
        {
            throw new InvalidOperationException("Hand cannot be split. Must have exactly 2 cards of the same rank.");
        }

        var cards = originalHand.Cards;
        
        // Create two new hands with one card each
        var firstHand = new Hand();
        var secondHand = new Hand();
        
        firstHand.AddCard(cards[0]);
        secondHand.AddCard(cards[1]);

        // Mark hands as split hands
        firstHand.MarkAsSplitHand();
        secondHand.MarkAsSplitHand();

        return (firstHand, secondHand);
    }

    /// <summary>
    /// Determines if a split hand with Aces should receive only one additional card.
    /// </summary>
    /// <param name="hand">The split hand to check.</param>
    /// <returns>True if this is a split Ace hand that should receive only one card, false otherwise.</returns>
    public bool IsSplitAcesHand(Hand hand)
    {
        if (!hand.IsSplitHand || hand.CardCount != 1)
        {
            return false;
        }

        return hand.Cards[0].Rank == Rank.Ace;
    }

    /// <summary>
    /// Validates that a player has sufficient funds to split their hand.
    /// </summary>
    /// <param name="player">The player attempting to split.</param>
    /// <returns>True if the player has sufficient funds, false otherwise.</returns>
    public bool HasSufficientFundsForSplit(Player player)
    {
        if (!player.HasActiveBet || player.CurrentBet == null)
        {
            return false;
        }

        return player.HasSufficientFunds(player.CurrentBet.Amount);
    }

    /// <summary>
    /// Creates a split bet for the second hand.
    /// </summary>
    /// <param name="originalBet">The original bet to base the split bet on.</param>
    /// <returns>A new bet for the split hand.</returns>
    /// <exception cref="ArgumentNullException">Thrown when originalBet is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the original bet cannot be split.</exception>
    public Bet CreateSplitBet(Bet originalBet)
    {
        if (originalBet == null)
        {
            throw new ArgumentNullException(nameof(originalBet));
        }

        if (originalBet.IsSettled)
        {
            throw new InvalidOperationException("Cannot create split bet from a settled bet.");
        }

        if (originalBet.Type != BetType.Standard)
        {
            throw new InvalidOperationException("Can only split standard bets.");
        }

        return new Bet(originalBet.Amount, originalBet.PlayerName, BetType.Split);
    }

    /// <summary>
    /// Determines the maximum number of times a hand can be split.
    /// </summary>
    /// <returns>The maximum number of splits allowed (typically 3, allowing up to 4 hands).</returns>
    public int GetMaximumSplits()
    {
        return 3; // Standard casino rule: can split up to 3 times (4 hands total)
    }

    /// <summary>
    /// Counts the current number of split hands for a player.
    /// </summary>
    /// <param name="player">The player to count split hands for.</param>
    /// <returns>The number of split hands the player currently has.</returns>
    public int CountSplitHands(Player player)
    {
        // This would need to be implemented when we add multiple hands support to Player
        // For now, return 0 as the basic Player entity only supports one hand
        return 0;
    }

    /// <summary>
    /// Validates that a split operation is allowed based on game rules.
    /// </summary>
    /// <param name="hand">The hand to validate for splitting.</param>
    /// <param name="player">The player attempting to split.</param>
    /// <returns>True if the split is valid, false otherwise.</returns>
    public bool ValidateSplitOperation(Hand hand, Player player)
    {
        // Check if hand can be split
        if (!CanSplit(hand))
        {
            return false;
        }

        // Check if player has sufficient funds
        if (!HasSufficientFundsForSplit(player))
        {
            return false;
        }

        // Check if player hasn't exceeded maximum splits
        if (CountSplitHands(player) >= GetMaximumSplits())
        {
            return false;
        }

        return true;
    }
}