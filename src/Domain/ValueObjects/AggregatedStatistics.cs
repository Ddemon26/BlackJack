namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents aggregated statistics across all players in the system.
/// Provides summary information and insights about overall game performance.
/// </summary>
/// <remarks>
/// This value object contains calculated statistics that provide insights into
/// the overall performance of all players combined. It's useful for system-wide
/// reporting and analysis of game patterns and outcomes.
/// </remarks>
public class AggregatedStatistics
{
    /// <summary>
    /// Initializes a new instance of the AggregatedStatistics class.
    /// </summary>
    /// <param name="totalPlayers">The total number of players with statistics.</param>
    /// <param name="totalGamesPlayed">The total number of games played across all players.</param>
    /// <param name="totalGamesWon">The total number of games won across all players.</param>
    /// <param name="totalGamesLost">The total number of games lost across all players.</param>
    /// <param name="totalGamesPushed">The total number of games pushed across all players.</param>
    /// <param name="totalBlackjacksAchieved">The total number of blackjacks achieved across all players.</param>
    /// <param name="totalAmountWagered">The total amount wagered across all players.</param>
    /// <param name="totalNetWinnings">The total net winnings across all players.</param>
    /// <param name="earliestGameDate">The date of the earliest game played.</param>
    /// <param name="latestGameDate">The date of the latest game played.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any count parameter is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when game counts are inconsistent or dates are invalid.</exception>
    public AggregatedStatistics(
        int totalPlayers,
        int totalGamesPlayed,
        int totalGamesWon,
        int totalGamesLost,
        int totalGamesPushed,
        int totalBlackjacksAchieved,
        Money totalAmountWagered,
        Money totalNetWinnings,
        DateTime? earliestGameDate,
        DateTime? latestGameDate)
    {
        if (totalPlayers < 0)
            throw new ArgumentOutOfRangeException(nameof(totalPlayers), "Total players cannot be negative.");

        if (totalGamesPlayed < 0)
            throw new ArgumentOutOfRangeException(nameof(totalGamesPlayed), "Total games played cannot be negative.");

        if (totalGamesWon < 0)
            throw new ArgumentOutOfRangeException(nameof(totalGamesWon), "Total games won cannot be negative.");

        if (totalGamesLost < 0)
            throw new ArgumentOutOfRangeException(nameof(totalGamesLost), "Total games lost cannot be negative.");

        if (totalGamesPushed < 0)
            throw new ArgumentOutOfRangeException(nameof(totalGamesPushed), "Total games pushed cannot be negative.");

        if (totalBlackjacksAchieved < 0)
            throw new ArgumentOutOfRangeException(nameof(totalBlackjacksAchieved), "Total blackjacks achieved cannot be negative.");

        if (totalAmountWagered.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(totalAmountWagered), "Total amount wagered cannot be negative.");

        if (totalGamesWon + totalGamesLost + totalGamesPushed != totalGamesPlayed)
            throw new ArgumentException("Total games played must equal the sum of games won, lost, and pushed.");

        if (totalBlackjacksAchieved > totalGamesWon)
            throw new ArgumentException("Total blackjacks achieved cannot exceed total games won.");

        if (earliestGameDate.HasValue && latestGameDate.HasValue && earliestGameDate > latestGameDate)
            throw new ArgumentException("Earliest game date cannot be after latest game date.");

        TotalPlayers = totalPlayers;
        TotalGamesPlayed = totalGamesPlayed;
        TotalGamesWon = totalGamesWon;
        TotalGamesLost = totalGamesLost;
        TotalGamesPushed = totalGamesPushed;
        TotalBlackjacksAchieved = totalBlackjacksAchieved;
        TotalAmountWagered = totalAmountWagered;
        TotalNetWinnings = totalNetWinnings;
        EarliestGameDate = earliestGameDate;
        LatestGameDate = latestGameDate;
    }

    /// <summary>
    /// Gets the total number of players with statistics.
    /// </summary>
    public int TotalPlayers { get; }

    /// <summary>
    /// Gets the total number of games played across all players.
    /// </summary>
    public int TotalGamesPlayed { get; }

    /// <summary>
    /// Gets the total number of games won across all players.
    /// </summary>
    public int TotalGamesWon { get; }

    /// <summary>
    /// Gets the total number of games lost across all players.
    /// </summary>
    public int TotalGamesLost { get; }

    /// <summary>
    /// Gets the total number of games pushed across all players.
    /// </summary>
    public int TotalGamesPushed { get; }

    /// <summary>
    /// Gets the total number of blackjacks achieved across all players.
    /// </summary>
    public int TotalBlackjacksAchieved { get; }

    /// <summary>
    /// Gets the total amount wagered across all players.
    /// </summary>
    public Money TotalAmountWagered { get; }

    /// <summary>
    /// Gets the total net winnings across all players.
    /// </summary>
    public Money TotalNetWinnings { get; }

    /// <summary>
    /// Gets the date of the earliest game played, or null if no games have been played.
    /// </summary>
    public DateTime? EarliestGameDate { get; }

    /// <summary>
    /// Gets the date of the latest game played, or null if no games have been played.
    /// </summary>
    public DateTime? LatestGameDate { get; }

    /// <summary>
    /// Gets the overall win percentage across all players.
    /// </summary>
    public double OverallWinPercentage => TotalGamesPlayed > 0 ? (double)TotalGamesWon / TotalGamesPlayed : 0.0;

    /// <summary>
    /// Gets the overall loss percentage across all players.
    /// </summary>
    public double OverallLossPercentage => TotalGamesPlayed > 0 ? (double)TotalGamesLost / TotalGamesPlayed : 0.0;

    /// <summary>
    /// Gets the overall push percentage across all players.
    /// </summary>
    public double OverallPushPercentage => TotalGamesPlayed > 0 ? (double)TotalGamesPushed / TotalGamesPlayed : 0.0;

    /// <summary>
    /// Gets the overall blackjack percentage across all players.
    /// </summary>
    public double OverallBlackjackPercentage => TotalGamesPlayed > 0 ? (double)TotalBlackjacksAchieved / TotalGamesPlayed : 0.0;

    /// <summary>
    /// Gets the average bet amount across all players.
    /// </summary>
    public Money AverageBetAmount => TotalGamesPlayed > 0 ? TotalAmountWagered / TotalGamesPlayed : Money.Zero;

    /// <summary>
    /// Gets the overall return on investment percentage.
    /// </summary>
    public double OverallReturnOnInvestment => TotalAmountWagered.IsZero ? 0.0 : (double)(TotalNetWinnings.Amount / TotalAmountWagered.Amount);

    /// <summary>
    /// Gets the average games played per player.
    /// </summary>
    public double AverageGamesPerPlayer => TotalPlayers > 0 ? (double)TotalGamesPlayed / TotalPlayers : 0.0;

    /// <summary>
    /// Gets the average net winnings per player.
    /// </summary>
    public Money AverageNetWinningsPerPlayer => TotalPlayers > 0 ? TotalNetWinnings / TotalPlayers : Money.Zero;

    /// <summary>
    /// Gets the total play period duration, or null if no games have been played.
    /// </summary>
    public TimeSpan? TotalPlayPeriod => EarliestGameDate.HasValue && LatestGameDate.HasValue 
        ? LatestGameDate.Value - EarliestGameDate.Value 
        : null;

    /// <summary>
    /// Gets a value indicating whether the overall system is profitable.
    /// </summary>
    public bool IsOverallProfitable => TotalNetWinnings.IsPositive;

    /// <summary>
    /// Returns a string representation of the aggregated statistics.
    /// </summary>
    /// <returns>A formatted string showing key aggregated statistics.</returns>
    public override string ToString()
    {
        if (TotalPlayers == 0)
        {
            return "No player statistics available";
        }

        return $"Aggregated Statistics: {TotalPlayers} players, {TotalGamesPlayed} games, " +
               $"{TotalGamesWon}W-{TotalGamesLost}L-{TotalGamesPushed}P ({OverallWinPercentage:P1}), " +
               $"{TotalBlackjacksAchieved} BJ, Net: {TotalNetWinnings}";
    }

    /// <summary>
    /// Returns a detailed string representation of the aggregated statistics.
    /// </summary>
    /// <returns>A comprehensive formatted string showing all aggregated statistics.</returns>
    public string ToDetailedString()
    {
        if (TotalPlayers == 0)
        {
            return "No player statistics available";
        }

        var playPeriod = TotalPlayPeriod?.TotalDays ?? 0;
        var playPeriodText = playPeriod > 0 ? $"{playPeriod:F0} days" : "N/A";

        return $"Aggregated Statistics:\n" +
               $"  Players: {TotalPlayers}\n" +
               $"  Total Games: {TotalGamesPlayed} ({TotalGamesWon}W-{TotalGamesLost}L-{TotalGamesPushed}P)\n" +
               $"  Win Rate: {OverallWinPercentage:P2}\n" +
               $"  Blackjacks: {TotalBlackjacksAchieved} ({OverallBlackjackPercentage:P2})\n" +
               $"  Total Wagered: {TotalAmountWagered}\n" +
               $"  Total Net Winnings: {TotalNetWinnings}\n" +
               $"  Average Bet: {AverageBetAmount}\n" +
               $"  Overall ROI: {OverallReturnOnInvestment:P2}\n" +
               $"  Avg Games/Player: {AverageGamesPerPlayer:F1}\n" +
               $"  Avg Winnings/Player: {AverageNetWinningsPerPlayer}\n" +
               $"  Play Period: {playPeriodText}\n" +
               $"  System Profitable: {(IsOverallProfitable ? "Yes" : "No")}";
    }

    /// <summary>
    /// Determines equality based on all statistical values.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not AggregatedStatistics other)
            return false;

        return TotalPlayers == other.TotalPlayers &&
               TotalGamesPlayed == other.TotalGamesPlayed &&
               TotalGamesWon == other.TotalGamesWon &&
               TotalGamesLost == other.TotalGamesLost &&
               TotalGamesPushed == other.TotalGamesPushed &&
               TotalBlackjacksAchieved == other.TotalBlackjacksAchieved &&
               TotalAmountWagered.Equals(other.TotalAmountWagered) &&
               TotalNetWinnings.Equals(other.TotalNetWinnings) &&
               EarliestGameDate == other.EarliestGameDate &&
               LatestGameDate == other.LatestGameDate;
    }

    /// <summary>
    /// Gets the hash code based on key statistics.
    /// </summary>
    /// <returns>The hash code for this aggregated statistics.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            TotalPlayers,
            TotalGamesPlayed,
            TotalGamesWon,
            TotalGamesLost,
            TotalGamesPushed,
            TotalBlackjacksAchieved,
            TotalAmountWagered,
            TotalNetWinnings
        );
    }
}