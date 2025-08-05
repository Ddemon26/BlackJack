using GroupProject.Domain.ValueObjects;
using GroupProject.Application.Models;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a multi-round blackjack gaming session with persistent player state and statistics tracking.
/// </summary>
/// <remarks>
/// A GameSession manages the lifecycle of multiple rounds of blackjack gameplay,
/// maintaining player bankrolls, statistics, and session state across rounds.
/// It provides comprehensive tracking of session duration, rounds played, and player performance.
/// </remarks>
public class GameSession
{
    private readonly Dictionary<string, Player> _players;
    private readonly List<GameSummary> _roundSummaries;
    private readonly Dictionary<string, PlayerStatistics> _sessionStatistics;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameSession"/> class.
    /// </summary>
    /// <param name="sessionId">The unique identifier for this session.</param>
    /// <param name="playerNames">The names of players participating in the session.</param>
    /// <param name="configuration">The game configuration for this session.</param>
    /// <param name="defaultBankroll">The default bankroll for each player.</param>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty, or when playerNames is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when defaultBankroll is negative.</exception>
    public GameSession(
        string sessionId,
        IEnumerable<string> playerNames,
        GameConfiguration configuration,
        Money defaultBankroll)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));

        var playerNamesList = playerNames?.ToList() ?? throw new ArgumentNullException(nameof(playerNames));
        if (!playerNamesList.Any())
            throw new ArgumentException("At least one player name must be provided.", nameof(playerNames));

        if (playerNamesList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Player names cannot be null or empty.", nameof(playerNames));

        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (defaultBankroll.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(defaultBankroll), "Default bankroll cannot be negative.");

        SessionId = sessionId;
        StartTime = DateTime.UtcNow;
        EndTime = null;
        RoundsPlayed = 0;
        IsActive = true;
        DefaultBankroll = defaultBankroll;

        _players = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);
        _roundSummaries = new List<GameSummary>();
        _sessionStatistics = new Dictionary<string, PlayerStatistics>(StringComparer.OrdinalIgnoreCase);

        // Initialize players with default bankrolls
        foreach (var playerName in playerNamesList.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var player = new Player(playerName, PlayerType.Human, defaultBankroll);
            _players[playerName] = player;
            _sessionStatistics[playerName] = new PlayerStatistics(playerName);
        }
    }

    /// <summary>
    /// Gets the unique identifier for this session.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the time when the session started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the time when the session ended, or null if still active.
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Gets the number of rounds played in this session.
    /// </summary>
    public int RoundsPlayed { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the session is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the game configuration for this session.
    /// </summary>
    public GameConfiguration Configuration { get; }

    /// <summary>
    /// Gets the default bankroll amount for new players.
    /// </summary>
    public Money DefaultBankroll { get; }

    /// <summary>
    /// Gets the duration of the session.
    /// </summary>
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

    /// <summary>
    /// Gets the players participating in this session.
    /// </summary>
    public IReadOnlyDictionary<string, Player> Players => _players.AsReadOnly();

    /// <summary>
    /// Gets the summaries of all rounds played in this session.
    /// </summary>
    public IReadOnlyList<GameSummary> RoundSummaries => _roundSummaries.AsReadOnly();

    /// <summary>
    /// Gets the session statistics for all players.
    /// </summary>
    public IReadOnlyDictionary<string, PlayerStatistics> SessionStatistics => _sessionStatistics.AsReadOnly();

    /// <summary>
    /// Gets a player by name.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The player with the specified name.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when no player with the specified name exists.</exception>
    public Player GetPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (!_players.TryGetValue(playerName, out var player))
            throw new KeyNotFoundException($"Player '{playerName}' not found in session.");

        return player;
    }

    /// <summary>
    /// Checks if a player exists in the session.
    /// </summary>
    /// <param name="playerName">The name of the player to check.</param>
    /// <returns>True if the player exists, false otherwise.</returns>
    public bool HasPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        return _players.ContainsKey(playerName);
    }

    /// <summary>
    /// Adds a player to the session.
    /// </summary>
    /// <param name="playerName">The name of the player to add.</param>
    /// <param name="initialBankroll">The initial bankroll for the player (uses default if not specified).</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when session is not active or player already exists.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when initialBankroll is negative.</exception>
    public void AddPlayer(string playerName, Money? initialBankroll = null)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (!IsActive)
            throw new InvalidOperationException("Cannot add players to an inactive session.");

        if (_players.ContainsKey(playerName))
            throw new InvalidOperationException($"Player '{playerName}' already exists in the session.");

        var bankroll = initialBankroll ?? DefaultBankroll;
        if (bankroll.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(initialBankroll), "Initial bankroll cannot be negative.");

        var player = new Player(playerName, PlayerType.Human, bankroll);
        _players[playerName] = player;
        _sessionStatistics[playerName] = new PlayerStatistics(playerName);
    }

    /// <summary>
    /// Removes a player from the session.
    /// </summary>
    /// <param name="playerName">The name of the player to remove.</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when session is not active.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when player does not exist.</exception>
    public void RemovePlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (!IsActive)
            throw new InvalidOperationException("Cannot remove players from an inactive session.");

        if (!_players.ContainsKey(playerName))
            throw new KeyNotFoundException($"Player '{playerName}' not found in session.");

        _players.Remove(playerName);
        _sessionStatistics.Remove(playerName);
    }

    /// <summary>
    /// Records the completion of a round and updates session statistics.
    /// </summary>
    /// <param name="roundSummary">The summary of the completed round.</param>
    /// <exception cref="ArgumentNullException">Thrown when roundSummary is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when session is not active.</exception>
    public void RecordRound(GameSummary roundSummary)
    {
        if (roundSummary == null)
            throw new ArgumentNullException(nameof(roundSummary));

        if (!IsActive)
            throw new InvalidOperationException("Cannot record rounds for an inactive session.");

        _roundSummaries.Add(roundSummary);
        RoundsPlayed++;

        // Update session statistics for each player
        foreach (var (playerName, result) in roundSummary.PlayerResults)
        {
            if (_players.TryGetValue(playerName, out var player) && 
                _sessionStatistics.TryGetValue(playerName, out var stats))
            {
                var betAmount = player.CurrentBet?.Amount ?? Money.Zero;
                var payout = CalculatePayout(result, betAmount);
                stats.RecordGame(result, betAmount, payout);
            }
        }
    }

    /// <summary>
    /// Ends the session and marks it as inactive.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when session is already inactive.</exception>
    public void EndSession()
    {
        if (!IsActive)
            throw new InvalidOperationException("Session is already inactive.");

        EndTime = DateTime.UtcNow;
        IsActive = false;
    }

    /// <summary>
    /// Resets all players for a new round.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when session is not active.</exception>
    public void ResetPlayersForNewRound()
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot reset players for an inactive session.");

        foreach (var player in _players.Values)
        {
            player.ResetForNewRound();
        }
    }

    /// <summary>
    /// Gets the players who are still able to play (have positive bankrolls).
    /// </summary>
    /// <returns>A collection of players with positive bankrolls.</returns>
    public IEnumerable<Player> GetActivePlayers()
    {
        return _players.Values.Where(p => p.Bankroll.IsPositive);
    }

    /// <summary>
    /// Gets the players who are unable to continue playing (have zero or negative bankrolls).
    /// </summary>
    /// <returns>A collection of players with zero or negative bankrolls.</returns>
    public IEnumerable<Player> GetInactivePlayers()
    {
        return _players.Values.Where(p => !p.Bankroll.IsPositive);
    }

    /// <summary>
    /// Checks if the session can continue (has at least one active player).
    /// </summary>
    /// <returns>True if the session can continue, false otherwise.</returns>
    public bool CanContinue()
    {
        return IsActive && GetActivePlayers().Any();
    }

    /// <summary>
    /// Gets the player with the highest bankroll.
    /// </summary>
    /// <returns>The player with the highest bankroll, or null if no players exist.</returns>
    public Player? GetBiggestWinner()
    {
        return _players.Values.OrderByDescending(p => p.Bankroll.Amount).FirstOrDefault();
    }

    /// <summary>
    /// Gets the largest pot (total bets) from any round in the session.
    /// </summary>
    /// <returns>The largest pot amount.</returns>
    public Money GetLargestPot()
    {
        if (!_roundSummaries.Any())
            return Money.Zero;

        return _players.Values
            .SelectMany(p => _roundSummaries.Select(r => p.CurrentBet?.Amount ?? Money.Zero))
            .DefaultIfEmpty(Money.Zero)
            .Aggregate((max, current) => current > max ? current : max);
    }

    /// <summary>
    /// Calculates the payout for a given game result and bet amount.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="betAmount">The bet amount.</param>
    /// <returns>The payout amount.</returns>
    private Money CalculatePayout(GameResult result, Money betAmount)
    {
        return result switch
        {
            GameResult.Win => betAmount,
            GameResult.Blackjack => betAmount * (decimal)Configuration.BlackjackPayout,
            GameResult.Push => Money.Zero,
            GameResult.Lose => Money.Zero,
            _ => Money.Zero
        };
    }

    /// <summary>
    /// Returns a string representation of the session.
    /// </summary>
    /// <returns>A formatted string describing the session.</returns>
    public override string ToString()
    {
        var status = IsActive ? "Active" : "Ended";
        var playerCount = _players.Count;
        var duration = Duration.TotalMinutes;

        return $"Session {SessionId}: {status}, {playerCount} players, {RoundsPlayed} rounds, {duration:F1} minutes";
    }

    /// <summary>
    /// Determines equality based on session ID.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not GameSession other)
            return false;

        return SessionId.Equals(other.SessionId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the hash code based on session ID.
    /// </summary>
    /// <returns>The hash code for this session.</returns>
    public override int GetHashCode()
    {
        return SessionId.ToLowerInvariant().GetHashCode();
    }
}