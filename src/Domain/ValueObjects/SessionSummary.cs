using GroupProject.Domain.Entities;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents a comprehensive summary of a completed gaming session with statistics and results.
/// </summary>
/// <remarks>
/// This value object provides a complete overview of a gaming session including duration,
/// rounds played, player statistics, and financial outcomes. It serves as a final report
/// of the session's activities and can be used for display, logging, or historical analysis.
/// </remarks>
public class SessionSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionSummary"/> class.
    /// </summary>
    /// <param name="sessionId">The unique identifier of the session.</param>
    /// <param name="startTime">The time when the session started.</param>
    /// <param name="endTime">The time when the session ended.</param>
    /// <param name="roundsPlayed">The number of rounds played in the session.</param>
    /// <param name="playerStatistics">The final statistics for each player.</param>
    /// <param name="finalBankrolls">The final bankroll amounts for each player.</param>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when playerStatistics or finalBankrolls is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when endTime is before startTime or roundsPlayed is negative.</exception>
    public SessionSummary(
        string sessionId,
        DateTime startTime,
        DateTime endTime,
        int roundsPlayed,
        IReadOnlyDictionary<string, PlayerStatistics> playerStatistics,
        IReadOnlyDictionary<string, Money> finalBankrolls)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));

        if (endTime < startTime)
            throw new ArgumentOutOfRangeException(nameof(endTime), "End time cannot be before start time.");

        if (roundsPlayed < 0)
            throw new ArgumentOutOfRangeException(nameof(roundsPlayed), "Rounds played cannot be negative.");

        SessionId = sessionId;
        StartTime = startTime;
        EndTime = endTime;
        RoundsPlayed = roundsPlayed;
        PlayerStatistics = playerStatistics ?? throw new ArgumentNullException(nameof(playerStatistics));
        FinalBankrolls = finalBankrolls ?? throw new ArgumentNullException(nameof(finalBankrolls));
    }

    /// <summary>
    /// Gets the unique identifier of the session.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the time when the session started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the time when the session ended.
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    /// Gets the duration of the session.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets the number of rounds played in the session.
    /// </summary>
    public int RoundsPlayed { get; }

    /// <summary>
    /// Gets the final statistics for each player.
    /// </summary>
    public IReadOnlyDictionary<string, PlayerStatistics> PlayerStatistics { get; }

    /// <summary>
    /// Gets the final bankroll amounts for each player.
    /// </summary>
    public IReadOnlyDictionary<string, Money> FinalBankrolls { get; }

    /// <summary>
    /// Gets the player with the highest final bankroll.
    /// </summary>
    public string? BiggestWinner
    {
        get
        {
            if (!FinalBankrolls.Any())
                return null;

            return FinalBankrolls
                .OrderByDescending(kvp => kvp.Value.Amount)
                .First()
                .Key;
        }
    }

    /// <summary>
    /// Gets the largest final bankroll amount.
    /// </summary>
    public Money LargestBankroll
    {
        get
        {
            if (!FinalBankrolls.Any())
                return Money.Zero;

            return FinalBankrolls.Values.OrderByDescending(m => m.Amount).First();
        }
    }

    /// <summary>
    /// Gets the total amount wagered across all players and rounds.
    /// </summary>
    public Money TotalWagered
    {
        get
        {
            if (!PlayerStatistics.Any())
                return Money.Zero;

            return PlayerStatistics.Values
                .Select(stats => stats.TotalWagered)
                .Aggregate(Money.Zero, (sum, amount) => sum + amount);
        }
    }

    /// <summary>
    /// Gets the total net winnings across all players.
    /// </summary>
    public Money TotalNetWinnings
    {
        get
        {
            if (!PlayerStatistics.Any())
                return Money.Zero;

            return PlayerStatistics.Values
                .Select(stats => stats.NetWinnings)
                .Aggregate(Money.Zero, (sum, amount) => sum + amount);
        }
    }

    /// <summary>
    /// Gets the total number of games played across all players.
    /// </summary>
    public int TotalGamesPlayed
    {
        get
        {
            return PlayerStatistics.Values.Sum(stats => stats.GamesPlayed);
        }
    }

    /// <summary>
    /// Gets the total number of blackjacks achieved across all players.
    /// </summary>
    public int TotalBlackjacks
    {
        get
        {
            return PlayerStatistics.Values.Sum(stats => stats.BlackjacksAchieved);
        }
    }

    /// <summary>
    /// Gets the average rounds per hour for the session.
    /// </summary>
    public double RoundsPerHour
    {
        get
        {
            var hours = Duration.TotalHours;
            return hours > 0 ? RoundsPlayed / hours : 0;
        }
    }

    /// <summary>
    /// Gets the players who ended with a profit.
    /// </summary>
    public IEnumerable<string> ProfitablePlayers
    {
        get
        {
            return PlayerStatistics
                .Where(kvp => kvp.Value.IsProfitable)
                .Select(kvp => kvp.Key);
        }
    }

    /// <summary>
    /// Gets the players who ended with a loss.
    /// </summary>
    public IEnumerable<string> UnprofitablePlayers
    {
        get
        {
            return PlayerStatistics
                .Where(kvp => !kvp.Value.IsProfitable)
                .Select(kvp => kvp.Key);
        }
    }

    /// <summary>
    /// Gets the number of players who ended with a profit.
    /// </summary>
    public int ProfitablePlayerCount => ProfitablePlayers.Count();

    /// <summary>
    /// Gets the number of players who ended with a loss.
    /// </summary>
    public int UnprofitablePlayerCount => UnprofitablePlayers.Count();

    /// <summary>
    /// Gets the overall win percentage across all players.
    /// </summary>
    public double OverallWinPercentage
    {
        get
        {
            var totalGames = TotalGamesPlayed;
            if (totalGames == 0)
                return 0;

            var totalWins = PlayerStatistics.Values.Sum(stats => stats.GamesWon);
            return (double)totalWins / totalGames;
        }
    }

    /// <summary>
    /// Gets the average bet amount across all players and rounds.
    /// </summary>
    public Money AverageBetAmount
    {
        get
        {
            var totalGames = TotalGamesPlayed;
            if (totalGames == 0)
                return Money.Zero;

            return TotalWagered / totalGames;
        }
    }

    /// <summary>
    /// Gets statistics for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The player's statistics, or null if the player doesn't exist.</returns>
    public PlayerStatistics? GetPlayerStatistics(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        return PlayerStatistics.TryGetValue(playerName, out var stats) ? stats : null;
    }

    /// <summary>
    /// Gets the final bankroll for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The player's final bankroll, or null if the player doesn't exist.</returns>
    public Money? GetPlayerFinalBankroll(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        return FinalBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : null;
    }

    /// <summary>
    /// Creates a summary from a completed game session.
    /// </summary>
    /// <param name="session">The completed game session.</param>
    /// <returns>A session summary based on the provided session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when session is still active.</exception>
    public static SessionSummary FromSession(GameSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        if (session.IsActive)
            throw new InvalidOperationException("Cannot create summary from an active session.");

        if (!session.EndTime.HasValue)
            throw new InvalidOperationException("Session must have an end time to create summary.");

        var finalBankrolls = session.Players.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Bankroll,
            StringComparer.OrdinalIgnoreCase);

        return new SessionSummary(
            session.SessionId,
            session.StartTime,
            session.EndTime.Value,
            session.RoundsPlayed,
            session.SessionStatistics,
            finalBankrolls);
    }

    /// <summary>
    /// Returns a string representation of the session summary.
    /// </summary>
    /// <returns>A formatted string showing key session metrics.</returns>
    public override string ToString()
    {
        var playerCount = PlayerStatistics.Count;
        var duration = Duration.TotalMinutes;
        var winner = BiggestWinner ?? "None";

        return $"Session {SessionId}: {playerCount} players, {RoundsPlayed} rounds, " +
               $"{duration:F1} minutes, Winner: {winner}";
    }

    /// <summary>
    /// Returns a detailed string representation of the session summary.
    /// </summary>
    /// <returns>A comprehensive formatted string showing all session details.</returns>
    public string ToDetailedString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"Session Summary: {SessionId}");
        sb.AppendLine($"Duration: {StartTime:yyyy-MM-dd HH:mm} to {EndTime:yyyy-MM-dd HH:mm} ({Duration.TotalHours:F1} hours)");
        sb.AppendLine($"Rounds Played: {RoundsPlayed} ({RoundsPerHour:F1} per hour)");
        sb.AppendLine($"Total Games: {TotalGamesPlayed}");
        sb.AppendLine($"Total Wagered: {TotalWagered}");
        sb.AppendLine($"Total Net Winnings: {TotalNetWinnings}");
        sb.AppendLine($"Average Bet: {AverageBetAmount}");
        sb.AppendLine($"Overall Win Rate: {OverallWinPercentage:P2}");
        sb.AppendLine($"Total Blackjacks: {TotalBlackjacks}");
        sb.AppendLine();

        sb.AppendLine("Player Results:");
        foreach (var (playerName, stats) in PlayerStatistics.OrderByDescending(kvp => kvp.Value.NetWinnings.Amount))
        {
            var finalBankroll = FinalBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : Money.Zero;
            sb.AppendLine($"  {playerName}: {stats.NetWinnings} net, {finalBankroll} final bankroll, {stats.WinPercentage:P1} win rate");
        }

        if (!string.IsNullOrEmpty(BiggestWinner))
        {
            sb.AppendLine();
            sb.AppendLine($"Biggest Winner: {BiggestWinner} ({LargestBankroll})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a formatted summary suitable for console display.
    /// </summary>
    /// <returns>A formatted string optimized for console output.</returns>
    public string ToConsoleString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║                    SESSION SUMMARY                           ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        sb.AppendLine($"║ Session ID: {SessionId,-47} ║");
        sb.AppendLine($"║ Duration: {Duration.TotalHours:F1} hours ({RoundsPlayed} rounds)                        ║");
        sb.AppendLine($"║ Total Wagered: {TotalWagered,-42} ║");
        sb.AppendLine($"║ Net Result: {TotalNetWinnings,-45} ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
        
        if (PlayerStatistics.Any())
        {
            sb.AppendLine("║                     PLAYER RESULTS                           ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            
            foreach (var (playerName, stats) in PlayerStatistics.OrderByDescending(kvp => kvp.Value.NetWinnings.Amount))
            {
                var finalBankroll = FinalBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : Money.Zero;
                var winRate = stats.WinPercentage;
                sb.AppendLine($"║ {playerName,-12} │ Net: {stats.NetWinnings,8} │ Final: {finalBankroll,8} │ Win: {winRate:P0} ║");
            }
        }
        
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        
        return sb.ToString();
    }

    /// <summary>
    /// Returns a compact summary suitable for logging or quick display.
    /// </summary>
    /// <returns>A compact formatted string with key metrics.</returns>
    public string ToCompactString()
    {
        var winner = BiggestWinner ?? "None";
        var profitablePlayers = ProfitablePlayerCount;
        var totalPlayers = PlayerStatistics.Count;
        
        return $"Session {SessionId}: {Duration.TotalHours:F1}h, {RoundsPlayed}r, " +
               $"{totalPlayers}p ({profitablePlayers} profitable), " +
               $"Wagered: {TotalWagered}, Net: {TotalNetWinnings}, Winner: {winner}";
    }

    /// <summary>
    /// Returns statistics formatted as a table for display.
    /// </summary>
    /// <returns>A table-formatted string showing player statistics.</returns>
    public string ToTableString()
    {
        if (!PlayerStatistics.Any())
            return "No player statistics available.";

        var sb = new System.Text.StringBuilder();
        
        // Header
        sb.AppendLine("┌──────────────┬──────────┬──────────┬──────────┬──────────┬──────────┬──────────┐");
        sb.AppendLine("│ Player       │ Games    │ Won      │ Lost     │ Pushed   │ Net      │ Final    │");
        sb.AppendLine("├──────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼──────────┤");
        
        // Player rows
        foreach (var (playerName, stats) in PlayerStatistics.OrderByDescending(kvp => kvp.Value.NetWinnings.Amount))
        {
            var finalBankroll = FinalBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : Money.Zero;
            sb.AppendLine($"│ {playerName,-12} │ {stats.GamesPlayed,8} │ {stats.GamesWon,8} │ {stats.GamesLost,8} │ {stats.GamesPushed,8} │ {stats.NetWinnings,8} │ {finalBankroll,8} │");
        }
        
        // Footer
        sb.AppendLine("└──────────────┴──────────┴──────────┴──────────┴──────────┴──────────┴──────────┘");
        
        // Summary row
        sb.AppendLine($"Totals: {TotalGamesPlayed} games, {TotalWagered} wagered, {TotalNetWinnings} net result");
        
        return sb.ToString();
    }

    /// <summary>
    /// Calculates the house edge based on total wagered vs. total net winnings.
    /// </summary>
    /// <returns>The house edge as a percentage (positive means house advantage).</returns>
    public double CalculateHouseEdge()
    {
        if (TotalWagered.IsZero)
            return 0.0;

        // House edge = (Total wagered - Total returned) / Total wagered
        // Since TotalNetWinnings is player perspective, house edge is negative of player edge
        var playerEdge = (double)(TotalNetWinnings.Amount / TotalWagered.Amount);
        return -playerEdge;
    }

    /// <summary>
    /// Gets the most active player (played the most games).
    /// </summary>
    /// <returns>The name of the most active player, or null if no players exist.</returns>
    public string? GetMostActivePlayer()
    {
        if (!PlayerStatistics.Any())
            return null;

        return PlayerStatistics
            .OrderByDescending(kvp => kvp.Value.GamesPlayed)
            .First()
            .Key;
    }

    /// <summary>
    /// Gets the player with the highest win percentage (minimum 5 games played).
    /// </summary>
    /// <returns>The name of the player with the highest win percentage, or null if no qualifying players exist.</returns>
    public string? GetBestWinRatePlayer()
    {
        var qualifyingPlayers = PlayerStatistics
            .Where(kvp => kvp.Value.GamesPlayed >= 5)
            .OrderByDescending(kvp => kvp.Value.WinPercentage);

        return qualifyingPlayers.FirstOrDefault().Key;
    }

    /// <summary>
    /// Gets the player who achieved the most blackjacks.
    /// </summary>
    /// <returns>The name of the player with the most blackjacks, or null if no players exist.</returns>
    public string? GetBlackjackChampion()
    {
        if (!PlayerStatistics.Any())
            return null;

        var champion = PlayerStatistics
            .Where(kvp => kvp.Value.BlackjacksAchieved > 0)
            .OrderByDescending(kvp => kvp.Value.BlackjacksAchieved)
            .FirstOrDefault();

        return champion.Key;
    }

    /// <summary>
    /// Calculates the average session metrics across all players.
    /// </summary>
    /// <returns>A tuple containing average games per player, average net winnings per player, and average win rate.</returns>
    public (double AvgGamesPerPlayer, Money AvgNetWinningsPerPlayer, double AvgWinRate) CalculateAverageMetrics()
    {
        if (!PlayerStatistics.Any())
            return (0.0, Money.Zero, 0.0);

        var playerCount = PlayerStatistics.Count;
        var avgGames = (double)TotalGamesPlayed / playerCount;
        var avgNetWinnings = TotalNetWinnings / playerCount;
        var avgWinRate = PlayerStatistics.Values.Average(stats => stats.WinPercentage);

        return (avgGames, avgNetWinnings, avgWinRate);
    }

    /// <summary>
    /// Gets session highlights including notable achievements and records.
    /// </summary>
    /// <returns>A collection of highlight strings describing notable session events.</returns>
    public IEnumerable<string> GetSessionHighlights()
    {
        var highlights = new List<string>();

        // Duration highlights
        if (Duration.TotalHours >= 3)
            highlights.Add($"Marathon session: {Duration.TotalHours:F1} hours of play");
        
        if (RoundsPerHour > 20)
            highlights.Add($"Fast-paced action: {RoundsPerHour:F1} rounds per hour");

        // Player achievements
        var mostActive = GetMostActivePlayer();
        if (mostActive != null)
        {
            var gamesPlayed = PlayerStatistics[mostActive].GamesPlayed;
            if (gamesPlayed >= 50)
                highlights.Add($"{mostActive} played {gamesPlayed} games - most active player");
        }

        var bestWinRate = GetBestWinRatePlayer();
        if (bestWinRate != null)
        {
            var winRate = PlayerStatistics[bestWinRate].WinPercentage;
            if (winRate >= 0.6)
                highlights.Add($"{bestWinRate} achieved {winRate:P1} win rate - excellent performance");
        }

        var blackjackChamp = GetBlackjackChampion();
        if (blackjackChamp != null)
        {
            var blackjacks = PlayerStatistics[blackjackChamp].BlackjacksAchieved;
            if (blackjacks >= 5)
                highlights.Add($"{blackjackChamp} hit {blackjacks} blackjacks - blackjack champion");
        }

        // Financial highlights
        if (TotalWagered.Amount >= 10000)
            highlights.Add($"High stakes session: {TotalWagered} total wagered");

        var biggestWinner = BiggestWinner;
        if (biggestWinner != null)
        {
            var winnings = PlayerStatistics[biggestWinner].NetWinnings;
            if (winnings.Amount >= 1000)
                highlights.Add($"{biggestWinner} won big: {winnings} net profit");
        }

        // Overall session highlights
        if (OverallWinPercentage >= 0.55)
            highlights.Add($"Players had a good session: {OverallWinPercentage:P1} overall win rate");
        else if (OverallWinPercentage <= 0.45)
            highlights.Add($"House had the edge: {OverallWinPercentage:P1} player win rate");

        if (TotalBlackjacks >= 10)
            highlights.Add($"Blackjack bonanza: {TotalBlackjacks} total blackjacks achieved");

        return highlights;
    }

    /// <summary>
    /// Determines equality based on session ID and end time.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not SessionSummary other)
            return false;

        return SessionId.Equals(other.SessionId, StringComparison.OrdinalIgnoreCase) &&
               EndTime.Equals(other.EndTime);
    }

    /// <summary>
    /// Gets the hash code based on session ID and end time.
    /// </summary>
    /// <returns>The hash code for this session summary.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(SessionId.ToLowerInvariant(), EndTime);
    }
}