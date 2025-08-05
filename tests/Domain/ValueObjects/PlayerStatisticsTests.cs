using GroupProject.Domain.ValueObjects;
using Xunit;

namespace GroupProject.Tests.Domain.ValueObjects;

public class PlayerStatisticsTests
{
    [Fact]
    public void Constructor_WithValidPlayerName_InitializesCorrectly()
    {
        // Arrange
        var playerName = "John";

        // Act
        var statistics = new PlayerStatistics(playerName);

        // Assert
        Assert.Equal(playerName, statistics.PlayerName);
        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Equal(0, statistics.GamesWon);
        Assert.Equal(0, statistics.GamesLost);
        Assert.Equal(0, statistics.GamesPushed);
        Assert.Equal(0, statistics.BlackjacksAchieved);
        Assert.Equal(Money.Zero, statistics.TotalWagered);
        Assert.Equal(Money.Zero, statistics.NetWinnings);
        Assert.Equal(0.0, statistics.WinPercentage);
        Assert.Equal(0.0, statistics.LossPercentage);
        Assert.Equal(0.0, statistics.PushPercentage);
        Assert.Equal(0.0, statistics.BlackjackPercentage);
        Assert.Equal(Money.Zero, statistics.AverageBet);
        Assert.Equal(0.0, statistics.ReturnOnInvestment);
        Assert.False(statistics.IsProfitable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Constructor_WithInvalidPlayerName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PlayerStatistics(invalidName!));
    }

    [Fact]
    public void Constructor_WithPlayerNameWithWhitespace_TrimsName()
    {
        // Arrange & Act
        var statistics = new PlayerStatistics("  John  ");

        // Assert
        Assert.Equal("John", statistics.PlayerName);
    }

    [Fact]
    public void Constructor_WithAllParameters_InitializesCorrectly()
    {
        // Arrange
        var playerName = "John";
        var gamesPlayed = 10;
        var gamesWon = 6;
        var gamesLost = 3;
        var gamesPushed = 1;
        var blackjacksAchieved = 2;
        var totalWagered = Money.FromUsd(500m);
        var netWinnings = Money.FromUsd(150m);
        var firstPlayed = DateTime.UtcNow.AddDays(-30);
        var lastPlayed = DateTime.UtcNow;

        // Act
        var statistics = new PlayerStatistics(playerName, gamesPlayed, gamesWon, gamesLost,
            gamesPushed, blackjacksAchieved, totalWagered, netWinnings, firstPlayed, lastPlayed);

        // Assert
        Assert.Equal(playerName, statistics.PlayerName);
        Assert.Equal(gamesPlayed, statistics.GamesPlayed);
        Assert.Equal(gamesWon, statistics.GamesWon);
        Assert.Equal(gamesLost, statistics.GamesLost);
        Assert.Equal(gamesPushed, statistics.GamesPushed);
        Assert.Equal(blackjacksAchieved, statistics.BlackjacksAchieved);
        Assert.Equal(totalWagered, statistics.TotalWagered);
        Assert.Equal(netWinnings, statistics.NetWinnings);
        Assert.Equal(firstPlayed, statistics.FirstPlayed);
        Assert.Equal(lastPlayed, statistics.LastPlayed);
    }

    [Fact]
    public void Constructor_WithNegativeGamesPlayed_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", -1, 0, 0, 0, 0, Money.Zero, Money.Zero, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNegativeGamesWon_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", 0, -1, 0, 0, 0, Money.Zero, Money.Zero, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithInconsistentGameCounts_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", 10, 5, 3, 1, 0, Money.Zero, Money.Zero, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithBlackjacksExceedingWins_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", 5, 2, 2, 1, 3, Money.Zero, Money.Zero, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithNegativeTotalWagered_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", 0, 0, 0, 0, 0, Money.FromUsd(-100m), Money.Zero, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithLastPlayedBeforeFirstPlayed_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var firstPlayed = DateTime.UtcNow;
        var lastPlayed = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new PlayerStatistics("John", 0, 0, 0, 0, 0, Money.Zero, Money.Zero, firstPlayed, lastPlayed));
    }

    [Fact]
    public void RecordGame_WithWin_UpdatesStatisticsCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        var betAmount = Money.FromUsd(25m);
        var payout = Money.FromUsd(25m);

        // Act
        statistics.RecordGame(GameResult.Win, betAmount, payout);

        // Assert
        Assert.Equal(1, statistics.GamesPlayed);
        Assert.Equal(1, statistics.GamesWon);
        Assert.Equal(0, statistics.GamesLost);
        Assert.Equal(0, statistics.GamesPushed);
        Assert.Equal(0, statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, statistics.TotalWagered);
        Assert.Equal(payout, statistics.NetWinnings);
        Assert.Equal(1.0, statistics.WinPercentage);
        Assert.True(statistics.IsProfitable);
    }

    [Fact]
    public void RecordGame_WithBlackjack_UpdatesStatisticsCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        var betAmount = Money.FromUsd(20m);
        var payout = Money.FromUsd(30m); // 3:2 payout

        // Act
        statistics.RecordGame(GameResult.Blackjack, betAmount, payout);

        // Assert
        Assert.Equal(1, statistics.GamesPlayed);
        Assert.Equal(1, statistics.GamesWon);
        Assert.Equal(0, statistics.GamesLost);
        Assert.Equal(0, statistics.GamesPushed);
        Assert.Equal(1, statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, statistics.TotalWagered);
        Assert.Equal(payout, statistics.NetWinnings);
        Assert.Equal(1.0, statistics.WinPercentage);
        Assert.Equal(1.0, statistics.BlackjackPercentage);
        Assert.True(statistics.IsProfitable);
    }

    [Fact]
    public void RecordGame_WithLoss_UpdatesStatisticsCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        var betAmount = Money.FromUsd(25m);
        var payout = Money.Zero;

        // Act
        statistics.RecordGame(GameResult.Lose, betAmount, payout);

        // Assert
        Assert.Equal(1, statistics.GamesPlayed);
        Assert.Equal(0, statistics.GamesWon);
        Assert.Equal(1, statistics.GamesLost);
        Assert.Equal(0, statistics.GamesPushed);
        Assert.Equal(0, statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, statistics.TotalWagered);
        Assert.Equal(Money.FromUsd(-25m), statistics.NetWinnings);
        Assert.Equal(0.0, statistics.WinPercentage);
        Assert.Equal(1.0, statistics.LossPercentage);
        Assert.False(statistics.IsProfitable);
    }

    [Fact]
    public void RecordGame_WithPush_UpdatesStatisticsCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        var betAmount = Money.FromUsd(25m);
        var payout = Money.Zero;

        // Act
        statistics.RecordGame(GameResult.Push, betAmount, payout);

        // Assert
        Assert.Equal(1, statistics.GamesPlayed);
        Assert.Equal(0, statistics.GamesWon);
        Assert.Equal(0, statistics.GamesLost);
        Assert.Equal(1, statistics.GamesPushed);
        Assert.Equal(0, statistics.BlackjacksAchieved);
        Assert.Equal(betAmount, statistics.TotalWagered);
        Assert.Equal(Money.Zero, statistics.NetWinnings);
        Assert.Equal(0.0, statistics.WinPercentage);
        Assert.Equal(1.0, statistics.PushPercentage);
        Assert.False(statistics.IsProfitable);
    }

    [Fact]
    public void RecordGame_WithNonPositiveBetAmount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            statistics.RecordGame(GameResult.Win, Money.Zero, Money.FromUsd(10m)));
    }

    [Fact]
    public void RecordGame_WithNegativePayout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(-10m)));
    }

    [Fact]
    public void RecordGame_MultipleGames_CalculatesPercentagesCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act - Record 10 games: 6 wins (1 blackjack), 3 losses, 1 push
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Blackjack, Money.FromUsd(20m), Money.FromUsd(30m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);
        statistics.RecordGame(GameResult.Push, Money.FromUsd(25m), Money.Zero);

        // Assert
        Assert.Equal(10, statistics.GamesPlayed);
        Assert.Equal(6, statistics.GamesWon);
        Assert.Equal(3, statistics.GamesLost);
        Assert.Equal(1, statistics.GamesPushed);
        Assert.Equal(1, statistics.BlackjacksAchieved);
        Assert.Equal(0.6, statistics.WinPercentage, 2);
        Assert.Equal(0.3, statistics.LossPercentage, 2);
        Assert.Equal(0.1, statistics.PushPercentage, 2);
        Assert.Equal(0.1, statistics.BlackjackPercentage, 2);
    }

    [Fact]
    public void AverageBet_WithMultipleGames_CalculatesCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act
        statistics.RecordGame(GameResult.Win, Money.FromUsd(20m), Money.FromUsd(20m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(30m), Money.FromUsd(30m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);

        // Assert
        Assert.Equal(Money.FromUsd(25m), statistics.AverageBet);
    }

    [Fact]
    public void ReturnOnInvestment_WithProfitablePlay_CalculatesCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act - Bet $100 total, win $50 net
        statistics.RecordGame(GameResult.Win, Money.FromUsd(50m), Money.FromUsd(50m));
        statistics.RecordGame(GameResult.Win, Money.FromUsd(50m), Money.FromUsd(50m));

        // Assert
        Assert.Equal(1.0, statistics.ReturnOnInvestment, 2); // 100% ROI
    }

    [Fact]
    public void ReturnOnInvestment_WithLosingPlay_CalculatesCorrectly()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act - Bet $100 total, lose $50 net
        statistics.RecordGame(GameResult.Win, Money.FromUsd(50m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(50m), Money.Zero);

        // Assert
        Assert.Equal(-0.25, statistics.ReturnOnInvestment, 2); // -25% ROI
    }

    [Fact]
    public void TotalPlayTime_CalculatesCorrectly()
    {
        // Arrange
        var firstPlayed = DateTime.UtcNow.AddHours(-5);
        var lastPlayed = DateTime.UtcNow;
        var statistics = new PlayerStatistics("John", 5, 3, 2, 0, 1, 
            Money.FromUsd(125m), Money.FromUsd(25m), firstPlayed, lastPlayed);

        // Act
        var playTime = statistics.TotalPlayTime;

        // Assert
        Assert.Equal(5.0, playTime.TotalHours, 1);
    }

    [Fact]
    public void Reset_ClearsAllStatistics()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);

        // Act
        statistics.Reset();

        // Assert
        Assert.Equal("John", statistics.PlayerName);
        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Equal(0, statistics.GamesWon);
        Assert.Equal(0, statistics.GamesLost);
        Assert.Equal(0, statistics.GamesPushed);
        Assert.Equal(0, statistics.BlackjacksAchieved);
        Assert.Equal(Money.Zero, statistics.TotalWagered);
        Assert.Equal(Money.Zero, statistics.NetWinnings);
        Assert.Equal(0.0, statistics.WinPercentage);
        Assert.False(statistics.IsProfitable);
    }

    [Fact]
    public void WithUpdatedValues_CreatesNewInstanceWithUpdatedValues()
    {
        // Arrange
        var original = new PlayerStatistics("John");
        var newLastPlayed = DateTime.UtcNow.AddHours(1);

        // Act
        var updated = original.WithUpdatedValues(gamesPlayed: 5, gamesWon: 3, 
            gamesLost: 2, lastPlayed: newLastPlayed);

        // Assert
        Assert.Equal(5, updated.GamesPlayed);
        Assert.Equal(3, updated.GamesWon);
        Assert.Equal(2, updated.GamesLost);
        Assert.Equal(newLastPlayed, updated.LastPlayed);
        Assert.Equal(original.PlayerName, updated.PlayerName);
        Assert.Equal(original.FirstPlayed, updated.FirstPlayed);
        
        // Original should be unchanged
        Assert.Equal(0, original.GamesPlayed);
    }

    [Fact]
    public void ToString_WithNoGames_ReturnsCorrectFormat()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act
        var result = statistics.ToString();

        // Assert
        Assert.Equal("John: No games played", result);
    }

    [Fact]
    public void ToString_WithGames_ReturnsCorrectFormat()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Blackjack, Money.FromUsd(20m), Money.FromUsd(30m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);

        // Act
        var result = statistics.ToString();

        // Assert
        Assert.Contains("John:", result);
        Assert.Contains("3 games", result);
        Assert.Contains("2W-1L-0P", result);
        Assert.Contains("1 BJ", result);
        Assert.Contains("Net:", result);
    }

    [Fact]
    public void ToDetailedString_WithNoGames_ReturnsCorrectFormat()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");

        // Act
        var result = statistics.ToDetailedString();

        // Assert
        Assert.Contains("John: No games played since", result);
    }

    [Fact]
    public void ToDetailedString_WithGames_ReturnsDetailedFormat()
    {
        // Arrange
        var statistics = new PlayerStatistics("John");
        statistics.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));
        statistics.RecordGame(GameResult.Lose, Money.FromUsd(25m), Money.Zero);

        // Act
        var result = statistics.ToDetailedString();

        // Assert
        Assert.Contains("John Statistics:", result);
        Assert.Contains("Games:", result);
        Assert.Contains("Win Rate:", result);
        Assert.Contains("Blackjacks:", result);
        Assert.Contains("Total Wagered:", result);
        Assert.Contains("Net Winnings:", result);
        Assert.Contains("Average Bet:", result);
        Assert.Contains("ROI:", result);
        Assert.Contains("Play Period:", result);
        Assert.Contains("Total Play Time:", result);
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var firstPlayed = DateTime.UtcNow.AddDays(-1);
        var lastPlayed = DateTime.UtcNow;
        var stats1 = new PlayerStatistics("John", 5, 3, 2, 0, 1, 
            Money.FromUsd(125m), Money.FromUsd(25m), firstPlayed, lastPlayed);
        var stats2 = new PlayerStatistics("John", 5, 3, 2, 0, 1, 
            Money.FromUsd(125m), Money.FromUsd(25m), firstPlayed, lastPlayed);

        // Act & Assert
        Assert.True(stats1.Equals(stats2));
        Assert.True(stats2.Equals(stats1));
    }

    [Fact]
    public void Equals_WithDifferentPlayerName_ReturnsFalse()
    {
        // Arrange
        var stats1 = new PlayerStatistics("John");
        var stats2 = new PlayerStatistics("Jane");

        // Act & Assert
        Assert.False(stats1.Equals(stats2));
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var stats1 = new PlayerStatistics("John");
        var stats2 = new PlayerStatistics("John");
        stats2.RecordGame(GameResult.Win, Money.FromUsd(25m), Money.FromUsd(25m));

        // Act & Assert
        Assert.False(stats1.Equals(stats2));
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var firstPlayed = DateTime.UtcNow.AddDays(-1);
        var lastPlayed = DateTime.UtcNow;
        var stats1 = new PlayerStatistics("John", 5, 3, 2, 0, 1, 
            Money.FromUsd(125m), Money.FromUsd(25m), firstPlayed, lastPlayed);
        var stats2 = new PlayerStatistics("john", 5, 3, 2, 0, 1, 
            Money.FromUsd(125m), Money.FromUsd(25m), firstPlayed, lastPlayed);

        // Act
        var hash1 = stats1.GetHashCode();
        var hash2 = stats2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ReturnsDifferentHashCodes()
    {
        // Arrange
        var stats1 = new PlayerStatistics("John");
        var stats2 = new PlayerStatistics("Jane");

        // Act
        var hash1 = stats1.GetHashCode();
        var hash2 = stats2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }
}