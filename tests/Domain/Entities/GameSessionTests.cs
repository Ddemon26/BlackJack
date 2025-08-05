using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using GroupProject.Application.Models;
using Xunit;

namespace GroupProject.Tests.Domain.Entities;

/// <summary>
/// Unit tests for the GameSession entity.
/// </summary>
public class GameSessionTests
{
    private readonly GameConfiguration _defaultConfiguration;
    private readonly Money _defaultBankroll;
    private readonly List<string> _defaultPlayerNames;

    public GameSessionTests()
    {
        _defaultConfiguration = new GameConfiguration
        {
            NumberOfDecks = 6,
            MaxPlayers = 4,
            MinPlayers = 1,
            AllowDoubleDown = true,
            AllowSplit = true,
            BlackjackPayout = 1.5
        };
        _defaultBankroll = new Money(1000m);
        _defaultPlayerNames = new List<string> { "Alice", "Bob", "Charlie" };
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesSession()
    {
        // Arrange
        var sessionId = "test-session-123";

        // Act
        var session = new GameSession(sessionId, _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Assert
        Assert.Equal(sessionId, session.SessionId);
        Assert.True(session.IsActive);
        Assert.Equal(0, session.RoundsPlayed);
        Assert.Equal(_defaultConfiguration, session.Configuration);
        Assert.Equal(_defaultBankroll, session.DefaultBankroll);
        Assert.Equal(3, session.Players.Count);
        Assert.True(session.Players.ContainsKey("Alice"));
        Assert.True(session.Players.ContainsKey("Bob"));
        Assert.True(session.Players.ContainsKey("Charlie"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidSessionId_ThrowsArgumentException(string sessionId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GameSession(sessionId, _defaultPlayerNames, _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public void Constructor_WithNullPlayerNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GameSession("test-session", (IEnumerable<string>)null!, _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public void Constructor_WithEmptyPlayerNames_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new GameSession("test-session", new List<string>(), _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new GameSession("test-session", _defaultPlayerNames, null!, _defaultBankroll));
    }

    [Fact]
    public void Constructor_WithNegativeBankroll_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var negativeBankroll = new Money(-100m);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, negativeBankroll));
    }

    [Fact]
    public void Constructor_WithDuplicatePlayerNames_CreatesSinglePlayer()
    {
        // Arrange
        var duplicateNames = new List<string> { "Alice", "Bob", "Alice", "Charlie" };

        // Act
        var session = new GameSession("test-session", duplicateNames, _defaultConfiguration, _defaultBankroll);

        // Assert
        Assert.Equal(3, session.Players.Count);
        Assert.True(session.Players.ContainsKey("Alice"));
        Assert.True(session.Players.ContainsKey("Bob"));
        Assert.True(session.Players.ContainsKey("Charlie"));
    }

    [Fact]
    public void GetPlayer_WithValidName_ReturnsPlayer()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var player = session.GetPlayer("Alice");

        // Assert
        Assert.NotNull(player);
        Assert.Equal("Alice", player.Name);
        Assert.Equal(_defaultBankroll, player.Bankroll);
    }

    [Fact]
    public void GetPlayer_WithInvalidName_ThrowsKeyNotFoundException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => session.GetPlayer("NonExistent"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPlayer_WithInvalidPlayerName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => session.GetPlayer(playerName));
    }

    [Fact]
    public void HasPlayer_WithExistingPlayer_ReturnsTrue()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.True(session.HasPlayer("Alice"));
        Assert.True(session.HasPlayer("alice")); // Case insensitive
    }

    [Fact]
    public void HasPlayer_WithNonExistentPlayer_ReturnsFalse()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.False(session.HasPlayer("NonExistent"));
        Assert.False(session.HasPlayer(null));
        Assert.False(session.HasPlayer(""));
    }

    [Fact]
    public void AddPlayer_WithValidName_AddsPlayer()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var customBankroll = new Money(2000m);

        // Act
        session.AddPlayer("David", customBankroll);

        // Assert
        Assert.Equal(4, session.Players.Count);
        Assert.True(session.HasPlayer("David"));
        var player = session.GetPlayer("David");
        Assert.Equal(customBankroll, player.Bankroll);
    }

    [Fact]
    public void AddPlayer_WithoutBankroll_UsesDefaultBankroll()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        session.AddPlayer("David");

        // Assert
        var player = session.GetPlayer("David");
        Assert.Equal(_defaultBankroll, player.Bankroll);
    }

    [Fact]
    public void AddPlayer_WithExistingName_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.AddPlayer("Alice"));
    }

    [Fact]
    public void AddPlayer_WhenSessionInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.AddPlayer("David"));
    }

    [Fact]
    public void RemovePlayer_WithExistingPlayer_RemovesPlayer()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        session.RemovePlayer("Alice");

        // Assert
        Assert.Equal(2, session.Players.Count);
        Assert.False(session.HasPlayer("Alice"));
    }

    [Fact]
    public void RemovePlayer_WithNonExistentPlayer_ThrowsKeyNotFoundException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => session.RemovePlayer("NonExistent"));
    }

    [Fact]
    public void RemovePlayer_WhenSessionInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.RemovePlayer("Alice"));
    }

    [Fact]
    public void RecordRound_WithValidSummary_UpdatesStatistics()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        
        // Place bets for players so they have valid bet amounts
        var alice = session.GetPlayer("Alice");
        var bob = session.GetPlayer("Bob");
        var charlie = session.GetPlayer("Charlie");
        alice.PlaceBet(new Money(100m));
        bob.PlaceBet(new Money(50m));
        charlie.PlaceBet(new Money(75m));
        
        var playerResults = new Dictionary<string, GameResult>
        {
            { "Alice", GameResult.Win },
            { "Bob", GameResult.Lose },
            { "Charlie", GameResult.Blackjack }
        };
        var dealerHand = new Hand();
        var roundSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act
        session.RecordRound(roundSummary);

        // Assert
        Assert.Equal(1, session.RoundsPlayed);
        Assert.Single(session.RoundSummaries);
    }

    [Fact]
    public void RecordRound_WithNullSummary_ThrowsArgumentNullException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => session.RecordRound(null!));
    }

    [Fact]
    public void RecordRound_WhenSessionInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();
        var playerResults = new Dictionary<string, GameResult> { { "Alice", GameResult.Win } };
        var dealerHand = new Hand();
        var roundSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.RecordRound(roundSummary));
    }

    [Fact]
    public void EndSession_WhenActive_EndsSession()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        session.EndSession();

        // Assert
        Assert.False(session.IsActive);
        Assert.NotNull(session.EndTime);
        Assert.True(session.EndTime <= DateTime.UtcNow);
    }

    [Fact]
    public void EndSession_WhenAlreadyInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.EndSession());
    }

    [Fact]
    public void ResetPlayersForNewRound_WhenActive_ResetsAllPlayers()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = session.GetPlayer("Alice");
        alice.AddCard(new Card(Suit.Hearts, Rank.Ace));
        alice.PlaceBet(new Money(100m));

        // Act
        session.ResetPlayersForNewRound();

        // Assert
        Assert.Equal(0, alice.GetCardCount());
        Assert.Null(alice.CurrentBet);
    }

    [Fact]
    public void ResetPlayersForNewRound_WhenInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.ResetPlayersForNewRound());
    }

    [Fact]
    public void GetActivePlayers_WithPositiveBankrolls_ReturnsActivePlayers()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = session.GetPlayer("Alice");
        alice.SetBankroll(Money.Zero); // Make Alice inactive

        // Act
        var activePlayers = session.GetActivePlayers().ToList();

        // Assert
        Assert.Equal(2, activePlayers.Count);
        Assert.DoesNotContain(activePlayers, p => p.Name == "Alice");
        Assert.Contains(activePlayers, p => p.Name == "Bob");
        Assert.Contains(activePlayers, p => p.Name == "Charlie");
    }

    [Fact]
    public void GetInactivePlayers_WithZeroBankrolls_ReturnsInactivePlayers()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = session.GetPlayer("Alice");
        alice.SetBankroll(Money.Zero);

        // Act
        var inactivePlayers = session.GetInactivePlayers().ToList();

        // Assert
        Assert.Single(inactivePlayers);
        Assert.Equal("Alice", inactivePlayers.First().Name);
    }

    [Fact]
    public void CanContinue_WithActivePlayersAndActiveSession_ReturnsTrue()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.True(session.CanContinue());
    }

    [Fact]
    public void CanContinue_WithInactiveSession_ReturnsFalse()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        session.EndSession();

        // Act & Assert
        Assert.False(session.CanContinue());
    }

    [Fact]
    public void CanContinue_WithNoActivePlayers_ReturnsFalse()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        foreach (var player in session.Players.Values)
        {
            player.SetBankroll(Money.Zero);
        }

        // Act & Assert
        Assert.False(session.CanContinue());
    }

    [Fact]
    public void GetBiggestWinner_WithPlayers_ReturnsPlayerWithHighestBankroll()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = session.GetPlayer("Alice");
        var bob = session.GetPlayer("Bob");
        alice.SetBankroll(new Money(2000m));
        bob.SetBankroll(new Money(500m));

        // Act
        var biggestWinner = session.GetBiggestWinner();

        // Assert
        Assert.NotNull(biggestWinner);
        Assert.Equal("Alice", biggestWinner.Name);
    }

    [Fact]
    public void Duration_ReturnsCorrectTimeSpan()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var startTime = session.StartTime;
        
        // Wait a small amount to ensure duration is positive
        Thread.Sleep(10);

        // Act
        var duration = session.Duration;

        // Assert
        Assert.True(duration.TotalMilliseconds > 0);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var sessionId = "test-session-123";
        var session = new GameSession(sessionId, _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var result = session.ToString();

        // Assert
        Assert.Contains(sessionId, result);
        Assert.Contains("Active", result);
        Assert.Contains("3 players", result);
        Assert.Contains("0 rounds", result);
    }

    [Fact]
    public void Equals_WithSameSessionId_ReturnsTrue()
    {
        // Arrange
        var sessionId = "test-session-123";
        var session1 = new GameSession(sessionId, _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var session2 = new GameSession(sessionId, new List<string> { "Different" }, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.True(session1.Equals(session2));
        Assert.Equal(session1.GetHashCode(), session2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentSessionId_ReturnsFalse()
    {
        // Arrange
        var session1 = new GameSession("session-1", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var session2 = new GameSession("session-2", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        Assert.False(session1.Equals(session2));
    }
}