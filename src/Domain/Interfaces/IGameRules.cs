using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Defines the contract for game rule validation and result determination in blackjack.
/// </summary>
public interface IGameRules
{
    /// <summary>
    /// Gets the blackjack value of a card considering the current hand value.
    /// </summary>
    /// <param name="card">The card to evaluate.</param>
    /// <param name="currentHandValue">The current value of the hand before adding this card.</param>
    /// <returns>The value this card contributes to the hand (1 or 11 for Ace, 10 for face cards, face value for others).</returns>
    int GetCardValue(Card card, int currentHandValue);

    /// <summary>
    /// Determines if the dealer should hit based on standard blackjack rules.
    /// </summary>
    /// <param name="dealerValue">The current value of the dealer's hand.</param>
    /// <returns>True if the dealer should hit (value 16 or less), false if the dealer should stand (17 or more).</returns>
    bool ShouldDealerHit(int dealerValue);

    /// <summary>
    /// Determines the game result for a player against the dealer.
    /// </summary>
    /// <param name="playerHand">The player's hand.</param>
    /// <param name="dealerHand">The dealer's hand.</param>
    /// <returns>The game result from the player's perspective.</returns>
    GameResult DetermineResult(Hand playerHand, Hand dealerHand);

    /// <summary>
    /// Validates if a player action is allowed given the current hand state.
    /// </summary>
    /// <param name="action">The action the player wants to take.</param>
    /// <param name="hand">The player's current hand.</param>
    /// <returns>True if the action is valid, false otherwise.</returns>
    bool IsValidPlayerAction(PlayerAction action, Hand hand);

    /// <summary>
    /// Determines if a hand qualifies as a natural blackjack.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand is a natural blackjack (21 with exactly 2 cards), false otherwise.</returns>
    bool IsNaturalBlackjack(Hand hand);

    /// <summary>
    /// Determines if a hand is busted.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand value exceeds 21, false otherwise.</returns>
    bool IsBusted(Hand hand);

    /// <summary>
    /// Determines if a hand qualifies for double down.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand can be doubled down (exactly 2 cards, not busted, not blackjack), false otherwise.</returns>
    bool CanDoubleDown(Hand hand);

    /// <summary>
    /// Determines if a hand can be split.
    /// </summary>
    /// <param name="hand">The hand to check.</param>
    /// <returns>True if the hand can be split (exactly 2 cards of the same rank), false otherwise.</returns>
    bool CanSplit(Hand hand);
}