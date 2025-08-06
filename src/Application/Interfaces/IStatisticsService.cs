using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Interfaces;

/// <summary>
/// Defines the contract for managing player statistics and aggregation operations.
/// Provides high-level statistics operations that coordinate with the statistics repository.
/// </summary>
/// <remarks>
/// This service acts as a facade over the statistics repository, providing business logic
/// for statistics management, aggregation, and reporting. It handles the coordination
/// between game results and statistics updates while maintaining data consistency.
/// </remarks>
public interface IStatisticsService
{
    /// <summary>
    /// Retrieves player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player statistics, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName);

    /// <summary>
    /// Updates player statistics based on a game result.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="gameResult">The result of the game.</param>
    /// <param name="betAmount">The amount that was bet.</param>
    /// <param name="payout">The payout received (zero for losses).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when betAmount is not positive or payout is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the update operation fails.</exception>
    Task UpdatePlayerStatisticsAsync(string playerName, GameResult gameResult, Money betAmount, Money payout);

    /// <summary>
    /// Retrieves statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<IEnumerable<PlayerStatistics>> GetAllPlayerStatisticsAsync();

    /// <summary>
    /// Gets aggregated statistics across all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains aggregated statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<AggregatedStatistics> GetAggregatedStatisticsAsync();

    /// <summary>
    /// Gets the top players by a specified metric.
    /// </summary>
    /// <param name="metric">The metric to rank players by.</param>
    /// <param name="count">The maximum number of players to return.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the top players.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<IEnumerable<PlayerStatistics>> GetTopPlayersAsync(StatisticsMetric metric, int count = 10);

    /// <summary>
    /// Resets statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player whose statistics should be reset.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the reset was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the reset operation fails.</exception>
    Task<bool> ResetPlayerStatisticsAsync(string playerName);

    /// <summary>
    /// Resets statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of players whose statistics were reset.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the reset operation fails.</exception>
    Task<int> ResetAllPlayerStatisticsAsync();

    /// <summary>
    /// Exports player statistics to a specified format.
    /// </summary>
    /// <param name="exportPath">The path where the export file should be created.</param>
    /// <param name="format">The export format.</param>
    /// <param name="playerNames">Optional list of specific players to export. If null, exports all players.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the exported file.</returns>
    /// <exception cref="ArgumentException">Thrown when exportPath is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the export operation fails.</exception>
    Task<string> ExportStatisticsAsync(string exportPath, StatisticsExportFormat format, IEnumerable<string>? playerNames = null);

    /// <summary>
    /// Imports player statistics from a file.
    /// </summary>
    /// <param name="importPath">The path to the file to import from.</param>
    /// <param name="mergeStrategy">The strategy to use when merging with existing statistics.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics imported.</returns>
    /// <exception cref="ArgumentException">Thrown when importPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the import file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the import operation fails.</exception>
    Task<int> ImportStatisticsAsync(string importPath, StatisticsMergeStrategy mergeStrategy = StatisticsMergeStrategy.Overwrite);

    /// <summary>
    /// Gets statistics for players who have played within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains statistics for players active in the date range.</returns>
    /// <exception cref="ArgumentException">Thrown when startDate is after endDate.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<IEnumerable<PlayerStatistics>> GetPlayerStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Checks if statistics exist for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether statistics exist for the player.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<bool> PlayerStatisticsExistAsync(string playerName);

    /// <summary>
    /// Gets the total number of players with statistics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of players.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    Task<int> GetPlayerCountAsync();

    /// <summary>
    /// Creates a backup of all player statistics.
    /// </summary>
    /// <param name="backupPath">The path where the backup should be created. If null, uses default backup location.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the created backup file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the backup operation fails.</exception>
    Task<string> CreateBackupAsync(string? backupPath = null);

    /// <summary>
    /// Restores player statistics from a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file to restore from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing statistics data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics restored.</returns>
    /// <exception cref="ArgumentException">Thrown when backupPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the backup file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the restore operation fails.</exception>
    Task<int> RestoreFromBackupAsync(string backupPath, bool overwriteExisting = false);
}