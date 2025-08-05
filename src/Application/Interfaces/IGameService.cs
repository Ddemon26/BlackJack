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
}