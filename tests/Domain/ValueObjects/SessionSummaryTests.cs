using GroupProject.Domain.ValueObjects;
using GroupProject.Domain.Entities;
using GroupProject.Application.Models;
using Xunit;

namespace GroupProject.Tests.Domain.ValueObjects;

/// <summary>
/// Unit tests for the SessionSummary value object.
/// </summary>
public class SessionSummaryTests
{
    private readonly string _sessionId;
    private readonly DateTime _startTime;
    private readonly DateTime _endTime;
    private readonly Dictionary<string, PlayerStatistics> _playerStatistics;
    private readonly Dictionary<string, Money> _finalBankrolls;

    public SessionSummaryTests()
    {
        _sessionId = "test-session-123";
        _startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        _endTime = new DateTime(2024, 1, 1, 12, 30, 0);

        _playerStatistics = new Dictionary<string, PlayerStatistics>
        {
            { "Alice", new PlayerStatistics("Alice", 20, 12, 6, 2, 3, new Money(2000m), new Money(500m), _startTime, _endTime) },
            { "Bob", new PlayerStatistics("Bob", 18, 8, 9, 1, 1, new Money(1800m), new Money(-200m), _startTime, _endTime) },
            { "Charlie", new PlayerStatistics("Charlie", 15, 7, 7, 1, 2, new Money(1500m), new Money(100m), _startTime, _endTime) }
        };

        _finalBankrolls = new Dictionary<string, Money>
        {
            { "Alice", new Money(1500m) },
            { "Bob", new Money(800m) },
            { "Charlie", new Money(1100m) }
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesSessionSummary()
    {
        // Act
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Assert
        Assert.Equal(_sessionId, summary.SessionId);
        Assert.Equal(_startTime, summary.StartTime);
        Assert.Equal(_endTime, summary.EndTime);
        Assert.Equal(25, summary.RoundsPlayed);
        Assert.Equal(TimeSpan.FromHours(2.5), summary.Duration);
        Assert.Equal(3, summary.PlayerStatistics.Count);
        Assert.Equal(3, summary.FinalBankrolls.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidSessionId_ThrowsArgumentException(string sessionId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SessionSummary(sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls));
    }

    [Fact]
    public void Constructor_WithEndTimeBeforeStartTime_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidEndTime = _startTime.AddHours(-1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new SessionSummary(_sessionId, _startTime, invalidEndTime, 25, _playerStatistics, _finalBankrolls));
    }

    [Fact]
    public void Constructor_WithNegativeRoundsPlayed_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new SessionSummary(_sessionId, _startTime, _endTime, -1, _playerStatistics, _finalBankrolls));
    }

    [Fact]
    public void Constructor_WithNullPlayerStatistics_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionSummary(_sessionId, _startTime, _endTime, 25, null!, _finalBankrolls));
    }

    [Fact]
    public void Constructor_WithNullFinalBankrolls_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, null!));
    }

    [Fact]
    public void BiggestWinner_ReturnsPlayerWithHighestBankroll()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var biggestWinner = summary.BiggestWinner;

        // Assert
        Assert.Equal("Alice", biggestWinner);
    }

    [Fact]
    public void BiggestWinner_WithEmptyBankrolls_ReturnsNull()
    {
        // Arrange
        var emptyBankrolls = new Dictionary<string, Money>();
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, emptyBankrolls);

        // Act
        var biggestWinner = summary.BiggestWinner;

        // Assert
        Assert.Null(biggestWinner);
    }

    [Fact]
    public void LargestBankroll_ReturnsHighestBankrollAmount()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var largestBankroll = summary.LargestBankroll;

        // Assert
        Assert.Equal(new Money(1500m), largestBankroll);
    }

    [Fact]
    public void TotalWagered_ReturnsCorrectSum()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var totalWagered = summary.TotalWagered;

        // Assert
        Assert.Equal(new Money(5300m), totalWagered); // 2000 + 1800 + 1500
    }

    [Fact]
    public void TotalNetWinnings_ReturnsCorrectSum()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var totalNetWinnings = summary.TotalNetWinnings;

        // Assert
        Assert.Equal(new Money(400m), totalNetWinnings); // 500 + (-200) + 100
    }

    [Fact]
    public void TotalGamesPlayed_ReturnsCorrectSum()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var totalGames = summary.TotalGamesPlayed;

        // Assert
        Assert.Equal(53, totalGames); // 20 + 18 + 15
    }

    [Fact]
    public void TotalBlackjacks_ReturnsCorrectSum()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var totalBlackjacks = summary.TotalBlackjacks;

        // Assert
        Assert.Equal(6, totalBlackjacks); // 3 + 1 + 2
    }

    [Fact]
    public void RoundsPerHour_CalculatesCorrectly()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var roundsPerHour = summary.RoundsPerHour;

        // Assert
        Assert.Equal(10.0, roundsPerHour); // 25 rounds / 2.5 hours
    }

    [Fact]
    public void ProfitablePlayers_ReturnsCorrectPlayers()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var profitablePlayers = summary.ProfitablePlayers.ToList();

        // Assert
        Assert.Equal(2, profitablePlayers.Count);
        Assert.Contains("Alice", profitablePlayers);
        Assert.Contains("Charlie", profitablePlayers);
        Assert.DoesNotContain("Bob", profitablePlayers);
    }

    [Fact]
    public void UnprofitablePlayers_ReturnsCorrectPlayers()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var unprofitablePlayers = summary.UnprofitablePlayers.ToList();

        // Assert
        Assert.Single(unprofitablePlayers);
        Assert.Contains("Bob", unprofitablePlayers);
    }

    [Fact]
    public void OverallWinPercentage_CalculatesCorrectly()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var overallWinPercentage = summary.OverallWinPercentage;

        // Assert
        var expectedWinPercentage = (double)(12 + 8 + 7) / 53; // Total wins / Total games
        Assert.Equal(expectedWinPercentage, overallWinPercentage, 0.001);
    }

    [Fact]
    public void AverageBetAmount_CalculatesCorrectly()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var averageBet = summary.AverageBetAmount;

        // Assert
        var expectedAverage = new Money(5300m) / 53; // Total wagered / Total games
        Assert.Equal(expectedAverage, averageBet);
    }

    [Fact]
    public void GetPlayerStatistics_WithExistingPlayer_ReturnsStatistics()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var aliceStats = summary.GetPlayerStatistics("Alice");

        // Assert
        Assert.NotNull(aliceStats);
        Assert.Equal("Alice", aliceStats.PlayerName);
        Assert.Equal(20, aliceStats.GamesPlayed);
    }

    [Fact]
    public void GetPlayerStatistics_WithNonExistentPlayer_ReturnsNull()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var stats = summary.GetPlayerStatistics("NonExistent");

        // Assert
        Assert.Null(stats);
    }

    [Fact]
    public void GetPlayerFinalBankroll_WithExistingPlayer_ReturnsBankroll()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var aliceBankroll = summary.GetPlayerFinalBankroll("Alice");

        // Assert
        Assert.Equal(new Money(1500m), aliceBankroll);
    }

    [Fact]
    public void GetPlayerFinalBankroll_WithNonExistentPlayer_ReturnsNull()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var bankroll = summary.GetPlayerFinalBankroll("NonExistent");

        // Assert
        Assert.Null(bankroll);
    }

    [Fact]
    public void FromSession_WithCompletedSession_CreatesSessionSummary()
    {
        // Arrange
        var playerNames = new List<string> { "Alice", "Bob" };
        var config = new GameConfiguration();
        var defaultBankroll = new Money(1000m);
        var session = new GameSession("test-session", playerNames, config, defaultBankroll);
        session.EndSession();

        // Act
        var summary = SessionSummary.FromSession(session);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(session.SessionId, summary.SessionId);
        Assert.Equal(session.StartTime, summary.StartTime);
        Assert.Equal(session.EndTime, summary.EndTime);
        Assert.Equal(session.RoundsPlayed, summary.RoundsPlayed);
    }

    [Fact]
    public void FromSession_WithActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var playerNames = new List<string> { "Alice", "Bob" };
        var config = new GameConfiguration();
        var defaultBankroll = new Money(1000m);
        var session = new GameSession("test-session", playerNames, config, defaultBankroll);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => SessionSummary.FromSession(session));
    }

    [Fact]
    public void FromSession_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SessionSummary.FromSession(null!));
    }

    [Fact]
    public void CalculateHouseEdge_ReturnsCorrectValue()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var houseEdge = summary.CalculateHouseEdge();

        // Assert
        var expectedHouseEdge = -(400.0 / 5300.0); // -(TotalNetWinnings / TotalWagered)
        Assert.Equal(expectedHouseEdge, houseEdge, 0.001);
    }

    [Fact]
    public void GetMostActivePlayer_ReturnsPlayerWithMostGames()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var mostActive = summary.GetMostActivePlayer();

        // Assert
        Assert.Equal("Alice", mostActive); // Alice played 20 games
    }

    [Fact]
    public void GetBestWinRatePlayer_ReturnsPlayerWithHighestWinRate()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var bestWinRate = summary.GetBestWinRatePlayer();

        // Assert
        Assert.Equal("Alice", bestWinRate); // Alice has 60% win rate (12/20)
    }

    [Fact]
    public void GetBlackjackChampion_ReturnsPlayerWithMostBlackjacks()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var champion = summary.GetBlackjackChampion();

        // Assert
        Assert.Equal("Alice", champion); // Alice has 3 blackjacks
    }

    [Fact]
    public void CalculateAverageMetrics_ReturnsCorrectAverages()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var (avgGames, avgNetWinnings, avgWinRate) = summary.CalculateAverageMetrics();

        // Assert
        Assert.Equal(53.0 / 3.0, avgGames, 0.001); // Total games / player count
        Assert.Equal(new Money(400m) / 3, avgNetWinnings); // Total net winnings / player count
        
        var expectedAvgWinRate = (0.6 + (8.0/18.0) + (7.0/15.0)) / 3.0; // Average of individual win rates
        Assert.Equal(expectedAvgWinRate, avgWinRate, 0.001);
    }

    [Fact]
    public void GetSessionHighlights_ReturnsRelevantHighlights()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var highlights = summary.GetSessionHighlights().ToList();

        // Assert
        Assert.NotEmpty(highlights);
        // Should include highlights about Alice being most active, having good win rate, etc.
        Assert.Contains(highlights, h => h.Contains("Alice"));
    }

    [Fact]
    public void ToConsoleString_ReturnsFormattedOutput()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var consoleOutput = summary.ToConsoleString();

        // Assert
        Assert.Contains("SESSION SUMMARY", consoleOutput);
        Assert.Contains(_sessionId, consoleOutput);
        Assert.Contains("Alice", consoleOutput);
        Assert.Contains("Bob", consoleOutput);
        Assert.Contains("Charlie", consoleOutput);
        Assert.Contains("╔", consoleOutput); // Box drawing characters
        Assert.Contains("║", consoleOutput);
        Assert.Contains("╚", consoleOutput);
    }

    [Fact]
    public void ToTableString_ReturnsTableFormattedOutput()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var tableOutput = summary.ToTableString();

        // Assert
        Assert.Contains("Player", tableOutput);
        Assert.Contains("Games", tableOutput);
        Assert.Contains("Won", tableOutput);
        Assert.Contains("Lost", tableOutput);
        Assert.Contains("Alice", tableOutput);
        Assert.Contains("Bob", tableOutput);
        Assert.Contains("Charlie", tableOutput);
        Assert.Contains("┌", tableOutput); // Table drawing characters
        Assert.Contains("│", tableOutput);
        Assert.Contains("└", tableOutput);
    }

    [Fact]
    public void ToCompactString_ReturnsCompactSummary()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var compactOutput = summary.ToCompactString();

        // Assert
        Assert.Contains(_sessionId, compactOutput);
        Assert.Contains("2.5h", compactOutput);
        Assert.Contains("25r", compactOutput);
        Assert.Contains("3p", compactOutput);
        Assert.Contains("Alice", compactOutput); // Winner
    }

    [Fact]
    public void ToString_ReturnsBasicSummary()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act
        var output = summary.ToString();

        // Assert
        Assert.Contains(_sessionId, output);
        Assert.Contains("3 players", output);
        Assert.Contains("25 rounds", output);
        Assert.Contains("Alice", output); // Winner
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var summary1 = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);
        var summary2 = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act & Assert
        Assert.True(summary1.Equals(summary2));
        Assert.Equal(summary1.GetHashCode(), summary2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var summary1 = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);
        var summary2 = new SessionSummary("different-session", _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act & Assert
        Assert.False(summary1.Equals(summary2));
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act & Assert
        Assert.False(summary.Equals(null));
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var summary = new SessionSummary(_sessionId, _startTime, _endTime, 25, _playerStatistics, _finalBankrolls);

        // Act & Assert
        Assert.False(summary.Equals("not a summary"));
    }
}