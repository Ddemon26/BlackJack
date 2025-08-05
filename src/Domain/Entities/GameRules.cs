using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Implements standard blackjack game rules for validation and result determination.
/// </summary>
public class GameRules : IGameRules
{
    /// <summary>
    /// Gets the blackjack value of a card considering the current hand value.
    /// </summary>
    /// <param name="card">The card to evaluate.</param>
    /// <param name="currentHandValue">The current value of the hand before adding this card.</param>
    /// <returns>The value this card contributes to the hand.</returns>
    public int GetCardValue(Card card, int currentHandValue)
    {
        return card.Rank switch
        {
            Rank.Ace => currentHandValue + 11 <= 21 ? 11 : 1,
            Rank.Jack or Rank.Queen or Rank.King => 10,
            _ => (int)card.Rank
        };
    }

    /// <summary>
    /// Determines if the dealer should hit based on standard blackjack rules.
    /// Dealer hits on 16 or less, stands on 17 or more.
    /// </summary>
    /// <param name="dealerValue">The current value of the dealer's hand.</param>
    /// <returns>True if the dealer should hit, false if the dealer should stand.</returns>
    public bool ShouldDealerHit(int dealerValue)
    {
        return dealerValue <= 16;
    }

    /// <summary>
    /// Determines the game result for a player against the dealer.
    /// </summary>
    /// <param name="playerHand">The player's hand.</param>
    /// <param name="dealerHand">The dealer's hand.</param>
    /// <returns>The game result from the player's perspective.</returns>
    public GameResult DetermineResult(Hand playerHand, Hand dealerHand)
    {
        // If player is busted, they lose regardless of dealer's hand
        if (playerHand.IsBusted())
        {
            return GameResult.Lose;
        }

        // If dealer is busted and player is not, player wins
        if (dealerHand.IsBusted())
        {
            return playerHand.IsBlackjack() ? GameResult.Blackjack : GameResult.Win;
        }

        int playerValue = playerHand.GetValue();
        int dealerValue = dealerHand.GetValue();

        // Both have blackjack - push
        if (playerHand.IsBlackjack() && dealerHand.IsBlackjack())
        {
            return GameResult.Push;
        }

        // Only player has blackjack - player wins with blackjack
        if (playerHand.IsBlackjack())
        {
            return GameResult.Blackjack;
        }

        // Only dealer has blackjack - player loses
        if (dealerHand.IsBlackjack())
        {
            return GameResult.Lose;
        }

        // Compare values
        if (playerValue > dealerValue)
        {
            return GameResult.Win;
        }
        else if (playerValue < dealerValue)
        {
            return GameResult.Lose;
        }
        else
        {
            return GameResult.Push;
        }
    }

    /// <summary>
    /// Validates if a player action is allowed given the current hand state.
    /// </summary>
    /// <param name="action">The action the player wants to take.</param>
    /// <param name="hand">The player's current hand.</param>
    /// <returns>True if the action is valid, false otherwise.</returns>
    public bool IsValidPlayerAction(PlayerAction action, Hand hand)
    {
        // Cannot take any action if hand is busted
        if (hand.IsBusted())
        {
            return false;
        }

        return action switch
        {
            PlayerAction.Hit => !hand.IsBusted(), // Can hit if not busted
            PlayerAction.Stand => true, // Can always stand if not busted
            PlayerAction.DoubleDown => hand.CardCount == 2, // Can only double down on initial 2 cards
            PlayerAction.Split => CanSplit(hand), // Can split if hand qualifies
            _ => false
        };
    }

    /// <summary>
    /// Determines if a hand qualifies as a natural blackjack.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand is a natural blackjack, false otherwise.</returns>
    public bool IsNaturalBlackjack(Hand hand)
    {
        return hand.IsBlackjack();
    }

    /// <summary>
    /// Determines if a hand is busted.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand value exceeds 21, false otherwise.</returns>
    public bool IsBusted(Hand hand)
    {
        return hand.IsBusted();
    }

    /// <summary>
    /// Determines if a hand qualifies for double down.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand can be doubled down, false otherwise.</returns>
    public bool CanDoubleDown(Hand hand)
    {
        // Must have exactly 2 cards
        if (hand.CardCount != 2)
        {
            return false;
        }

        // Must not be busted or have blackjack
        if (hand.IsBusted() || hand.IsBlackjack())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines if a hand can be split (has exactly 2 cards of the same rank).
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand can be split, false otherwise.</returns>
    public bool CanSplit(Hand hand)
    {
        if (hand.CardCount != 2)
        {
            return false;
        }

        var cards = hand.Cards;
        return cards[0].Rank == cards[1].Rank;
    }
}