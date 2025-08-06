using System;
using System.Threading.Tasks;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Interface for preserving and restoring game state during error recovery.
/// Provides mechanisms to save game state before risky operations and restore it if needed.
/// </summary>
public interface IGameStatePreserver
{
    /// <summary>
    /// Creates a snapshot of the current game state.
    /// </summary>
    /// <param name="stateId">A unique identifier for this state snapshot.</param>
    /// <param name="context">Additional context about when/why the state was preserved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<string> PreserveStateAsync(string stateId, string context = "");

    /// <summary>
    /// Restores the game state from a previously created snapshot.
    /// </summary>
    /// <param name="stateId">The identifier of the state snapshot to restore.</param>
    /// <returns>A task that returns true if the state was successfully restored, false otherwise.</returns>
    Task<bool> RestoreStateAsync(string stateId);

    /// <summary>
    /// Removes a state snapshot that is no longer needed.
    /// </summary>
    /// <param name="stateId">The identifier of the state snapshot to remove.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearStateAsync(string stateId);

    /// <summary>
    /// Checks if a state snapshot exists for the given identifier.
    /// </summary>
    /// <param name="stateId">The identifier to check.</param>
    /// <returns>A task that returns true if the state snapshot exists, false otherwise.</returns>
    Task<bool> StateExistsAsync(string stateId);

    /// <summary>
    /// Gets information about a preserved state snapshot.
    /// </summary>
    /// <param name="stateId">The identifier of the state snapshot.</param>
    /// <returns>A task that returns state information, or null if the state doesn't exist.</returns>
    Task<GameStateInfo?> GetStateInfoAsync(string stateId);

    /// <summary>
    /// Clears all preserved state snapshots older than the specified age.
    /// </summary>
    /// <param name="maxAge">The maximum age of state snapshots to keep.</param>
    /// <returns>A task that returns the number of state snapshots that were cleared.</returns>
    Task<int> ClearOldStatesAsync(TimeSpan maxAge);

    /// <summary>
    /// Gets a list of all preserved state identifiers.
    /// </summary>
    /// <returns>A task that returns an enumerable of state identifiers.</returns>
    Task<IEnumerable<string>> GetPreservedStateIdsAsync();
}

/// <summary>
/// Contains information about a preserved game state.
/// </summary>
public class GameStateInfo
{
    /// <summary>
    /// Initializes a new instance of the GameStateInfo class.
    /// </summary>
    /// <param name="stateId">The unique identifier for the state.</param>
    /// <param name="preservedAt">When the state was preserved.</param>
    /// <param name="context">Additional context about the state.</param>
    /// <param name="sizeBytes">The size of the preserved state in bytes.</param>
    public GameStateInfo(string stateId, DateTime preservedAt, string context, long sizeBytes)
    {
        StateId = stateId ?? throw new ArgumentNullException(nameof(stateId));
        PreservedAt = preservedAt;
        Context = context ?? string.Empty;
        SizeBytes = sizeBytes;
    }

    /// <summary>
    /// Gets the unique identifier for the state.
    /// </summary>
    public string StateId { get; }

    /// <summary>
    /// Gets when the state was preserved.
    /// </summary>
    public DateTime PreservedAt { get; }

    /// <summary>
    /// Gets additional context about the state.
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// Gets the size of the preserved state in bytes.
    /// </summary>
    public long SizeBytes { get; }

    /// <summary>
    /// Gets the age of the preserved state.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - PreservedAt;
}