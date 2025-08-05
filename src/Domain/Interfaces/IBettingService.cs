using System.Threading.Tasks;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Defines the contract for betting operations in a blackjack game.
/// Handles bet placement, validation, bankroll management, and payout calculations.
/// </summary>
public interface IBettingService
{
    /// <summary>
    /// Places a bet for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="amount">The amount to bet.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    Task<BettingResult> PlaceBetAsync(string playerName, Money amount);

    /// <summary>
    /// Validates whether a bet can be placed by the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
    Task<BettingResult> ValidateBetAsync(string playerName, Money amount);

    /// <summary>
    /// Calculates the payout for a bet based on the game result.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="bet">The bet to calculate payout for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the payout result.</returns>
    Task<PayoutResult> CalculatePayoutAsync(GameResult result, Bet bet);

    /// <summary>
    /// Checks if the specified player has sufficient funds for the given amount.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates if funds are sufficient.</returns>
    Task<bool> HasSufficientFundsAsync(string playerName, Money amount);

    /// <summary>
    /// Gets the current bankroll for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player's bankroll.</returns>
    Task<Money> GetPlayerBankrollAsync(string playerName);

    /// <summary>
    /// Updates the bankroll for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to add to (positive) or subtract from (negative) the bankroll.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateBankrollAsync(string playerName, Money amount);

    /// <summary>
    /// Sets the initial bankroll for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="initialAmount">The initial bankroll amount.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetInitialBankrollAsync(string playerName, Money initialAmount);

    /// <summary>
    /// Gets the current bet for the specified player, if any.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current bet or null if no bet is placed.</returns>
    Task<Bet?> GetCurrentBetAsync(string playerName);

    /// <summary>
    /// Clears all current bets, typically called at the start of a new round.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ClearAllBetsAsync();

    /// <summary>
    /// Processes payouts for all players based on their game results.
    /// </summary>
    /// <param name="playerResults">A dictionary mapping player names to their game results.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the payout summary.</returns>
    Task<PayoutSummary> ProcessPayoutsAsync(IDictionary<string, GameResult> playerResults);
}