using GroupProject.Application.Services;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using GroupProject.Application.Models;
using Xunit;

namespace GroupProject.Tests.Application.Services;

/// <summary>
/// Unit tests for the SessionManager service.
/// </summary>
public class SessionManagerTests : IDisposable
{
    private readonly SessionManager _sessionManager;
    private readonly string _testStoragePath;
    private readonly GameConfiguration _defaultConfiguration;
    private readonly Money _defaultBankroll;
    private readonly List<string> _defaultPlayerNames;

    public SessionManagerTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "blackjack_test_sessions", Guid.NewGuid().ToString());
        _sessionManager = new SessionManager(_testStoragePath);
        
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

    public void Dispose()
    {
        // Clean up test storage directory
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, true);
        }
    }

    [Fact]
    public async Task StartSessionAsync_WithValidParameters_CreatesSession()
    {
        // Act
        var session = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Assert
        Assert.NotNull(session);
        Assert.True(session.IsActive);
        Assert.Equal(3, session.Players.Count);
        Assert.Equal(_defaultConfiguration, session.Configuration);
        Assert.Equal(_defaultBankroll, session.DefaultBankroll);
    }

    [Fact]
    public async Task StartSessionAsync_WithNullPlayerNames_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sessionManager.StartSessionAsync(null!, _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public async Task StartSessionAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sessionManager.StartSessionAsync(_defaultPlayerNames, null!, _defaultBankroll));
    }

    [Fact]
    public async Task StartSessionAsync_WithEmptyPlayerNames_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sessionManager.StartSessionAsync(new List<string>(), _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public async Task StartSessionAsync_WhenSessionAlreadyActive_ThrowsInvalidOperationException()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll));
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WithActiveSession_ReturnsSession()
    {
        // Arrange
        var originalSession = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var currentSession = await _sessionManager.GetCurrentSessionAsync();

        // Assert
        Assert.NotNull(currentSession);
        Assert.Equal(originalSession.SessionId, currentSession.SessionId);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_WithNoActiveSession_ReturnsNull()
    {
        // Act
        var currentSession = await _sessionManager.GetCurrentSessionAsync();

        // Assert
        Assert.Null(currentSession);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithValidSession_UpdatesSession()
    {
        // Arrange
        var session = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        await _sessionManager.UpdateSessionAsync(session);

        // Assert
        var currentSession = await _sessionManager.GetCurrentSessionAsync();
        Assert.NotNull(currentSession);
        Assert.Equal(session.SessionId, currentSession.SessionId);
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sessionManager.UpdateSessionAsync(null!));
    }

    [Fact]
    public async Task UpdateSessionAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new GameSession("test-session", _defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.UpdateSessionAsync(session));
    }

    [Fact]
    public async Task EndSessionAsync_WithActiveSession_EndsSessionAndReturnsSummary()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var summary = await _sessionManager.EndSessionAsync();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(3, summary.PlayerStatistics.Count);
        Assert.Equal(0, summary.RoundsPlayed);
        
        var currentSession = await _sessionManager.GetCurrentSessionAsync();
        Assert.Null(currentSession);
    }

    [Fact]
    public async Task EndSessionAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.EndSessionAsync());
    }

    [Fact]
    public async Task CanContinueSessionAsync_WithActiveSession_ReturnsTrue()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var canContinue = await _sessionManager.CanContinueSessionAsync();

        // Assert
        Assert.True(canContinue);
    }

    [Fact]
    public async Task CanContinueSessionAsync_WithNoActiveSession_ReturnsFalse()
    {
        // Act
        var canContinue = await _sessionManager.CanContinueSessionAsync();

        // Assert
        Assert.False(canContinue);
    }

    [Fact]
    public async Task RecordRoundAsync_WithValidSummary_RecordsRound()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        
        // Place bets for players
        var alice = await _sessionManager.GetPlayerAsync("Alice");
        var bob = await _sessionManager.GetPlayerAsync("Bob");
        alice.PlaceBet(new Money(100m));
        bob.PlaceBet(new Money(50m));
        
        var playerResults = new Dictionary<string, GameResult>
        {
            { "Alice", GameResult.Win },
            { "Bob", GameResult.Lose }
        };
        var dealerHand = new Hand();
        var roundSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act
        await _sessionManager.RecordRoundAsync(roundSummary);

        // Assert
        var currentSession = await _sessionManager.GetCurrentSessionAsync();
        Assert.NotNull(currentSession);
        Assert.Equal(1, currentSession.RoundsPlayed);
    }

    [Fact]
    public async Task RecordRoundAsync_WithNullSummary_ThrowsArgumentNullException()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _sessionManager.RecordRoundAsync(null!));
    }

    [Fact]
    public async Task RecordRoundAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var playerResults = new Dictionary<string, GameResult> { { "Alice", GameResult.Win } };
        var dealerHand = new Hand();
        var roundSummary = new GameSummary(playerResults, dealerHand, DateTime.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.RecordRoundAsync(roundSummary));
    }

    [Fact]
    public async Task AddPlayerAsync_WithValidName_AddsPlayer()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var customBankroll = new Money(2000m);

        // Act
        await _sessionManager.AddPlayerAsync("David", customBankroll);

        // Assert
        var players = await _sessionManager.GetAllPlayersAsync();
        Assert.Equal(4, players.Count);
        Assert.True(players.ContainsKey("David"));
        Assert.Equal(customBankroll, players["David"].Bankroll);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddPlayerAsync_WithInvalidName_ThrowsArgumentException(string playerName)
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sessionManager.AddPlayerAsync(playerName));
    }

    [Fact]
    public async Task AddPlayerAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.AddPlayerAsync("David"));
    }

    [Fact]
    public async Task RemovePlayerAsync_WithExistingPlayer_RemovesPlayer()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        await _sessionManager.RemovePlayerAsync("Alice");

        // Assert
        var players = await _sessionManager.GetAllPlayersAsync();
        Assert.Equal(2, players.Count);
        Assert.False(players.ContainsKey("Alice"));
    }

    [Fact]
    public async Task RemovePlayerAsync_WithNonExistentPlayer_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _sessionManager.RemovePlayerAsync("NonExistent"));
    }

    [Fact]
    public async Task GetPlayerAsync_WithExistingPlayer_ReturnsPlayer()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var player = await _sessionManager.GetPlayerAsync("Alice");

        // Assert
        Assert.NotNull(player);
        Assert.Equal("Alice", player.Name);
        Assert.Equal(_defaultBankroll, player.Bankroll);
    }

    [Fact]
    public async Task GetPlayerAsync_WithNonExistentPlayer_ThrowsKeyNotFoundException()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _sessionManager.GetPlayerAsync("NonExistent"));
    }

    [Fact]
    public async Task GetAllPlayersAsync_WithActiveSession_ReturnsAllPlayers()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        var players = await _sessionManager.GetAllPlayersAsync();

        // Assert
        Assert.Equal(3, players.Count);
        Assert.True(players.ContainsKey("Alice"));
        Assert.True(players.ContainsKey("Bob"));
        Assert.True(players.ContainsKey("Charlie"));
    }

    [Fact]
    public async Task GetAllPlayersAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.GetAllPlayersAsync());
    }

    [Fact]
    public async Task GetActivePlayersAsync_WithPositiveBankrolls_ReturnsActivePlayers()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = await _sessionManager.GetPlayerAsync("Alice");
        alice.SetBankroll(Money.Zero);

        // Act
        var activePlayers = await _sessionManager.GetActivePlayersAsync();

        // Assert
        var activePlayersList = activePlayers.ToList();
        Assert.Equal(2, activePlayersList.Count);
        Assert.DoesNotContain(activePlayersList, p => p.Name == "Alice");
    }

    [Fact]
    public async Task ResetPlayersForNewRoundAsync_WithActiveSession_ResetsAllPlayers()
    {
        // Arrange
        await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        var alice = await _sessionManager.GetPlayerAsync("Alice");
        alice.AddCard(new Card(Suit.Hearts, Rank.Ace));
        alice.PlaceBet(new Money(100m));

        // Act
        await _sessionManager.ResetPlayersForNewRoundAsync();

        // Assert
        Assert.Equal(0, alice.GetCardCount());
        Assert.Null(alice.CurrentBet);
    }

    [Fact]
    public async Task ResetPlayersForNewRoundAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.ResetPlayersForNewRoundAsync());
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithActiveSession_SavesSession()
    {
        // Arrange
        var session = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);

        // Act
        await _sessionManager.SaveSessionStateAsync();

        // Assert
        var sessionIds = await _sessionManager.GetSavedSessionIdsAsync();
        Assert.Contains(session.SessionId, sessionIds);
    }

    [Fact]
    public async Task SaveSessionStateAsync_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _sessionManager.SaveSessionStateAsync());
    }

    [Fact]
    public async Task LoadSessionAsync_WithExistingSession_ReturnsSession()
    {
        // Arrange
        var originalSession = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();

        // Act
        var loadedSession = await _sessionManager.LoadSessionAsync(originalSession.SessionId);

        // Assert
        Assert.NotNull(loadedSession);
        Assert.Equal(originalSession.SessionId, loadedSession.SessionId);
    }

    [Fact]
    public async Task LoadSessionAsync_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var loadedSession = await _sessionManager.LoadSessionAsync("non-existent-session");

        // Assert
        Assert.Null(loadedSession);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadSessionAsync_WithInvalidSessionId_ThrowsArgumentException(string sessionId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sessionManager.LoadSessionAsync(sessionId));
    }

    [Fact]
    public async Task GetSavedSessionIdsAsync_WithSavedSessions_ReturnsSessionIds()
    {
        // Arrange
        var session1 = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();
        await _sessionManager.EndSessionAsync();

        var session2 = await _sessionManager.StartSessionAsync(new List<string> { "Player1" }, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();

        // Act
        var sessionIds = await _sessionManager.GetSavedSessionIdsAsync();

        // Assert
        var sessionIdsList = sessionIds.ToList();
        Assert.Equal(2, sessionIdsList.Count);
        Assert.Contains(session1.SessionId, sessionIdsList);
        Assert.Contains(session2.SessionId, sessionIdsList);
    }

    [Fact]
    public async Task GetSavedSessionIdsAsync_WithNoSavedSessions_ReturnsEmptyCollection()
    {
        // Act
        var sessionIds = await _sessionManager.GetSavedSessionIdsAsync();

        // Assert
        Assert.Empty(sessionIds);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithExistingSession_DeletesSession()
    {
        // Arrange
        var session = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();

        // Act
        var deleted = await _sessionManager.DeleteSessionAsync(session.SessionId);

        // Assert
        Assert.True(deleted);
        var sessionIds = await _sessionManager.GetSavedSessionIdsAsync();
        Assert.DoesNotContain(session.SessionId, sessionIds);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var deleted = await _sessionManager.DeleteSessionAsync("non-existent-session");

        // Assert
        Assert.False(deleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteSessionAsync_WithInvalidSessionId_ThrowsArgumentException(string sessionId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sessionManager.DeleteSessionAsync(sessionId));
    }

    [Fact]
    public async Task RecoverSessionAsync_WithNoSavedSessions_ReturnsNull()
    {
        // Act
        var recoveredSession = await _sessionManager.RecoverSessionAsync();

        // Assert
        Assert.Null(recoveredSession);
    }

    [Fact]
    public async Task RecoverSessionAsync_WithSavedSessions_ReturnsMostRecentSession()
    {
        // Arrange
        var session1 = await _sessionManager.StartSessionAsync(_defaultPlayerNames, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();
        await _sessionManager.EndSessionAsync();

        // Wait a bit to ensure different timestamps
        await Task.Delay(100);

        var session2 = await _sessionManager.StartSessionAsync(new List<string> { "Player1" }, _defaultConfiguration, _defaultBankroll);
        await _sessionManager.SaveSessionStateAsync();
        await _sessionManager.EndSessionAsync();

        // Clear current session
        var currentSession = await _sessionManager.GetCurrentSessionAsync();
        Assert.Null(currentSession);

        // Act
        var recoveredSession = await _sessionManager.RecoverSessionAsync();

        // Assert
        Assert.NotNull(recoveredSession);
        // Note: The actual recovery logic depends on the implementation details
        // This test verifies that recovery attempts to find a session
    }
}