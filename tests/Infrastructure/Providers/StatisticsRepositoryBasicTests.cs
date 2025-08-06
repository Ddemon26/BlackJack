using System;
using System.IO;
using System.Threading.Tasks;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Providers;
using Xunit;

namespace GroupProject.Tests.Infrastructure.Providers;

/// <summary>
/// Basic unit tests for the StatisticsRepository class to verify core functionality.
/// </summary>
public class StatisticsRepositoryBasicTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly StatisticsRepository _repository;

    public StatisticsRepositoryBasicTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), "BlackjackGameTests", Guid.NewGuid().ToString());
        _repository = new StatisticsRepository(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        _repository?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
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
    public async Task SaveAndGetPlayerStatisticsAsync_WithValidData_WorksCorrectly()
    {
        // Arrange
        var playerName = "TestPlayer";
        var statistics = new PlayerStatistics(playerName);
        statistics.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        // Act
        await _repository.SavePlayerStatisticsAsync(playerName, statistics);
        var retrieved = await _repository.GetPlayerStatisticsAsync(playerName);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(playerName, retrieved.PlayerName);
        Assert.Equal(1, retrieved.GamesPlayed);
        Assert.Equal(1, retrieved.GamesWon);
        Assert.Equal(Money.FromUsd(10m), retrieved.TotalWagered);
        Assert.Equal(Money.FromUsd(10m), retrieved.NetWinnings);
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
    public async Task GetPlayerCountAsync_WithNoPlayers_ReturnsZero()
    {
        // Act
        var count = await _repository.GetPlayerCountAsync();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetPlayerCountAsync_WithOnePlayers_ReturnsOne()
    {
        // Arrange
        var statistics = new PlayerStatistics("TestPlayer");
        await _repository.SavePlayerStatisticsAsync("TestPlayer", statistics);

        // Act
        var count = await _repository.GetPlayerCountAsync();

        // Assert
        Assert.Equal(1, count);
    }
}