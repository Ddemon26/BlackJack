using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces;

/// <summary>
/// Defines the contract for persisting and retrieving player statistics data.
/// Provides CRUD operations for player statistics with data migration and backup capabilities.
/// </summary>
/// <remarks>
/// This repository interface abstracts the data persistence layer for player statistics,
/// allowing for different storage implementations (file-based, database, etc.) while
/// maintaining a consistent interface for the application layer.
/// </remarks>
public interface IStatisticsRepository
{
    /// <summary>
    /// Retrieves player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player statistics, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName);

    /// <summary>
    /// Saves or updates player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="statistics">The player statistics to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task SavePlayerStatisticsAsync(string playerName, PlayerStatistics statistics);

    /// <summary>
    /// Retrieves statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task<IEnumerable<PlayerStatistics>> GetAllPlayerStatisticsAsync();

    /// <summary>
    /// Deletes player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player whose statistics should be deleted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task<bool> DeletePlayerStatisticsAsync(string playerName);

    /// <summary>
    /// Checks if statistics exist for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether statistics exist for the player.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task<bool> PlayerStatisticsExistAsync(string playerName);

    /// <summary>
    /// Creates a backup of all player statistics data.
    /// </summary>
    /// <param name="backupPath">The path where the backup should be created. If null, uses default backup location.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the created backup file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the backup operation fails.</exception>
    Task<string> CreateBackupAsync(string? backupPath = null);

    /// <summary>
    /// Restores player statistics data from a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file to restore from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing statistics data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics restored.</returns>
    /// <exception cref="ArgumentException">Thrown when backupPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the backup file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the restore operation fails.</exception>
    Task<int> RestoreFromBackupAsync(string backupPath, bool overwriteExisting = false);

    /// <summary>
    /// Migrates statistics data from an older format to the current format.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics migrated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the migration operation fails.</exception>
    Task<int> MigrateDataAsync();

    /// <summary>
    /// Gets the total number of player statistics records in the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    Task<int> GetPlayerCountAsync();

    /// <summary>
    /// Clears all player statistics data from the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the clear operation fails.</exception>
    Task ClearAllStatisticsAsync();
}