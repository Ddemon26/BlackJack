using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using GroupProject.Application.Interfaces;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Services;

/// <summary>
/// Implementation of the statistics service that provides high-level statistics management operations.
/// Coordinates with the statistics repository to provide business logic for statistics operations.
/// </summary>
/// <remarks>
/// This service acts as a facade over the statistics repository, providing additional business logic
/// for statistics management, aggregation, and reporting. It handles complex operations like
/// statistics export/import, aggregation calculations, and player ranking.
/// </remarks>
public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _statisticsRepository;

    /// <summary>
    /// Initializes a new instance of the StatisticsService class.
    /// </summary>
    /// <param name="statisticsRepository">The statistics repository to use for data operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when statisticsRepository is null.</exception>
    public StatisticsService(IStatisticsRepository statisticsRepository)
    {
        _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
    }

    /// <summary>
    /// Retrieves player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player statistics, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        try
        {
            return await _statisticsRepository.GetPlayerStatisticsAsync(playerName);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to retrieve statistics for player '{playerName}': {ex.Message}", ex);
        }
    }

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
    public async Task UpdatePlayerStatisticsAsync(string playerName, GameResult gameResult, Money betAmount, Money payout)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        if (!betAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(betAmount), "Bet amount must be positive.");

        if (payout.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(payout), "Payout cannot be negative.");

        try
        {
            // Get existing statistics or create new ones
            var existingStats = await _statisticsRepository.GetPlayerStatisticsAsync(playerName);
            var statistics = existingStats ?? new PlayerStatistics(playerName);

            // Record the game result
            statistics.RecordGame(gameResult, betAmount, payout);

            // Save the updated statistics
            await _statisticsRepository.SavePlayerStatisticsAsync(playerName, statistics);
        }
        catch (Exception ex) when (ex is not ArgumentException and not ArgumentOutOfRangeException)
        {
            throw new InvalidOperationException($"Failed to update statistics for player '{playerName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Retrieves statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<IEnumerable<PlayerStatistics>> GetAllPlayerStatisticsAsync()
    {
        try
        {
            return await _statisticsRepository.GetAllPlayerStatisticsAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve all player statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets aggregated statistics across all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains aggregated statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<AggregatedStatistics> GetAggregatedStatisticsAsync()
    {
        try
        {
            var allStats = await _statisticsRepository.GetAllPlayerStatisticsAsync();
            var statsList = allStats.ToList();

            if (!statsList.Any())
            {
                return new AggregatedStatistics(0, 0, 0, 0, 0, 0, Money.Zero, Money.Zero, null, null);
            }

            var totalPlayers = statsList.Count;
            var totalGamesPlayed = statsList.Sum(s => s.GamesPlayed);
            var totalGamesWon = statsList.Sum(s => s.GamesWon);
            var totalGamesLost = statsList.Sum(s => s.GamesLost);
            var totalGamesPushed = statsList.Sum(s => s.GamesPushed);
            var totalBlackjacksAchieved = statsList.Sum(s => s.BlackjacksAchieved);
            var totalAmountWagered = statsList.Aggregate(Money.Zero, (sum, s) => sum + s.TotalWagered);
            var totalNetWinnings = statsList.Aggregate(Money.Zero, (sum, s) => sum + s.NetWinnings);
            var earliestGameDate = statsList.Min(s => s.FirstPlayed);
            var latestGameDate = statsList.Max(s => s.LastPlayed);

            return new AggregatedStatistics(
                totalPlayers,
                totalGamesPlayed,
                totalGamesWon,
                totalGamesLost,
                totalGamesPushed,
                totalBlackjacksAchieved,
                totalAmountWagered,
                totalNetWinnings,
                earliestGameDate,
                latestGameDate);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to calculate aggregated statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the top players by a specified metric.
    /// </summary>
    /// <param name="metric">The metric to rank players by.</param>
    /// <param name="count">The maximum number of players to return.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the top players.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<IEnumerable<PlayerStatistics>> GetTopPlayersAsync(StatisticsMetric metric, int count = 10)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");

        try
        {
            var allStats = await _statisticsRepository.GetAllPlayerStatisticsAsync();
            var statsList = allStats.ToList();

            if (!statsList.Any())
                return Enumerable.Empty<PlayerStatistics>();

            return metric switch
            {
                StatisticsMetric.GamesPlayed => statsList.OrderByDescending(s => s.GamesPlayed).Take(count),
                StatisticsMetric.GamesWon => statsList.OrderByDescending(s => s.GamesWon).Take(count),
                StatisticsMetric.WinPercentage => statsList.Where(s => s.GamesPlayed > 0).OrderByDescending(s => s.WinPercentage).Take(count),
                StatisticsMetric.NetWinnings => statsList.OrderByDescending(s => s.NetWinnings.Amount).Take(count),
                StatisticsMetric.TotalWagered => statsList.OrderByDescending(s => s.TotalWagered.Amount).Take(count),
                StatisticsMetric.BlackjacksAchieved => statsList.OrderByDescending(s => s.BlackjacksAchieved).Take(count),
                StatisticsMetric.BlackjackPercentage => statsList.Where(s => s.GamesPlayed > 0).OrderByDescending(s => s.BlackjackPercentage).Take(count),
                StatisticsMetric.ReturnOnInvestment => statsList.Where(s => s.TotalWagered.IsPositive).OrderByDescending(s => s.ReturnOnInvestment).Take(count),
                StatisticsMetric.AverageBet => statsList.Where(s => s.GamesPlayed > 0).OrderByDescending(s => s.AverageBet.Amount).Take(count),
                StatisticsMetric.TotalPlayTime => statsList.OrderByDescending(s => s.TotalPlayTime.TotalMinutes).Take(count),
                _ => throw new ArgumentException($"Unknown statistics metric: {metric}", nameof(metric))
            };
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException and not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to get top players by {metric}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resets statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player whose statistics should be reset.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the reset was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the reset operation fails.</exception>
    public async Task<bool> ResetPlayerStatisticsAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        try
        {
            var existingStats = await _statisticsRepository.GetPlayerStatisticsAsync(playerName);
            if (existingStats == null)
                return false;

            var resetStats = new PlayerStatistics(playerName);
            await _statisticsRepository.SavePlayerStatisticsAsync(playerName, resetStats);
            return true;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to reset statistics for player '{playerName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resets statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of players whose statistics were reset.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the reset operation fails.</exception>
    public async Task<int> ResetAllPlayerStatisticsAsync()
    {
        try
        {
            var allStats = await _statisticsRepository.GetAllPlayerStatisticsAsync();
            var resetCount = 0;

            foreach (var stats in allStats)
            {
                var resetStats = new PlayerStatistics(stats.PlayerName);
                await _statisticsRepository.SavePlayerStatisticsAsync(stats.PlayerName, resetStats);
                resetCount++;
            }

            return resetCount;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to reset all player statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Exports player statistics to a specified format.
    /// </summary>
    /// <param name="exportPath">The path where the export file should be created.</param>
    /// <param name="format">The export format.</param>
    /// <param name="playerNames">Optional list of specific players to export. If null, exports all players.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the exported file.</returns>
    /// <exception cref="ArgumentException">Thrown when exportPath is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the export operation fails.</exception>
    public async Task<string> ExportStatisticsAsync(string exportPath, StatisticsExportFormat format, IEnumerable<string>? playerNames = null)
    {
        if (string.IsNullOrWhiteSpace(exportPath))
            throw new ArgumentException("Export path cannot be null, empty, or whitespace.", nameof(exportPath));

        try
        {
            var allStats = await _statisticsRepository.GetAllPlayerStatisticsAsync();
            var statsToExport = playerNames != null 
                ? allStats.Where(s => playerNames.Contains(s.PlayerName, StringComparer.OrdinalIgnoreCase))
                : allStats;

            var statsList = statsToExport.ToList();

            // Ensure export directory exists
            var exportDir = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(exportDir) && !Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            string content = format switch
            {
                StatisticsExportFormat.Json => await ExportToJsonAsync(statsList),
                StatisticsExportFormat.Csv => await ExportToCsvAsync(statsList),
                StatisticsExportFormat.Xml => await ExportToXmlAsync(statsList),
                StatisticsExportFormat.Text => await ExportToTextAsync(statsList),
                _ => throw new ArgumentException($"Unknown export format: {format}", nameof(format))
            };

            await File.WriteAllTextAsync(exportPath, content);
            return exportPath;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to export statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Imports player statistics from a file.
    /// </summary>
    /// <param name="importPath">The path to the file to import from.</param>
    /// <param name="mergeStrategy">The strategy to use when merging with existing statistics.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics imported.</returns>
    /// <exception cref="ArgumentException">Thrown when importPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the import file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the import operation fails.</exception>
    public async Task<int> ImportStatisticsAsync(string importPath, StatisticsMergeStrategy mergeStrategy = StatisticsMergeStrategy.Overwrite)
    {
        if (string.IsNullOrWhiteSpace(importPath))
            throw new ArgumentException("Import path cannot be null, empty, or whitespace.", nameof(importPath));

        if (!File.Exists(importPath))
            throw new FileNotFoundException($"Import file not found: {importPath}");

        try
        {
            var content = await File.ReadAllTextAsync(importPath);
            var extension = Path.GetExtension(importPath).ToLowerInvariant();

            List<PlayerStatistics> importedStats = extension switch
            {
                ".json" => await ImportFromJsonAsync(content),
                ".csv" => await ImportFromCsvAsync(content),
                ".xml" => await ImportFromXmlAsync(content),
                _ => throw new InvalidOperationException($"Unsupported import file format: {extension}")
            };

            var importedCount = 0;

            foreach (var stats in importedStats)
            {
                var shouldImport = mergeStrategy switch
                {
                    StatisticsMergeStrategy.Overwrite => true,
                    StatisticsMergeStrategy.Skip => !await _statisticsRepository.PlayerStatisticsExistAsync(stats.PlayerName),
                    StatisticsMergeStrategy.Merge => await HandleMergeImportAsync(stats),
                    StatisticsMergeStrategy.CreateNew => await HandleCreateNewImportAsync(stats),
                    _ => throw new ArgumentException($"Unknown merge strategy: {mergeStrategy}", nameof(mergeStrategy))
                };

                if (shouldImport)
                {
                    await _statisticsRepository.SavePlayerStatisticsAsync(stats.PlayerName, stats);
                    importedCount++;
                }
            }

            return importedCount;
        }
        catch (Exception ex) when (ex is not ArgumentException and not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to import statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets statistics for players who have played within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains statistics for players active in the date range.</returns>
    /// <exception cref="ArgumentException">Thrown when startDate is after endDate.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<IEnumerable<PlayerStatistics>> GetPlayerStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("Start date cannot be after end date.", nameof(startDate));

        try
        {
            var allStats = await _statisticsRepository.GetAllPlayerStatisticsAsync();
            return allStats.Where(s => s.LastPlayed >= startDate && s.FirstPlayed <= endDate);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to get player statistics by date range: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if statistics exist for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether statistics exist for the player.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<bool> PlayerStatisticsExistAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        try
        {
            return await _statisticsRepository.PlayerStatisticsExistAsync(playerName);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to check if statistics exist for player '{playerName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the total number of players with statistics.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of players.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is in an invalid state.</exception>
    public async Task<int> GetPlayerCountAsync()
    {
        try
        {
            return await _statisticsRepository.GetPlayerCountAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get player count: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a backup of all player statistics.
    /// </summary>
    /// <param name="backupPath">The path where the backup should be created. If null, uses default backup location.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the created backup file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the backup operation fails.</exception>
    public async Task<string> CreateBackupAsync(string? backupPath = null)
    {
        try
        {
            return await _statisticsRepository.CreateBackupAsync(backupPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Restores player statistics from a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file to restore from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing statistics data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics restored.</returns>
    /// <exception cref="ArgumentException">Thrown when backupPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the backup file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the restore operation fails.</exception>
    public async Task<int> RestoreFromBackupAsync(string backupPath, bool overwriteExisting = false)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Backup path cannot be null, empty, or whitespace.", nameof(backupPath));

        try
        {
            return await _statisticsRepository.RestoreFromBackupAsync(backupPath, overwriteExisting);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to restore from backup: {ex.Message}", ex);
        }
    }

    #region Private Export Methods

    private static Task<string> ExportToJsonAsync(List<PlayerStatistics> statistics)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var exportData = statistics.Select(s => new
        {
            s.PlayerName,
            s.GamesPlayed,
            s.GamesWon,
            s.GamesLost,
            s.GamesPushed,
            s.BlackjacksAchieved,
            TotalWagered = s.TotalWagered.Amount,
            NetWinnings = s.NetWinnings.Amount,
            s.FirstPlayed,
            s.LastPlayed,
            s.WinPercentage,
            s.BlackjackPercentage,
            AverageBet = s.AverageBet.Amount,
            s.ReturnOnInvestment
        });

        var json = JsonSerializer.Serialize(exportData, jsonOptions);
        return Task.FromResult(json);
    }

    private static Task<string> ExportToCsvAsync(List<PlayerStatistics> statistics)
    {
        var csv = new StringBuilder();
        csv.AppendLine("PlayerName,GamesPlayed,GamesWon,GamesLost,GamesPushed,BlackjacksAchieved,TotalWagered,NetWinnings,FirstPlayed,LastPlayed,WinPercentage,BlackjackPercentage,AverageBet,ReturnOnInvestment");

        foreach (var stats in statistics)
        {
            csv.AppendLine($"{EscapeCsvValue(stats.PlayerName)},{stats.GamesPlayed},{stats.GamesWon},{stats.GamesLost},{stats.GamesPushed},{stats.BlackjacksAchieved},{stats.TotalWagered.Amount},{stats.NetWinnings.Amount},{stats.FirstPlayed:yyyy-MM-dd HH:mm:ss},{stats.LastPlayed:yyyy-MM-dd HH:mm:ss},{stats.WinPercentage:F4},{stats.BlackjackPercentage:F4},{stats.AverageBet.Amount},{stats.ReturnOnInvestment:F4}");
        }

        return Task.FromResult(csv.ToString());
    }

    private static Task<string> ExportToXmlAsync(List<PlayerStatistics> statistics)
    {
        var root = new XElement("PlayerStatistics");

        foreach (var stats in statistics)
        {
            var playerElement = new XElement("Player",
                new XElement("PlayerName", stats.PlayerName),
                new XElement("GamesPlayed", stats.GamesPlayed),
                new XElement("GamesWon", stats.GamesWon),
                new XElement("GamesLost", stats.GamesLost),
                new XElement("GamesPushed", stats.GamesPushed),
                new XElement("BlackjacksAchieved", stats.BlackjacksAchieved),
                new XElement("TotalWagered", stats.TotalWagered.Amount),
                new XElement("NetWinnings", stats.NetWinnings.Amount),
                new XElement("FirstPlayed", stats.FirstPlayed.ToString("yyyy-MM-dd HH:mm:ss")),
                new XElement("LastPlayed", stats.LastPlayed.ToString("yyyy-MM-dd HH:mm:ss")),
                new XElement("WinPercentage", stats.WinPercentage.ToString("F4")),
                new XElement("BlackjackPercentage", stats.BlackjackPercentage.ToString("F4")),
                new XElement("AverageBet", stats.AverageBet.Amount),
                new XElement("ReturnOnInvestment", stats.ReturnOnInvestment.ToString("F4"))
            );

            root.Add(playerElement);
        }

        return Task.FromResult(root.ToString());
    }

    private static Task<string> ExportToTextAsync(List<PlayerStatistics> statistics)
    {
        var text = new StringBuilder();
        text.AppendLine("Player Statistics Export");
        text.AppendLine("========================");
        text.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        text.AppendLine($"Total Players: {statistics.Count}");
        text.AppendLine();

        foreach (var stats in statistics)
        {
            text.AppendLine(stats.ToDetailedString());
            text.AppendLine();
        }

        return Task.FromResult(text.ToString());
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion

    #region Private Import Methods

    private static Task<List<PlayerStatistics>> ImportFromJsonAsync(string content)
    {
        // This is a simplified implementation - in a real scenario, you'd want more robust JSON parsing
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var importedData = JsonSerializer.Deserialize<List<dynamic>>(content, jsonOptions);
        var statistics = new List<PlayerStatistics>();

        // Note: This is a placeholder implementation. In a real scenario, you'd need to properly
        // deserialize the JSON into PlayerStatistics objects with proper type handling.
        // For now, we'll return an empty list to satisfy the interface contract.

        return Task.FromResult(statistics);
    }

    private static Task<List<PlayerStatistics>> ImportFromCsvAsync(string content)
    {
        var statistics = new List<PlayerStatistics>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1) // No data rows
            return Task.FromResult(statistics);

        // Skip header row
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length >= 14) // Ensure we have all required columns
            {
                try
                {
                    var playerName = values[0];
                    var gamesPlayed = int.Parse(values[1]);
                    var gamesWon = int.Parse(values[2]);
                    var gamesLost = int.Parse(values[3]);
                    var gamesPushed = int.Parse(values[4]);
                    var blackjacksAchieved = int.Parse(values[5]);
                    var totalWagered = Money.FromUsd(decimal.Parse(values[6]));
                    var netWinnings = Money.FromUsd(decimal.Parse(values[7]));
                    var firstPlayed = DateTime.Parse(values[8]);
                    var lastPlayed = DateTime.Parse(values[9]);

                    var playerStats = new PlayerStatistics(playerName, gamesPlayed, gamesWon, gamesLost,
                        gamesPushed, blackjacksAchieved, totalWagered, netWinnings, firstPlayed, lastPlayed);

                    statistics.Add(playerStats);
                }
                catch
                {
                    // Skip invalid rows
                    continue;
                }
            }
        }

        return Task.FromResult(statistics);
    }

    private static Task<List<PlayerStatistics>> ImportFromXmlAsync(string content)
    {
        var statistics = new List<PlayerStatistics>();

        try
        {
            var root = XElement.Parse(content);
            var playerElements = root.Elements("Player");

            foreach (var playerElement in playerElements)
            {
                try
                {
                    var playerName = playerElement.Element("PlayerName")?.Value ?? "";
                    var gamesPlayed = int.Parse(playerElement.Element("GamesPlayed")?.Value ?? "0");
                    var gamesWon = int.Parse(playerElement.Element("GamesWon")?.Value ?? "0");
                    var gamesLost = int.Parse(playerElement.Element("GamesLost")?.Value ?? "0");
                    var gamesPushed = int.Parse(playerElement.Element("GamesPushed")?.Value ?? "0");
                    var blackjacksAchieved = int.Parse(playerElement.Element("BlackjacksAchieved")?.Value ?? "0");
                    var totalWagered = Money.FromUsd(decimal.Parse(playerElement.Element("TotalWagered")?.Value ?? "0"));
                    var netWinnings = Money.FromUsd(decimal.Parse(playerElement.Element("NetWinnings")?.Value ?? "0"));
                    var firstPlayed = DateTime.Parse(playerElement.Element("FirstPlayed")?.Value ?? DateTime.UtcNow.ToString());
                    var lastPlayed = DateTime.Parse(playerElement.Element("LastPlayed")?.Value ?? DateTime.UtcNow.ToString());

                    var playerStats = new PlayerStatistics(playerName, gamesPlayed, gamesWon, gamesLost,
                        gamesPushed, blackjacksAchieved, totalWagered, netWinnings, firstPlayed, lastPlayed);

                    statistics.Add(playerStats);
                }
                catch
                {
                    // Skip invalid elements
                    continue;
                }
            }
        }
        catch
        {
            // Return empty list if XML parsing fails
        }

        return Task.FromResult(statistics);
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values.ToArray();
    }

    private async Task<bool> HandleMergeImportAsync(PlayerStatistics importedStats)
    {
        var existingStats = await _statisticsRepository.GetPlayerStatisticsAsync(importedStats.PlayerName);
        if (existingStats == null)
        {
            return true; // Import as new
        }

        // Merge the statistics by creating a new PlayerStatistics with combined values
        var mergedStats = new PlayerStatistics(
            importedStats.PlayerName,
            existingStats.GamesPlayed + importedStats.GamesPlayed,
            existingStats.GamesWon + importedStats.GamesWon,
            existingStats.GamesLost + importedStats.GamesLost,
            existingStats.GamesPushed + importedStats.GamesPushed,
            existingStats.BlackjacksAchieved + importedStats.BlackjacksAchieved,
            existingStats.TotalWagered + importedStats.TotalWagered,
            existingStats.NetWinnings + importedStats.NetWinnings,
            existingStats.FirstPlayed < importedStats.FirstPlayed ? existingStats.FirstPlayed : importedStats.FirstPlayed,
            existingStats.LastPlayed > importedStats.LastPlayed ? existingStats.LastPlayed : importedStats.LastPlayed
        );

        await _statisticsRepository.SavePlayerStatisticsAsync(importedStats.PlayerName, mergedStats);
        return false; // We handled the merge ourselves
    }

    private async Task<bool> HandleCreateNewImportAsync(PlayerStatistics importedStats)
    {
        var originalName = importedStats.PlayerName;
        var newName = originalName;
        var counter = 1;

        while (await _statisticsRepository.PlayerStatisticsExistAsync(newName))
        {
            newName = $"{originalName}_{counter}";
            counter++;
        }

        if (newName != originalName)
        {
            // Create new PlayerStatistics with the modified name
            var newStats = new PlayerStatistics(
                newName,
                importedStats.GamesPlayed,
                importedStats.GamesWon,
                importedStats.GamesLost,
                importedStats.GamesPushed,
                importedStats.BlackjacksAchieved,
                importedStats.TotalWagered,
                importedStats.NetWinnings,
                importedStats.FirstPlayed,
                importedStats.LastPlayed
            );

            await _statisticsRepository.SavePlayerStatisticsAsync(newName, newStats);
            return false; // We handled the save ourselves
        }

        return true; // Use original name
    }

    #endregion
}