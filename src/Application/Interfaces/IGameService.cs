using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Interfaces;

/// <summary>
/// Defines the contract for core game logic and state management in blackjack.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Starts a new game with the specified players.
    /// </summary>
    /// <param name="playerNames">The names of the players to include in the game.</param>
    /// <exception cref="ArgumentException">Thrown when player names are invalid or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a game is already in progress.</exception>
    void StartNewGame(IEnumerable<string> playerNames);

    /// <summary>
    /// Deals the initial two cards to each player and the dealer.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or cards have already been dealt.</exception>
    void DealInitialCards();

    /// <summary>
    /// Processes a player action (hit, stand, etc.) and returns the result.
    /// </summary>
    /// <param name="playerName">The name of the player taking the action.</param>
    /// <param name="action">The action the player wants to take.</param>
    /// <returns>The result of the player action.</returns>
    /// <exception cref="ArgumentException">Thrown when the player name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or it's not the player's turn.</exception>
    PlayerActionResult ProcessPlayerAction(string playerName, PlayerAction action);

    /// <summary>
    /// Plays the dealer's turn automatically following standard blackjack rules.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or dealer turn is not valid.</exception>
    void PlayDealerTurn();

    /// <summary>
    /// Gets the results of the current game for all players.
    /// </summary>
    /// <returns>A summary of game results for all players.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or game is not complete.</exception>
    GameSummary GetGameResults();

    /// <summary>
    /// Gets the results of the current game for all players with payout processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a summary of game results with payouts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or game is not complete.</exception>
    Task<GameSummary> GetGameResultsWithPayoutsAsync();

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    /// <returns>The current game state, or null if no game is in progress.</returns>
    GameState? GetCurrentGameState();

    /// <summary>
    /// Gets all players in the current game.
    /// </summary>
    /// <returns>A read-only list of all players, or empty if no game is in progress.</returns>
    IReadOnlyList<Player> GetPlayers();

    /// <summary>
    /// Gets the dealer for the current game.
    /// </summary>
    /// <returns>The dealer player, or null if no game is in progress.</returns>
    Player? GetDealer();

    /// <summary>
    /// Gets a specific player by name.
    /// </summary>
    /// <param name="playerName">The name of the player to find.</param>
    /// <returns>The player with the specified name, or null if not found.</returns>
    Player? GetPlayer(string playerName);

    /// <summary>
    /// Determines if it's currently the specified player's turn.
    /// </summary>
    /// <param name="playerName">The name of the player to check.</param>
    /// <returns>True if it's the player's turn, false otherwise.</returns>
    bool IsPlayerTurn(string playerName);

    /// <summary>
    /// Gets a value indicating whether the current game is complete.
    /// </summary>
    bool IsGameComplete { get; }

    /// <summary>
    /// Gets a value indicating whether a game is currently in progress.
    /// </summary>
    bool IsGameInProgress { get; }

    /// <summary>
    /// Processes a double down action for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player performing the double down.</param>
    /// <returns>A task that returns the result of the double down action.</returns>
    /// <exception cref="ArgumentException">Thrown when the player name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when double down is not allowed.</exception>
    Task<PlayerActionResult> ProcessDoubleDownAsync(string playerName);

    /// <summary>
    /// Determines if the specified player can double down.
    /// </summary>
    /// <param name="playerName">The name of the player to check.</param>
    /// <returns>A task that returns true if the player can double down, false otherwise.</returns>
    Task<bool> CanPlayerDoubleDownAsync(string playerName);

    /// <summary>
    /// Processes a split action for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player performing the split.</param>
    /// <returns>A task that returns the result of the split action.</returns>
    /// <exception cref="ArgumentException">Thrown when the player name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when split is not allowed.</exception>
    Task<PlayerActionResult> ProcessSplitAsync(string playerName);

    /// <summary>
    /// Determines if the specified player can split their hand.
    /// </summary>
    /// <param name="playerName">The name of the player to check.</param>
    /// <returns>A task that returns true if the player can split, false otherwise.</returns>
    Task<bool> CanPlayerSplitAsync(string playerName);

    /// <summary>
    /// Gets all hands for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A read-only list of all hands belonging to the player.</returns>
    IReadOnlyList<Hand> GetPlayerHands(string playerName);

    /// <summary>
    /// Processes the betting round by collecting and validating bets from all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or betting phase is not active.</exception>
    Task<BettingResult> ProcessBettingRoundAsync();

    /// <summary>
    /// Places a bet for a specific player during the betting round.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="amount">The amount to bet.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when betting is not allowed.</exception>
    Task<BettingResult> PlacePlayerBetAsync(string playerName, Money amount);

    /// <summary>
    /// Gets the current betting state.
    /// </summary>
    /// <returns>The current betting state, or null if no betting round is active.</returns>
    BettingState? GetCurrentBettingState();

    /// <summary>
    /// Determines if all players have placed their bets.
    /// </summary>
    /// <returns>True if all players have placed bets, false otherwise.</returns>
    bool AllPlayersHaveBets();
}