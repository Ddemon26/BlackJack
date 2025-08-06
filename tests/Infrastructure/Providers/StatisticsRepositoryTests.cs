using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using Xunit;

namespace GroupProject.Tests.Infrastructure.Providers;

/// <summary>
/// Unit tests for the StatisticsRepository class.
/// Tests file-based persistence, backup/restore functionality, and data migration capabilities.
/// </summary>
public class StatisticsRepositoryTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly StatisticsRepository _repository;

    public StatisticsRepositoryTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), "BlackjackGameTests", Guid.NewGuid().ToString());
        _repository = new StatisticsRepository(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        _repository.Dispose();
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WhenPlayerDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetPlayerStatisticsAsync("NonExistentPlayer");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetPlayerStatisticsAsync(null!));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithEmptyPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetPlayerStatisticsAsync(""));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithWhitespacePlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.GetPlayerStatisticsAsync("   "));
    }

    [Fact]
    public async Task SavePlayerStatisticsAsync_WithValidData_SavesSuccessfully()
    {
        // Arrange
        var playerName = "TestPlayer";
        var statistics = new PlayerStatistics(playerName);
        statistics.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        // Act
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);

        // Assert
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);
        Assert.NotNull(retrieved);
        Assert.Equal(playerName, retrieved.PlayerName);
        Assert.Equal(1, retrieved.GamesPlayed);
        Assert.Equal(1, retrieved.GamesWon);
        Assert.Equal(Money.FromUsd(10m), retrieved.TotalWagered);
        Assert.Equal(Money.FromUsd(10m), retrieved.NetWinnings);
    }

    [Fact]
    public async Task SavePlayerStatisticsAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Arrange
        var statistics = new PlayerStatistics("TestPlayer");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.SavePlayerStatisticsAsync(null!, statistics));
    }

    [Fact]
    public async Task SavePlayerStatisticsAsync_WithNullStatistics_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.SavePlayerStatisticsAsync("TestPlayer", null!));
    }

    [Fact]
    public async Task SavePlayerStatisticsAsync_OverwritesExistingData()
    {
        // Arrange
        var playerName = "TestPlayer";
        var originalStats = new PlayerStatistics(playerName);
        originalStats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var updatedStats = new PlayerStatistics(playerName);
        updatedStats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));
        updatedStats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);

        // Act
        await _repository.SavePlayerStatisticsAsync(playerName, originalStats);
        await _repository.SavePlayerStatisticsAsync(playerName, updatedStats);

        // Assert
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.GamesPlayed);
        Assert.Equal(1, retrieved.GamesWon);
        Assert.Equal(1, retrieved.GamesLost);
    }

    [Fact]
    public async Task GetAllPlayerStatisticsAsync_WithNoPlayers_ReturnsEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllPlayerStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllPlayerStatisticsAsync_WithMultiplePlayers_ReturnsAllPlayers()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var player2Stats = new PlayerStatistics("Player2");
        player2Stats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);

        var player3Stats = new PlayerStatistics("Player3");
        player3Stats.RecordGame(GameResult.Blackjack, Money.FromUsd(20m), Money.FromUsd(30m));

        // Act
        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);
        await _repository.SavePlayerStatisticsAsync("Player3", player3Stats);

        var result = await _repository.GetAllPlayerStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        
        var playerNames = result.Select(s => s.PlayerName).ToList();
        Assert.Contains("Player1", playerNames);
        Assert.Contains("Player2", playerNames);
        Assert.Contains("Player3", playerNames);
    }

    [Fact]
    public async Task DeletePlayerStatisticsAsync_WhenPlayerExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var playerName = "TestPlayer";
        var statistics = new PlayerStatistics(playerName);
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);

        // Act
        var result = await _repository.DeletePlayerStatisticsAsync(playerName);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeletePlayerStatisticsAsync_WhenPlayerDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeletePlayerStatisticsAsync("NonExistentPlayer");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeletePlayerStatisticsAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.DeletePlayerStatisticsAsync(null!));
    }

    [Fact]
    public async Task PlayerStatisticsExistAsync_WhenPlayerExists_ReturnsTrue()
    {
        // Arrange
        var playerName = "TestPlayer";
        var statistics = new PlayerStatistics(playerName);
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);

        // Act
        var result = await _repository.PlayerStatisticsExistAsync(playerName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PlayerStatisticsExistAsync_WhenPlayerDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await _repository.PlayerStatisticsExistAsync("NonExistentPlayer");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PlayerStatisticsExistAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.PlayerStatisticsExistAsync(null!));
    }

    [Fact]
    public async Task CreateBackupAsync_WithDefaultPath_CreatesBackupFile()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var player2Stats = new PlayerStatistics("Player2");
        player2Stats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);

        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);

        // Act
        var backupPath = await _repository.CreateBackupAsync();

        // Assert
        Assert.NotNull(backupPath);
        Assert.True(File.Exists(backupPath));
        Assert.Contains("backup_", Path.GetFileName(backupPath));
        Assert.EndsWith(".json", backupPath);

        // Verify backup content
        var backupContent = await File.ReadAllTextAsync(backupPath);
        Assert.Contains("Player1", backupContent);
        Assert.Contains("Player2", backupContent);
    }

    [Fact]
    public async Task CreateBackupAsync_WithCustomPath_CreatesBackupAtSpecifiedLocation()
    {
        // Arrange
        var customBackupPath = Path.Combine(_testDirectory, "custom_backup.json");
        var statistics = new PlayerStatistics("TestPlayer");
        await _repository.SavePlayerStatisticsAsync("TestPlayer", statistics);

        // Act
        var backupPath = await _repository.CreateBackupAsync(customBackupPath);

        // Assert
        Assert.Equal(customBackupPath, backupPath);
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidBackup_RestoresData()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var player2Stats = new PlayerStatistics("Player2");
        player2Stats.RecordGame(GameResult.Blackjack, Money.FromUsd(20m), Money.FromUsd(30m));

        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);

        var backupPath = await _repository.CreateBackupAsync();

        // Clear existing data
        await _repository.ClearAllStatisticsAsync();

        // Act
        var restoredCount = await _repository.RestoreFromBackupAsync(backupPath);

        // Assert
        Assert.Equal(2, restoredCount);

        var restoredPlayer1 = await _repository.GetPlayerStatisticsAsync("Player1");
        var restoredPlayer2 = await _repository.GetPlayerStatisticsAsync("Player2");

        Assert.NotNull(restoredPlayer1);
        Assert.NotNull(restoredPlayer2);
        Assert.Equal(1, restoredPlayer1.GamesWon);
        Assert.Equal(1, restoredPlayer2.BlackjacksAchieved);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _repository.RestoreFromBackupAsync("nonexistent_backup.json"));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _repository.RestoreFromBackupAsync(null!));
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithOverwriteFalse_DoesNotOverwriteExistingData()
    {
        // Arrange
        var originalStats = new PlayerStatistics("TestPlayer");
        originalStats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));
        await _repository.SavePlayerStatisticsAsync("TestPlayer", originalStats);

        var backupStats = new PlayerStatistics("TestPlayer");
        backupStats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);
        backupStats.RecordGame(GameResult.Win, Money.FromUsd(15m), Money.FromUsd(15m));

        // Create a separate repository for backup creation
        var backupDirectory = Path.Combine(Path.GetTempPath(), "BackupTest", Guid.NewGuid().ToString());
        var backupRepository = new StatisticsRepository(backupDirectory);
        await backupRepository.SavePlayerStatisticsAsync("TestPlayer", backupStats);
        var backupPath = await backupRepository.CreateBackupAsync();

        // Act
        var restoredCount = await _repository.RestoreFromBackupAsync(backupPath, overwriteExisting: false);

        // Assert
        Assert.Equal(0, restoredCount); // Should not restore because player already exists

        var currentStats = await _repository.GetPlayerStatisticsAsync("TestPlayer");
        Assert.NotNull(currentStats);
        Assert.Equal(1, currentStats.GamesPlayed); // Should still have original data

        // Cleanup
        backupRepository.Dispose();
        if (Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithOverwriteTrue_OverwritesExistingData()
    {
        // Arrange
        var originalStats = new PlayerStatistics("TestPlayer");
        originalStats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));
        await _repository.SavePlayerStatisticsAsync("TestPlayer", originalStats);

        var backupStats = new PlayerStatistics("TestPlayer");
        backupStats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);
        backupStats.RecordGame(GameResult.Win, Money.FromUsd(15m), Money.FromUsd(15m));

        // Create a separate repository for backup creation
        var backupDirectory = Path.Combine(Path.GetTempPath(), "BackupTest", Guid.NewGuid().ToString());
        var backupRepository = new StatisticsRepository(backupDirectory);
        await backupRepository.SavePlayerStatisticsAsync("TestPlayer", backupStats);
        var backupPath = await backupRepository.CreateBackupAsync();

        // Act
        var restoredCount = await _repository.RestoreFromBackupAsync(backupPath, overwriteExisting: true);

        // Assert
        Assert.Equal(1, restoredCount); // Should restore and overwrite

        var currentStats = await _repository.GetPlayerStatisticsAsync("TestPlayer");
        Assert.NotNull(currentStats);
        Assert.Equal(2, currentStats.GamesPlayed); // Should have backup data

        // Cleanup
        backupRepository.Dispose();
        if (Directory.Exists(backupDirectory))
        {
            Directory.Delete(backupDirectory, true);
        }
    }

    [Fact]
    public async Task MigrateDataAsync_WithExistingData_ReturnsCorrectCount()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        var player2Stats = new PlayerStatistics("Player2");
        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);

        // Act
        var migratedCount = await _repository.MigrateDataAsync();

        // Assert
        Assert.Equal(2, migratedCount);
    }

    [Fact]
    public async Task GetPlayerCountAsync_WithNoPlayers_ReturnsZero()
    {
        // Act
        var count = await _repository.GetPlayerCountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetPlayerCountAsync_WithMultiplePlayers_ReturnsCorrectCount()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        var player2Stats = new PlayerStatistics("Player2");
        var player3Stats = new PlayerStatistics("Player3");

        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);
        await _repository.SavePlayerStatisticsAsync("Player3", player3Stats);

        // Act
        var count = await _repository.GetPlayerCountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ClearAllStatisticsAsync_RemovesAllPlayerData()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        var player2Stats = new PlayerStatistics("Player2");
        await _repository.SavePlayerStatisticsAsync("Player1", player1Stats);
        await _repository.SavePlayerStatisticsAsync("Player2", player2Stats);

        // Verify data exists
        var initialCount = await _repository.GetPlayerCountAsync();
        Assert.Equal(2, initialCount);

        // Act
        await _repository.ClearAllStatisticsAsync();

        // Assert
        var finalCount = await _repository.GetPlayerCountAsync();
        Assert.Equal(0, finalCount);

        var allStats = await _repository.GetAllPlayerStatisticsAsync();
        Assert.Empty(allStats);
    }

    [Fact]
    public async Task SavePlayerStatisticsAsync_WithSpecialCharactersInPlayerName_HandlesCorrectly()
    {
        // Arrange
        var playerName = "Player/With\\Special:Characters*?\"<>|";
        var statistics = new PlayerStatistics(playerName);
        statistics.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        // Act
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);

        // Assert
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);
        Assert.NotNull(retrieved);
        Assert.Equal(playerName, retrieved.PlayerName);
    }

    [Fact]
    public async Task Repository_ConcurrentOperations_HandlesCorrectly()
    {
        // Arrange
        var tasks = new List<Task>();
        var playerCount = 10;

        // Act - Perform concurrent save operations
        for (int i = 0; i < playerCount; i++)
        {
            var playerName = $"Player{i}";
            var statistics = new PlayerStatistics(playerName);
            statistics.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));
            
            tasks.Add(_repository.SavePlayerStatisticsAsync(playerName, statistics));
        }

        await Task.WhenAll(tasks);

        // Assert
        var finalCount = await _repository.GetPlayerCountAsync();
        Assert.Equal(playerCount, finalCount);

        var allStats = await _repository.GetAllPlayerStatisticsAsync();
        Assert.Equal(playerCount, allStats.Count());
    }

    [Theory]
    [InlineData(GameResult.Win)]
    [InlineData(GameResult.Lose)]
    [InlineData(GameResult.Push)]
    [InlineData(GameResult.Blackjack)]
    public async Task SaveAndRetrievePlayerStatistics_WithDifferentGameResults_PreservesData(GameResult gameResult)
    {
        // Arrange
        var playerName = "TestPlayer";
        var statistics = new PlayerStatistics(playerName);
        var betAmount = Money.FromUsd(25m);
        var payout = gameResult switch
        {
            GameResult.Win => Money.FromUsd(25m),
            GameResult.Blackjack => Money.FromUsd(37.50m),
            GameResult.Push => Money.Zero,
            GameResult.Lose => Money.Zero,
            _ => Money.Zero
        };

        statistics.RecordGame(gameResult, betAmount, payout);

        // Act
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.GamesPlayed);
        Assert.Equal(betAmount, retrieved.TotalWagered);

        switch (gameResult)
        {
            case GameResult.Win:
                Assert.Equal(1, retrieved.GamesWon);
                Assert.Equal(0, retrieved.BlackjacksAchieved);
                break;
            case GameResult.Blackjack:
                Assert.Equal(1, retrieved.GamesWon);
                Assert.Equal(1, retrieved.BlackjacksAchieved);
                break;
            case GameResult.Lose:
                Assert.Equal(1, retrieved.GamesLost);
                break;
            case GameResult.Push:
                Assert.Equal(1, retrieved.GamesPushed);
                break;
        }
    }
}