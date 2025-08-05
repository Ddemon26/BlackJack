using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Services;

/// <summary>
/// Implements betting operations for a blackjack game.
/// Manages player bankrolls, bet placement, validation, and payout calculations.
/// </summary>
/// <remarks>
/// This service uses thread-safe collections to support concurrent operations.
/// It maintains player bankrolls and current bets in memory for the duration of a game session.
/// </remarks>
public class BettingService : IBettingService
{
    private readonly ConcurrentDictionary<string, Money> _playerBankrolls;
    private readonly ConcurrentDictionary<string, Bet> _currentBets;
    private readonly decimal _blackjackMultiplier;
    private readonly Money _minimumBet;
    private readonly Money _maximumBet;

    /// <summary>
    /// Initializes a new instance of the BettingService class.
    /// </summary>
    /// <param name="blackjackMultiplier">The multiplier for blackjack payouts (default 1.5 for 3:2 odds).</param>
    /// <param name="minimumBet">The minimum bet amount (default $1.00).</param>
    /// <param name="maximumBet">The maximum bet amount (default $1000.00).</param>
    public BettingService(
        decimal blackjackMultiplier = 1.5m,
        Money? minimumBet = null,
        Money? maximumBet = null)
    {
        if (blackjackMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(blackjackMultiplier), 
                "Blackjack multiplier must be positive.");

        _playerBankrolls = new ConcurrentDictionary<string, Money>(StringComparer.OrdinalIgnoreCase);
        _currentBets = new ConcurrentDictionary<string, Bet>(StringComparer.OrdinalIgnoreCase);
        _blackjackMultiplier = blackjackMultiplier;
        _minimumBet = minimumBet ?? Money.FromUsd(1.00m);
        _maximumBet = maximumBet ?? Money.FromUsd(1000.00m);

        ValidateBetLimits();
    }

    /// <summary>
    /// Places a bet for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="amount">The amount to bet.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    public async Task<BettingResult> PlaceBetAsync(string playerName, Money amount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return BettingResult.Failure("Player name cannot be null or empty.");

        var validationResult = await ValidateBetAsync(playerName, amount);
        if (validationResult.IsFailure)
            return validationResult;

        try
        {
            var bet = new Bet(amount, playerName.Trim());
            
            // Check if player already has a bet
            if (_currentBets.ContainsKey(playerName))
                return BettingResult.Failure($"Player {playerName} already has an active bet.");

            // Deduct the bet amount from the player's bankroll
            var currentBankroll = await GetPlayerBankrollAsync(playerName);
            var newBankroll = currentBankroll - amount;
            _playerBankrolls.TryUpdate(playerName, newBankroll, currentBankroll);

            // Store the bet
            _currentBets.TryAdd(playerName, bet);

            return BettingResult.Success($"Bet of {amount} placed successfully for {playerName}.", bet);
        }
        catch (Exception ex)
        {
            return BettingResult.Failure($"Failed to place bet: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates whether a bet can be placed by the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
    public async Task<BettingResult> ValidateBetAsync(string playerName, Money amount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return BettingResult.Failure("Player name cannot be null or empty.");

        if (!amount.IsPositive)
            return BettingResult.Failure("Bet amount must be positive.");

        // Check currency compatibility
        if (!string.Equals(amount.Currency, _minimumBet.Currency, StringComparison.OrdinalIgnoreCase))
            return BettingResult.Failure($"Bet currency {amount.Currency} does not match table currency {_minimumBet.Currency}.");

        // Check bet limits
        if (amount < _minimumBet)
            return BettingResult.Failure($"Bet amount {amount} is below minimum bet {_minimumBet}.");

        if (amount > _maximumBet)
            return BettingResult.Failure($"Bet amount {amount} exceeds maximum bet {_maximumBet}.");

        // Check if player has sufficient funds
        if (!await HasSufficientFundsAsync(playerName, amount))
        {
            var bankroll = await GetPlayerBankrollAsync(playerName);
            return BettingResult.Failure($"Insufficient funds. Available: {bankroll}, Required: {amount}.");
        }

        return BettingResult.Success("Bet validation successful.");
    }

    /// <summary>
    /// Calculates the payout for a bet based on the game result.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="bet">The bet to calculate payout for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the payout result.</returns>
    public Task<PayoutResult> CalculatePayoutAsync(GameResult result, Bet bet)
    {
        if (bet == null)
            throw new ArgumentNullException(nameof(bet));

        try
        {
            var payoutAmount = bet.CalculatePayout(result, _blackjackMultiplier);
            var totalReturn = bet.CalculateTotalReturn(result, _blackjackMultiplier);

            var payoutResult = new PayoutResult(bet, result, payoutAmount, totalReturn);
            return Task.FromResult(payoutResult);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to calculate payout: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if the specified player has sufficient funds for the given amount.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates if funds are sufficient.</returns>
    public async Task<bool> HasSufficientFundsAsync(string playerName, Money amount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        var bankroll = await GetPlayerBankrollAsync(playerName);
        return bankroll >= amount;
    }

    /// <summary>
    /// Gets the current bankroll for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player's bankroll.</returns>
    public Task<Money> GetPlayerBankrollAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return Task.FromResult(Money.Zero);

        var bankroll = _playerBankrolls.GetValueOrDefault(playerName.Trim(), Money.Zero);
        return Task.FromResult(bankroll);
    }

    /// <summary>
    /// Updates the bankroll for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to add to (positive) or subtract from (negative) the bankroll.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateBankrollAsync(string playerName, Money amount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        var trimmedName = playerName.Trim();
        var currentBankroll = await GetPlayerBankrollAsync(trimmedName);
        var newBankroll = currentBankroll + amount;

        // Prevent negative bankrolls
        if (newBankroll.IsNegative)
            newBankroll = Money.Zero;

        _playerBankrolls.AddOrUpdate(trimmedName, newBankroll, (_, _) => newBankroll);
    }

    /// <summary>
    /// Sets the initial bankroll for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="initialAmount">The initial bankroll amount.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetInitialBankrollAsync(string playerName, Money initialAmount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (initialAmount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(initialAmount), "Initial bankroll cannot be negative.");

        var trimmedName = playerName.Trim();
        _playerBankrolls.AddOrUpdate(trimmedName, initialAmount, (_, _) => initialAmount);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current bet for the specified player, if any.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current bet or null if no bet is placed.</returns>
    public Task<Bet?> GetCurrentBetAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return Task.FromResult<Bet?>(null);

        var bet = _currentBets.GetValueOrDefault(playerName.Trim());
        return Task.FromResult(bet);
    }

    /// <summary>
    /// Clears all current bets, typically called at the start of a new round.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ClearAllBetsAsync()
    {
        _currentBets.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes payouts for all players based on their game results.
    /// </summary>
    /// <param name="playerResults">A dictionary mapping player names to their game results.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the payout summary.</returns>
    public async Task<PayoutSummary> ProcessPayoutsAsync(IDictionary<string, GameResult> playerResults)
    {
        if (playerResults == null)
            throw new ArgumentNullException(nameof(playerResults));

        var payoutResults = new List<PayoutResult>();

        foreach (var (playerName, gameResult) in playerResults)
        {
            var bet = await GetCurrentBetAsync(playerName);
            if (bet == null)
                continue; // Skip players without bets

            try
            {
                var payoutResult = await CalculatePayoutAsync(gameResult, bet);
                payoutResults.Add(payoutResult);

                // Update player's bankroll with the total return
                await UpdateBankrollAsync(playerName, payoutResult.TotalReturn);

                // Settle the bet
                bet.Settle();
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other players
                // In a real application, you might want to use a proper logging framework
                Console.WriteLine($"Error processing payout for {playerName}: {ex.Message}");
            }
        }

        return new PayoutSummary(payoutResults);
    }

    /// <summary>
    /// Gets the minimum bet amount.
    /// </summary>
    public Money MinimumBet => _minimumBet;

    /// <summary>
    /// Gets the maximum bet amount.
    /// </summary>
    public Money MaximumBet => _maximumBet;

    /// <summary>
    /// Gets the blackjack multiplier.
    /// </summary>
    public decimal BlackjackMultiplier => _blackjackMultiplier;

    /// <summary>
    /// Gets all current bets.
    /// </summary>
    /// <returns>A dictionary of current bets keyed by player name.</returns>
    public IReadOnlyDictionary<string, Bet> GetAllCurrentBets()
    {
        return _currentBets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Gets all player bankrolls.
    /// </summary>
    /// <returns>A dictionary of player bankrolls keyed by player name.</returns>
    public IReadOnlyDictionary<string, Money> GetAllBankrolls()
    {
        return _playerBankrolls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Validates that the bet limits are properly configured.
    /// </summary>
    private void ValidateBetLimits()
    {
        if (_minimumBet >= _maximumBet)
            throw new ArgumentException("Minimum bet must be less than maximum bet.");

        if (!_minimumBet.IsPositive)
            throw new ArgumentException("Minimum bet must be positive.");

        if (!_maximumBet.IsPositive)
            throw new ArgumentException("Maximum bet must be positive.");
    }
}