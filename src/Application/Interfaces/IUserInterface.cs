using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Interfaces;

/// <summary>
/// Defines the contract for user interface operations in the blackjack game.
/// </summary>
public interface IUserInterface
{
    /// <summary>
    /// Shows the welcome message and game instructions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowWelcomeMessageAsync();

    /// <summary>
    /// Displays the current game state including all player hands.
    /// </summary>
    /// <param name="gameState">The current game state to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowGameStateAsync(GameState gameState);

    /// <summary>
    /// Displays a specific player's hand.
    /// </summary>
    /// <param name="player">The player whose hand to display.</param>
    /// <param name="hideFirstCard">Whether to hide the first card (typically for dealer).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowPlayerHandAsync(Player player, bool hideFirstCard = false);

    /// <summary>
    /// Displays the final game results for all players.
    /// </summary>
    /// <param name="results">The game results to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowGameResultsAsync(GameSummary results);

    /// <summary>
    /// Prompts the user for the number of players and returns the result.
    /// </summary>
    /// <returns>A task that returns the number of players.</returns>
    Task<int> GetPlayerCountAsync();

    /// <summary>
    /// Prompts the user for player names and returns the collection.
    /// </summary>
    /// <param name="count">The number of player names to collect.</param>
    /// <returns>A task that returns the collection of player names.</returns>
    Task<IEnumerable<string>> GetPlayerNamesAsync(int count);

    /// <summary>
    /// Prompts the user for a player action and returns the result.
    /// </summary>
    /// <param name="playerName">The name of the player taking the action.</param>
    /// <param name="validActions">The valid actions available to the player.</param>
    /// <returns>A task that returns the selected player action.</returns>
    Task<PlayerAction> GetPlayerActionAsync(string playerName, IEnumerable<PlayerAction> validActions);

    /// <summary>
    /// Prompts the user to determine if they want to play another round.
    /// </summary>
    /// <returns>A task that returns true if the user wants to play another round, false otherwise.</returns>
    Task<bool> ShouldPlayAnotherRoundAsync();

    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowErrorMessageAsync(string message);

    /// <summary>
    /// Shows an informational message to the user.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowMessageAsync(string message);

    /// <summary>
    /// Clears the display (if supported by the interface).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearDisplayAsync();
}