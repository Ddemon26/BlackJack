using System.Text.Json;
using System.Text.Json.Serialization;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Infrastructure.Providers;

/// <summary>
/// File-based implementation of the statistics repository using JSON serialization.
/// Provides persistent storage for player statistics with backup and migration capabilities.
/// </summary>
/// <remarks>
/// This implementation stores player statistics in JSON files within a dedicated statistics directory.
/// It supports data migration, backup creation, and restoration to ensure data integrity and recovery.
/// The repository is thread-safe for concurrent read operations but serializes write operations.
/// </remarks>
public class StatisticsRepository : IStatisticsRepository, IDisposable
{
    private readonly string _statisticsDirectory;
    private readonly string _backupDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _writeLock;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the StatisticsRepository class.
    /// </summary>
    /// <param name="statisticsDirectory">The directory where statistics files are stored. If null, uses default location.</param>
    public StatisticsRepository(string? statisticsDirectory = null)
    {
        _statisticsDirectory = statisticsDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlackjackGame", "Statistics");
        _backupDirectory = Path.Combine(_statisticsDirectory, "Backups");
        _writeLock = new SemaphoreSlim(1, 1);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Retrieves player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the player statistics, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public async Task<PlayerStatistics?> GetPlayerStatisticsAsync(string playerName)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        try
        {
            var filePath = GetPlayerStatisticsFilePath(playerName);
            
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            var dto = JsonSerializer.Deserialize<PlayerStatisticsDto>(json, _jsonOptions);
            
            return dto?.ToPlayerStatistics();
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to retrieve statistics for player '{playerName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Saves or updates player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="statistics">The player statistics to save.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public async Task SavePlayerStatisticsAsync(string playerName, PlayerStatistics statistics)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            var filePath = GetPlayerStatisticsFilePath(playerName);
            var dto = PlayerStatisticsDto.FromPlayerStatistics(statistics);
            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save statistics for player '{playerName}': {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Retrieves statistics for all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public async Task<IEnumerable<PlayerStatistics>> GetAllPlayerStatisticsAsync()
    {
        ThrowIfDisposed();
        
        try
        {
            var statisticsFiles = Directory.GetFiles(_statisticsDirectory, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("backup_", StringComparison.OrdinalIgnoreCase));

            var allStatistics = new List<PlayerStatistics>();

            foreach (var filePath in statisticsFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var dto = JsonSerializer.Deserialize<PlayerStatisticsDto>(json, _jsonOptions);
                    
                    if (dto != null)
                    {
                        var statistics = dto.ToPlayerStatistics();
                        allStatistics.Add(statistics);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other files
                    Console.WriteLine($"Warning: Failed to load statistics from {filePath}: {ex.Message}");
                }
            }

            return allStatistics;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve all player statistics: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes player statistics for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player whose statistics should be deleted.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public async Task<bool> DeletePlayerStatisticsAsync(string playerName)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            var filePath = GetPlayerStatisticsFilePath(playerName);
            
            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete statistics for player '{playerName}': {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Checks if statistics exist for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether statistics exist for the player.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public Task<bool> PlayerStatisticsExistAsync(string playerName)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        try
        {
            var filePath = GetPlayerStatisticsFilePath(playerName);
            return Task.FromResult(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to check if statistics exist for player '{playerName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a backup of all player statistics data.
    /// </summary>
    /// <param name="backupPath">The path where the backup should be created. If null, uses default backup location.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path to the created backup file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the backup operation fails.</exception>
    public async Task<string> CreateBackupAsync(string? backupPath = null)
    {
        ThrowIfDisposed();
        
        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var defaultBackupFileName = $"backup_{timestamp}.json";
            var finalBackupPath = backupPath ?? Path.Combine(_backupDirectory, defaultBackupFileName);

            // Ensure backup directory exists
            var backupDir = Path.GetDirectoryName(finalBackupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var allStatistics = await GetAllPlayerStatisticsAsync();
            var backupData = new StatisticsBackupDto
            {
                BackupTimestamp = DateTime.UtcNow,
                Version = "1.0",
                PlayerStatistics = allStatistics.Select(PlayerStatisticsDto.FromPlayerStatistics).ToList()
            };

            var json = JsonSerializer.Serialize(backupData, _jsonOptions);
            await File.WriteAllTextAsync(finalBackupPath, json);

            return finalBackupPath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create backup: {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Restores player statistics data from a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file to restore from.</param>
    /// <param name="overwriteExisting">Whether to overwrite existing statistics data.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics restored.</returns>
    /// <exception cref="ArgumentException">Thrown when backupPath is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the backup file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the restore operation fails.</exception>
    public async Task<int> RestoreFromBackupAsync(string backupPath, bool overwriteExisting = false)
    {
        ThrowIfDisposed();
        
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Backup path cannot be null, empty, or whitespace.", nameof(backupPath));

        if (!File.Exists(backupPath))
            throw new FileNotFoundException($"Backup file not found: {backupPath}");

        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            var json = await File.ReadAllTextAsync(backupPath);
            var backupData = JsonSerializer.Deserialize<StatisticsBackupDto>(json, _jsonOptions);

            if (backupData?.PlayerStatistics == null)
                throw new InvalidOperationException("Invalid backup file format.");

            var restoredCount = 0;

            foreach (var statisticsDto in backupData.PlayerStatistics)
            {
                var statistics = statisticsDto.ToPlayerStatistics();
                var exists = await PlayerStatisticsExistAsync(statistics.PlayerName);

                if (!exists || overwriteExisting)
                {
                    // Use internal save method to avoid recursive semaphore wait
                    var filePath = GetPlayerStatisticsFilePath(statistics.PlayerName);
                    var dto = PlayerStatisticsDto.FromPlayerStatistics(statistics);
                    var statisticsJson = JsonSerializer.Serialize(dto, _jsonOptions);
                    await File.WriteAllTextAsync(filePath, statisticsJson);
                    restoredCount++;
                }
            }

            return restoredCount;
        }
        catch (Exception ex) when (ex is not ArgumentException and not FileNotFoundException)
        {
            throw new InvalidOperationException($"Failed to restore from backup: {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Migrates statistics data from an older format to the current format.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result indicates the number of player statistics migrated.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the migration operation fails.</exception>
    public async Task<int> MigrateDataAsync()
    {
        ThrowIfDisposed();
        
        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            // For now, this is a placeholder for future migration logic
            // In a real implementation, this would handle migration from older file formats
            var allStatistics = await GetAllPlayerStatisticsAsync();
            
            // Check if any migration is needed (placeholder logic)
            var migratedCount = 0;
            
            foreach (var statistics in allStatistics)
            {
                // Placeholder: Re-save all statistics to ensure they're in the current format
                // Use internal save method to avoid recursive semaphore wait
                var filePath = GetPlayerStatisticsFilePath(statistics.PlayerName);
                var dto = PlayerStatisticsDto.FromPlayerStatistics(statistics);
                var json = JsonSerializer.Serialize(dto, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                migratedCount++;
            }

            return migratedCount;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to migrate data: {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the total number of player statistics records in the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the total count of player statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository is in an invalid state.</exception>
    public Task<int> GetPlayerCountAsync()
    {
        ThrowIfDisposed();
        
        try
        {
            var statisticsFiles = Directory.GetFiles(_statisticsDirectory, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("backup_", StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(statisticsFiles.Count());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get player count: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Clears all player statistics data from the repository.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the clear operation fails.</exception>
    public async Task ClearAllStatisticsAsync()
    {
        ThrowIfDisposed();
        
        await _writeLock.WaitAsync();
        try
        {
            ThrowIfDisposed();
            var statisticsFiles = Directory.GetFiles(_statisticsDirectory, "*.json")
                .Where(f => !Path.GetFileName(f).StartsWith("backup_", StringComparison.OrdinalIgnoreCase));

            foreach (var filePath in statisticsFiles)
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to clear all statistics: {ex.Message}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Gets the file path for a player's statistics file.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The full path to the player's statistics file.</returns>
    private string GetPlayerStatisticsFilePath(string playerName)
    {
        // Sanitize the player name for use as a filename
        var sanitizedName = string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_statisticsDirectory, $"{sanitizedName}.json");
    }

    /// <summary>
    /// Ensures that the required directories exist.
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(_statisticsDirectory))
        {
            Directory.CreateDirectory(_statisticsDirectory);
        }

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the repository has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(StatisticsRepository));
    }

    /// <summary>
    /// Releases the resources used by the StatisticsRepository.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the StatisticsRepository.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _writeLock?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Data transfer object for JSON serialization of player statistics.
/// </summary>
internal class PlayerStatisticsDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    public int GamesPushed { get; set; }
    public int BlackjacksAchieved { get; set; }
    public MoneyDto TotalWagered { get; set; } = new();
    public MoneyDto NetWinnings { get; set; } = new();
    public DateTime FirstPlayed { get; set; }
    public DateTime LastPlayed { get; set; }

    public static PlayerStatisticsDto FromPlayerStatistics(PlayerStatistics statistics)
    {
        return new PlayerStatisticsDto
        {
            PlayerName = statistics.PlayerName,
            GamesPlayed = statistics.GamesPlayed,
            GamesWon = statistics.GamesWon,
            GamesLost = statistics.GamesLost,
            GamesPushed = statistics.GamesPushed,
            BlackjacksAchieved = statistics.BlackjacksAchieved,
            TotalWagered = MoneyDto.FromMoney(statistics.TotalWagered),
            NetWinnings = MoneyDto.FromMoney(statistics.NetWinnings),
            FirstPlayed = statistics.FirstPlayed,
            LastPlayed = statistics.LastPlayed
        };
    }

    public PlayerStatistics ToPlayerStatistics()
    {
        return new PlayerStatistics(
            PlayerName,
            GamesPlayed,
            GamesWon,
            GamesLost,
            GamesPushed,
            BlackjacksAchieved,
            TotalWagered.ToMoney(),
            NetWinnings.ToMoney(),
            FirstPlayed,
            LastPlayed
        );
    }
}

/// <summary>
/// Data transfer object for JSON serialization of Money values.
/// </summary>
internal class MoneyDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public static MoneyDto FromMoney(Money money)
    {
        return new MoneyDto
        {
            Amount = money.Amount,
            Currency = money.Currency
        };
    }

    public Money ToMoney()
    {
        return new Money(Amount, Currency);
    }
}

/// <summary>
/// Data transfer object for backup files containing multiple player statistics.
/// </summary>
internal class StatisticsBackupDto
{
    public DateTime BackupTimestamp { get; set; }
    public string Version { get; set; } = "1.0";
    public List<PlayerStatisticsDto> PlayerStatistics { get; set; } = new();
}