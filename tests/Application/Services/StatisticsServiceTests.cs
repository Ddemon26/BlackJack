using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Application.Services;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GroupProject.Tests.Application.Services;

/// <summary>
/// Unit tests for the StatisticsService class.
/// Tests statistics management, aggregation, and export/import functionality.
/// </summary>
public class StatisticsServiceTests : IDisposable
{
    private readonly Mock<IStatisticsRepository> _mockRepository;
    private readonly StatisticsService _statisticsService;
    private readonly string _testDirectory;

    public StatisticsServiceTests()
    {
        _mockRepository = new Mock<IStatisticsRepository>();
        _statisticsService = new StatisticsService(_mockRepository.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), "StatisticsServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StatisticsService(null!));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithValidPlayerName_ReturnsStatistics()
    {
        // Arrange
        var playerName = "TestPlayer";
        var expectedStats = new PlayerStatistics(playerName);
        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(playerName))
                      .ReturnsAsync(expectedStats);

        // Act
        var result = await _statisticsService.GetPlayerStatisticsAsync(playerName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playerName, result.PlayerName);
        _mockRepository.Verify(r => r.GetPlayerStatisticsAsync(playerName), Times.Once);
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statisticsService.GetPlayerStatisticsAsync(null!));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithEmptyPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statisticsService.GetPlayerStatisticsAsync(""));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WithWhitespacePlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _statisticsService.GetPlayerStatisticsAsync("   "));
    }

    [Fact]
    public async Task GetPlayerStatisticsAsync_WhenRepositoryThrowsException_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(It.IsAny<string>()))
                      .ThrowsAsync(new InvalidOperationException("Repository error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _statisticsService.GetPlayerStatisticsAsync("TestPlayer"));
        Assert.Contains("Failed to retrieve statistics", exception.Message);
    }

    [Fact]
    public async Task UpdatePlayerStatisticsAsync_WithNewPlayer_CreatesAndSavesStatistics()
    {
        // Arrange
        var playerName = "NewPlayer";
        var gameResult = GameResult.Win;
        var betAmount = Money.FromUsd(10m);
        var payout = Money.FromUsd(10m);

        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(playerName))
                      .ReturnsAsync((PlayerStatistics?)null);

        // Act
        await _statisticsService.UpdatePlayerStatisticsAsync(playerName, gameResult, betAmount, payout);

        // Assert
        _mockRepository.Verify(r => r.GetPlayerStatisticsAsync(playerName), Times.Once);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync(playerName, It.IsAny<PlayerStatistics>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayerStatisticsAsync_WithExistingPlayer_UpdatesAndSavesStatistics()
    {
        // Arrange
        var playerName = "ExistingPlayer";
        var existingStats = new PlayerStatistics(playerName);
        var gameResult = GameResult.Win;
        var betAmount = Money.FromUsd(10m);
        var payout = Money.FromUsd(10m);

        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(playerName))
                      .ReturnsAsync(existingStats);

        // Act
        await _statisticsService.UpdatePlayerStatisticsAsync(playerName, gameResult, betAmount, payout);

        // Assert
        _mockRepository.Verify(r => r.GetPlayerStatisticsAsync(playerName), Times.Once);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync(playerName, It.IsAny<PlayerStatistics>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlayerStatisticsAsync_WithNullPlayerName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _statisticsService.UpdatePlayerStatisticsAsync(null!, GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m)));
    }

    [Fact]
    public async Task UpdatePlayerStatisticsAsync_WithNegativeBetAmount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _statisticsService.UpdatePlayerStatisticsAsync("Player", GameResult.Win, Money.FromUsd(-10m), Money.FromUsd(10m)));
    }

    [Fact]
    public async Task UpdatePlayerStatisticsAsync_WithNegativePayout_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _statisticsService.UpdatePlayerStatisticsAsync("Player", GameResult.Win, Money.FromUsd(10m), Money.FromUsd(-10m)));
    }

    [Fact]
    public async Task GetAllPlayerStatisticsAsync_ReturnsAllStatistics()
    {
        // Arrange
        var expectedStats = new List<PlayerStatistics>
        {
            new("Player1"),
            new("Player2"),
            new("Player3")
        };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(expectedStats);

        // Act
        var result = await _statisticsService.GetAllPlayerStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockRepository.Verify(r => r.GetAllPlayerStatisticsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAggregatedStatisticsAsync_WithNoPlayers_ReturnsEmptyAggregatedStatistics()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(new List<PlayerStatistics>());

        // Act
        var result = await _statisticsService.GetAggregatedStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPlayers);
        Assert.Equal(0, result.TotalGamesPlayed);
    }

    [Fact]
    public async Task GetAggregatedStatisticsAsync_WithMultiplePlayers_ReturnsCorrectAggregation()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));
        player1Stats.RecordGame(GameResult.Lose, Money.FromUsd(5m), Money.Zero);

        var player2Stats = new PlayerStatistics("Player2");
        player2Stats.RecordGame(GameResult.Blackjack, Money.FromUsd(20m), Money.FromUsd(30m));

        var allStats = new List<PlayerStatistics> { player1Stats, player2Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        // Act
        var result = await _statisticsService.GetAggregatedStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalPlayers);
        Assert.Equal(3, result.TotalGamesPlayed);
        Assert.Equal(2, result.TotalGamesWon);
        Assert.Equal(1, result.TotalGamesLost);
        Assert.Equal(0, result.TotalGamesPushed);
        Assert.Equal(1, result.TotalBlackjacksAchieved);
        Assert.Equal(Money.FromUsd(35m), result.TotalAmountWagered);
        Assert.Equal(Money.FromUsd(35m), result.TotalNetWinnings); // 10 - 5 + 30 = 35
    }

    [Theory]
    [InlineData(StatisticsMetric.GamesPlayed)]
    [InlineData(StatisticsMetric.GamesWon)]
    [InlineData(StatisticsMetric.WinPercentage)]
    [InlineData(StatisticsMetric.NetWinnings)]
    [InlineData(StatisticsMetric.TotalWagered)]
    [InlineData(StatisticsMetric.BlackjacksAchieved)]
    [InlineData(StatisticsMetric.BlackjackPercentage)]
    [InlineData(StatisticsMetric.ReturnOnInvestment)]
    [InlineData(StatisticsMetric.AverageBet)]
    [InlineData(StatisticsMetric.TotalPlayTime)]
    public async Task GetTopPlayersAsync_WithValidMetric_ReturnsTopPlayers(StatisticsMetric metric)
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var player2Stats = new PlayerStatistics("Player2");
        player2Stats.RecordGame(GameResult.Win, Money.FromUsd(20m), Money.FromUsd(20m));
        player2Stats.RecordGame(GameResult.Win, Money.FromUsd(15m), Money.FromUsd(15m));

        var allStats = new List<PlayerStatistics> { player1Stats, player2Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        // Act
        var result = await _statisticsService.GetTopPlayersAsync(metric, 5);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() <= 5);
        _mockRepository.Verify(r => r.GetAllPlayerStatisticsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetTopPlayersAsync_WithInvalidCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _statisticsService.GetTopPlayersAsync(StatisticsMetric.GamesPlayed, 0));
    }

    [Fact]
    public async Task ResetPlayerStatisticsAsync_WithExistingPlayer_ResetsAndReturnsTrue()
    {
        // Arrange
        var playerName = "TestPlayer";
        var existingStats = new PlayerStatistics(playerName);
        existingStats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(playerName))
                      .ReturnsAsync(existingStats);

        // Act
        var result = await _statisticsService.ResetPlayerStatisticsAsync(playerName);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetPlayerStatisticsAsync(playerName), Times.Once);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync(playerName, It.IsAny<PlayerStatistics>()), Times.Once);
    }

    [Fact]
    public async Task ResetPlayerStatisticsAsync_WithNonExistentPlayer_ReturnsFalse()
    {
        // Arrange
        var playerName = "NonExistentPlayer";
        _mockRepository.Setup(r => r.GetPlayerStatisticsAsync(playerName))
                      .ReturnsAsync((PlayerStatistics?)null);

        // Act
        var result = await _statisticsService.ResetPlayerStatisticsAsync(playerName);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetPlayerStatisticsAsync(playerName), Times.Once);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync(It.IsAny<string>(), It.IsAny<PlayerStatistics>()), Times.Never);
    }

    [Fact]
    public async Task ResetAllPlayerStatisticsAsync_WithMultiplePlayers_ResetsAllAndReturnsCount()
    {
        // Arrange
        var player1Stats = new PlayerStatistics("Player1");
        var player2Stats = new PlayerStatistics("Player2");
        var allStats = new List<PlayerStatistics> { player1Stats, player2Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        // Act
        var result = await _statisticsService.ResetAllPlayerStatisticsAsync();

        // Assert
        Assert.Equal(2, result);
        _mockRepository.Verify(r => r.GetAllPlayerStatisticsAsync(), Times.Once);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync(It.IsAny<string>(), It.IsAny<PlayerStatistics>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(StatisticsExportFormat.Json)]
    [InlineData(StatisticsExportFormat.Csv)]
    [InlineData(StatisticsExportFormat.Xml)]
    [InlineData(StatisticsExportFormat.Text)]
    public async Task ExportStatisticsAsync_WithValidFormat_CreatesExportFile(StatisticsExportFormat format)
    {
        // Arrange
        var exportPath = Path.Combine(_testDirectory, $"export.{format.ToString().ToLower()}");
        var player1Stats = new PlayerStatistics("Player1");
        player1Stats.RecordGame(GameResult.Win, Money.FromUsd(10m), Money.FromUsd(10m));

        var allStats = new List<PlayerStatistics> { player1Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        // Act
        var result = await _statisticsService.ExportStatisticsAsync(exportPath, format);

        // Assert
        Assert.Equal(exportPath, result);
        Assert.True(File.Exists(exportPath));
        _mockRepository.Verify(r => r.GetAllPlayerStatisticsAsync(), Times.Once);
    }

    [Fact]
    public async Task ExportStatisticsAsync_WithNullExportPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _statisticsService.ExportStatisticsAsync(null!, StatisticsExportFormat.Json));
    }

    [Fact]
    public async Task ExportStatisticsAsync_WithSpecificPlayers_ExportsOnlySpecifiedPlayers()
    {
        // Arrange
        var exportPath = Path.Combine(_testDirectory, "specific_export.json");
        var player1Stats = new PlayerStatistics("Player1");
        var player2Stats = new PlayerStatistics("Player2");
        var player3Stats = new PlayerStatistics("Player3");
        var allStats = new List<PlayerStatistics> { player1Stats, player2Stats, player3Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        var specificPlayers = new[] { "Player1", "Player3" };

        // Act
        var result = await _statisticsService.ExportStatisticsAsync(exportPath, StatisticsExportFormat.Json, specificPlayers);

        // Assert
        Assert.Equal(exportPath, result);
        Assert.True(File.Exists(exportPath));
        
        var exportContent = await File.ReadAllTextAsync(exportPath);
        Assert.Contains("Player1", exportContent);
        Assert.Contains("Player3", exportContent);
        Assert.DoesNotContain("Player2", exportContent);
    }

    [Fact]
    public async Task ImportStatisticsAsync_WithNullImportPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _statisticsService.ImportStatisticsAsync(null!));
    }

    [Fact]
    public async Task ImportStatisticsAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _statisticsService.ImportStatisticsAsync("nonexistent.json"));
    }

    [Fact]
    public async Task ImportStatisticsAsync_WithValidCsvFile_ImportsStatistics()
    {
        // Arrange
        var csvContent = "PlayerName,GamesPlayed,GamesWon,GamesLost,GamesPushed,BlackjacksAchieved,TotalWagered,NetWinnings,FirstPlayed,LastPlayed,WinPercentage,BlackjackPercentage,AverageBet,ReturnOnInvestment\n" +
                        "TestPlayer,2,1,1,0,0,15.00,5.00,2023-01-01 10:00:00,2023-01-01 11:00:00,0.5000,0.0000,7.50,0.3333";

        var csvPath = Path.Combine(_testDirectory, "import.csv");
        await File.WriteAllTextAsync(csvPath, csvContent);

        _mockRepository.Setup(r => r.PlayerStatisticsExistAsync(It.IsAny<string>()))
                      .ReturnsAsync(false);

        // Act
        var result = await _statisticsService.ImportStatisticsAsync(csvPath);

        // Assert
        Assert.Equal(1, result);
        _mockRepository.Verify(r => r.SavePlayerStatisticsAsync("TestPlayer", It.IsAny<PlayerStatistics>()), Times.Once);
    }

    [Fact]
    public async Task GetPlayerStatisticsByDateRangeAsync_WithValidDateRange_ReturnsFilteredStatistics()
    {
        // Arrange
        var startDate = new DateTime(2023, 1, 1);
        var endDate = new DateTime(2023, 12, 31);

        var player1Stats = new PlayerStatistics("Player1", 1, 1, 0, 0, 0, Money.FromUsd(10m), Money.FromUsd(10m), 
            new DateTime(2023, 6, 1), new DateTime(2023, 6, 1));
        var player2Stats = new PlayerStatistics("Player2", 1, 1, 0, 0, 0, Money.FromUsd(10m), Money.FromUsd(10m), 
            new DateTime(2022, 6, 1), new DateTime(2022, 6, 1));

        var allStats = new List<PlayerStatistics> { player1Stats, player2Stats };

        _mockRepository.Setup(r => r.GetAllPlayerStatisticsAsync())
                      .ReturnsAsync(allStats);

        // Act
        var result = await _statisticsService.GetPlayerStatisticsByDateRangeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Player1", result.First().PlayerName);
    }

    [Fact]
    public async Task GetPlayerStatisticsByDateRangeAsync_WithInvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var startDate = new DateTime(2023, 12, 31);
        var endDate = new DateTime(2023, 1, 1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _statisticsService.GetPlayerStatisticsByDateRangeAsync(startDate, endDate));
    }

    [Fact]
    public async Task PlayerStatisticsExistAsync_WithValidPlayerName_ReturnsRepositoryResult()
    {
        // Arrange
        var playerName = "TestPlayer";
        _mockRepository.Setup(r => r.PlayerStatisticsExistAsync(playerName))
                      .ReturnsAsync(true);

        // Act
        var result = await _statisticsService.PlayerStatisticsExistAsync(playerName);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.PlayerStatisticsExistAsync(playerName), Times.Once);
    }

    [Fact]
    public async Task GetPlayerCountAsync_ReturnsRepositoryResult()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetPlayerCountAsync())
                      .ReturnsAsync(5);

        // Act
        var result = await _statisticsService.GetPlayerCountAsync();

        // Assert
        Assert.Equal(5, result);
        _mockRepository.Verify(r => r.GetPlayerCountAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_WithDefaultPath_ReturnsRepositoryResult()
    {
        // Arrange
        var expectedPath = "/path/to/backup.json";
        _mockRepository.Setup(r => r.CreateBackupAsync(null))
                      .ReturnsAsync(expectedPath);

        // Act
        var result = await _statisticsService.CreateBackupAsync();

        // Assert
        Assert.Equal(expectedPath, result);
        _mockRepository.Verify(r => r.CreateBackupAsync(null), Times.Once);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithValidPath_ReturnsRepositoryResult()
    {
        // Arrange
        var backupPath = "/path/to/backup.json";
        _mockRepository.Setup(r => r.RestoreFromBackupAsync(backupPath, false))
                      .ReturnsAsync(3);

        // Act
        var result = await _statisticsService.RestoreFromBackupAsync(backupPath);

        // Assert
        Assert.Equal(3, result);
        _mockRepository.Verify(r => r.RestoreFromBackupAsync(backupPath, false), Times.Once);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _statisticsService.RestoreFromBackupAsync(null!));
    }
}