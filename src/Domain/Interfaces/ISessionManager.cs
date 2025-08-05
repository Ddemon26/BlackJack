using GroupProject.Domain.Entities;
using GroupProject.Application.Models;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for managing blackjack game sessions with lifecycle management and persistence capabilities.
/// </summary>
/// <remarks>
/// The session manager handles the creation, management, and persistence of multi-round gaming sessions.
/// It provides methods for session lifecycle management, state persistence, and recovery capabilities.
/// All operations are designed to be thread-safe and support concurrent access where appropriate.
/// </remarks>
public interface ISessionManager
{
    /// <summary>
    /// Starts a new gaming session with the specified players and configuration.
    /// </summary>
    /// <param name="playerNames">The names of players to include in the session.</param>
    /// <param name="configuration">The game configuration for the session.</param>
    /// <param name="defaultBankroll">The default bankroll for each player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when playerNames or configuration is null.</exception>
    /// <exception cref="ArgumentException">Thrown when playerNames is empty or contains invalid names.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a session is already active.</exception>
    Task<GameSession> StartSessionAsync(
        IEnumerable<string> playerNames, 
        GameConfiguration configuration, 
        Money defaultBankroll);

    /// <summary>
    /// Gets the currently active session, if any.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the current session or null if none is active.</returns>
    Task<GameSession?> GetCurrentSessionAsync();

    /// <summary>
    /// Updates the current session with new state information.
    /// </summary>
    /// <param name="session">The session to update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not the current active session.</exception>
    Task UpdateSessionAsync(GameSession session);

    /// <summary>
    /// Ends the current session and returns a comprehensive summary.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the session summary.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task<SessionSummary> EndSessionAsync();

    /// <summary>
    /// Checks if the current session can continue (has active players and is not ended).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the session can continue.</returns>
    Task<bool> CanContinueSessionAsync();

    /// <summary>
    /// Records the completion of a round in the current session.
    /// </summary>
    /// <param name="roundSummary">The summary of the completed round.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when roundSummary is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task RecordRoundAsync(GameSummary roundSummary);

    /// <summary>
    /// Adds a player to the current session.
    /// </summary>
    /// <param name="playerName">The name of the player to add.</param>
    /// <param name="initialBankroll">The initial bankroll for the player (uses session default if not specified).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists or player already exists.</exception>
    Task AddPlayerAsync(string playerName, Money? initialBankroll = null);

    /// <summary>
    /// Removes a player from the current session.
    /// </summary>
    /// <param name="playerName">The name of the player to remove.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when player does not exist in the session.</exception>
    Task RemovePlayerAsync(string playerName);

    /// <summary>
    /// Gets a player from the current session by name.
    /// </summary>
    /// <param name="playerName">The name of the player to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when player does not exist in the session.</exception>
    Task<Player> GetPlayerAsync(string playerName);

    /// <summary>
    /// Gets all players from the current session.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains all players in the session.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task<IReadOnlyDictionary<string, Player>> GetAllPlayersAsync();

    /// <summary>
    /// Gets the players who are still able to play (have positive bankrolls).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the active players.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task<IEnumerable<Player>> GetActivePlayersAsync();

    /// <summary>
    /// Resets all players in the current session for a new round.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task ResetPlayersForNewRoundAsync();

    /// <summary>
    /// Saves the current session state to persistent storage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no active session exists.</exception>
    Task SaveSessionStateAsync();

    /// <summary>
    /// Loads a session from persistent storage by session ID.
    /// </summary>
    /// <param name="sessionId">The ID of the session to load.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded session or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    Task<GameSession?> LoadSessionAsync(string sessionId);

    /// <summary>
    /// Gets a list of all saved session IDs.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the list of session IDs.</returns>
    Task<IEnumerable<string>> GetSavedSessionIdsAsync();

    /// <summary>
    /// Deletes a saved session from persistent storage.
    /// </summary>
    /// <param name="sessionId">The ID of the session to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the session was successfully deleted.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    Task<bool> DeleteSessionAsync(string sessionId);

    /// <summary>
    /// Attempts to recover from an unexpected shutdown by loading the most recent session.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the recovered session or null if no recovery is possible.</returns>
    Task<GameSession?> RecoverSessionAsync();
}