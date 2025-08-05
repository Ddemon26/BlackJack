using System;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents player statistics for tracking game performance and history.
/// Provides comprehensive tracking of wins, losses, pushes, and financial metrics.
/// </summary>
/// <remarks>
/// This value object maintains immutable statistics that can be updated through specific methods.
/// It tracks both game outcomes and financial performance to provide comprehensive player analytics.
/// All calculations are performed with precision to ensure accurate statistical reporting.
/// </remarks>
public class PlayerStatistics
{
    /// <summary>
    /// Initializes a new instance of the PlayerStatistics class with default values.
    /// </summary>
    /// <param name="playerName">The name of the player these statistics belong to.</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    public PlayerStatistics(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        PlayerName = playerName.Trim();
        GamesPlayed = 0;
        GamesWon = 0;
        GamesLost = 0;
        GamesPushed = 0;
        BlackjacksAchieved = 0;
        TotalWagered = Money.Zero;
        NetWinnings = Money.Zero;
        FirstPlayed = DateTime.UtcNow;
        LastPlayed = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the PlayerStatistics class with specified values.
    /// </summary>
    /// <param name="playerName">The name of the player these statistics belong to.</param>
    /// <param name="gamesPlayed">The total number of games played.</param>
    /// <param name="gamesWon">The number of games won.</param>
    /// <param name="gamesLost">The number of games lost.</param>
    /// <param name="gamesPushed">The number of games that ended in a push.</param>
    /// <param name="blackjacksAchieved">The number of blackjacks achieved.</param>
    /// <param name="totalWagered">The total amount wagered.</param>
    /// <param name="netWinnings">The net winnings (can be negative for losses).</param>
    /// <param name="firstPlayed">The timestamp of the first game played.</param>
    /// <param name="lastPlayed">The timestamp of the last game played.</param>
    /// <exception cref="ArgumentException">Thrown when playerName is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any numeric parameter is negative or when game counts are inconsistent.</exception>
    public PlayerStatistics(string playerName, int gamesPlayed, int gamesWon, int gamesLost, 
        int gamesPushed, int blackjacksAchieved, Money totalWagered, Money netWinnings, 
        DateTime firstPlayed, DateTime lastPlayed)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null, empty, or whitespace.", nameof(playerName));

        if (gamesPlayed < 0)
            throw new ArgumentOutOfRangeException(nameof(gamesPlayed), "Games played cannot be negative.");

        if (gamesWon < 0)
            throw new ArgumentOutOfRangeException(nameof(gamesWon), "Games won cannot be negative.");

        if (gamesLost < 0)
            throw new ArgumentOutOfRangeException(nameof(gamesLost), "Games lost cannot be negative.");

        if (gamesPushed < 0)
            throw new ArgumentOutOfRangeException(nameof(gamesPushed), "Games pushed cannot be negative.");

        if (blackjacksAchieved < 0)
            throw new ArgumentOutOfRangeException(nameof(blackjacksAchieved), "Blackjacks achieved cannot be negative.");

        if (totalWagered.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(totalWagered), "Total wagered cannot be negative.");

        if (gamesWon + gamesLost + gamesPushed != gamesPlayed)
            throw new ArgumentOutOfRangeException(nameof(gamesPlayed), 
                "Games played must equal the sum of games won, lost, and pushed.");

        if (blackjacksAchieved > gamesWon)
            throw new ArgumentOutOfRangeException(nameof(blackjacksAchieved), 
                "Blackjacks achieved cannot exceed games won.");

        if (firstPlayed > lastPlayed)
            throw new ArgumentOutOfRangeException(nameof(lastPlayed), 
                "Last played cannot be before first played.");

        PlayerName = playerName.Trim();
        GamesPlayed = gamesPlayed;
        GamesWon = gamesWon;
        GamesLost = gamesLost;
        GamesPushed = gamesPushed;
        BlackjacksAchieved = blackjacksAchieved;
        TotalWagered = totalWagered;
        NetWinnings = netWinnings;
        FirstPlayed = firstPlayed;
        LastPlayed = lastPlayed;
    }

    /// <summary>
    /// Gets the name of the player these statistics belong to.
    /// </summary>
    public string PlayerName { get; }

    /// <summary>
    /// Gets the total number of games played.
    /// </summary>
    public int GamesPlayed { get; private set; }

    /// <summary>
    /// Gets the number of games won.
    /// </summary>
    public int GamesWon { get; private set; }

    /// <summary>
    /// Gets the number of games lost.
    /// </summary>
    public int GamesLost { get; private set; }

    /// <summary>
    /// Gets the number of games that ended in a push (tie).
    /// </summary>
    public int GamesPushed { get; private set; }

    /// <summary>
    /// Gets the number of blackjacks achieved.
    /// </summary>
    public int BlackjacksAchieved { get; private set; }

    /// <summary>
    /// Gets the total amount wagered across all games.
    /// </summary>
    public Money TotalWagered { get; private set; }

    /// <summary>
    /// Gets the net winnings (positive for profit, negative for losses).
    /// </summary>
    public Money NetWinnings { get; private set; }

    /// <summary>
    /// Gets the timestamp of the first game played.
    /// </summary>
    public DateTime FirstPlayed { get; }

    /// <summary>
    /// Gets the timestamp of the last game played.
    /// </summary>
    public DateTime LastPlayed { get; private set; }

    /// <summary>
    /// Gets the win percentage as a value between 0 and 1.
    /// </summary>
    public double WinPercentage => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed : 0.0;

    /// <summary>
    /// Gets the loss percentage as a value between 0 and 1.
    /// </summary>
    public double LossPercentage => GamesPlayed > 0 ? (double)GamesLost / GamesPlayed : 0.0;

    /// <summary>
    /// Gets the push percentage as a value between 0 and 1.
    /// </summary>
    public double PushPercentage => GamesPlayed > 0 ? (double)GamesPushed / GamesPlayed : 0.0;

    /// <summary>
    /// Gets the blackjack percentage as a value between 0 and 1.
    /// </summary>
    public double BlackjackPercentage => GamesPlayed > 0 ? (double)BlackjacksAchieved / GamesPlayed : 0.0;

    /// <summary>
    /// Gets the average bet amount.
    /// </summary>
    public Money AverageBet => GamesPlayed > 0 ? TotalWagered / GamesPlayed : Money.Zero;

    /// <summary>
    /// Gets the return on investment as a percentage.
    /// </summary>
    public double ReturnOnInvestment => TotalWagered.IsZero ? 0.0 : (double)(NetWinnings.Amount / TotalWagered.Amount);

    /// <summary>
    /// Gets a value indicating whether the player is profitable overall.
    /// </summary>
    public bool IsProfitable => NetWinnings.IsPositive;

    /// <summary>
    /// Gets the total duration of play time.
    /// </summary>
    public TimeSpan TotalPlayTime => LastPlayed - FirstPlayed;

    /// <summary>
    /// Records a game result and updates the statistics accordingly.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="betAmount">The amount that was bet.</param>
    /// <param name="payout">The payout received (zero for losses).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when betAmount is not positive or payout is negative.</exception>
    public void RecordGame(GameResult result, Money betAmount, Money payout)
    {
        if (!betAmount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(betAmount), "Bet amount must be positive.");

        if (payout.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(payout), "Payout cannot be negative.");

        GamesPlayed++;
        TotalWagered += betAmount;
        LastPlayed = DateTime.UtcNow;

        switch (result)
        {
            case GameResult.Win:
                GamesWon++;
                NetWinnings += payout;
                break;

            case GameResult.Blackjack:
                GamesWon++;
                BlackjacksAchieved++;
                NetWinnings += payout;
                break;

            case GameResult.Lose:
                GamesLost++;
                NetWinnings -= betAmount; // Lose the bet amount
                break;

            case GameResult.Push:
                GamesPushed++;
                // No change to net winnings for a push
                break;

            default:
                throw new ArgumentException($"Unknown game result: {result}", nameof(result));
        }
    }

    /// <summary>
    /// Resets all statistics to their initial values while preserving the player name and first played timestamp.
    /// </summary>
    public void Reset()
    {
        GamesPlayed = 0;
        GamesWon = 0;
        GamesLost = 0;
        GamesPushed = 0;
        BlackjacksAchieved = 0;
        TotalWagered = Money.Zero;
        NetWinnings = Money.Zero;
        LastPlayed = FirstPlayed;
    }

    /// <summary>
    /// Creates a copy of the current statistics with updated values.
    /// </summary>
    /// <param name="gamesPlayed">The new games played count.</param>
    /// <param name="gamesWon">The new games won count.</param>
    /// <param name="gamesLost">The new games lost count.</param>
    /// <param name="gamesPushed">The new games pushed count.</param>
    /// <param name="blackjacksAchieved">The new blackjacks achieved count.</param>
    /// <param name="totalWagered">The new total wagered amount.</param>
    /// <param name="netWinnings">The new net winnings amount.</param>
    /// <param name="lastPlayed">The new last played timestamp.</param>
    /// <returns>A new PlayerStatistics instance with the updated values.</returns>
    public PlayerStatistics WithUpdatedValues(int? gamesPlayed = null, int? gamesWon = null, 
        int? gamesLost = null, int? gamesPushed = null, int? blackjacksAchieved = null,
        Money? totalWagered = null, Money? netWinnings = null, DateTime? lastPlayed = null)
    {
        return new PlayerStatistics(
            PlayerName,
            gamesPlayed ?? GamesPlayed,
            gamesWon ?? GamesWon,
            gamesLost ?? GamesLost,
            gamesPushed ?? GamesPushed,
            blackjacksAchieved ?? BlackjacksAchieved,
            totalWagered ?? TotalWagered,
            netWinnings ?? NetWinnings,
            FirstPlayed,
            lastPlayed ?? LastPlayed
        );
    }

    /// <summary>
    /// Returns a string representation of the player statistics.
    /// </summary>
    /// <returns>A formatted string showing key statistics.</returns>
    public override string ToString()
    {
        if (GamesPlayed == 0)
        {
            return $"{PlayerName}: No games played";
        }

        return $"{PlayerName}: {GamesPlayed} games, {GamesWon}W-{GamesLost}L-{GamesPushed}P " +
               $"({WinPercentage:P1}), {BlackjacksAchieved} BJ, Net: {NetWinnings}";
    }

    /// <summary>
    /// Returns a detailed string representation of the player statistics.
    /// </summary>
    /// <returns>A comprehensive formatted string showing all statistics.</returns>
    public string ToDetailedString()
    {
        if (GamesPlayed == 0)
        {
            return $"{PlayerName}: No games played since {FirstPlayed:yyyy-MM-dd}";
        }

        return $"{PlayerName} Statistics:\n" +
               $"  Games: {GamesPlayed} ({GamesWon}W-{GamesLost}L-{GamesPushed}P)\n" +
               $"  Win Rate: {WinPercentage:P2}\n" +
               $"  Blackjacks: {BlackjacksAchieved} ({BlackjackPercentage:P2})\n" +
               $"  Total Wagered: {TotalWagered}\n" +
               $"  Net Winnings: {NetWinnings}\n" +
               $"  Average Bet: {AverageBet}\n" +
               $"  ROI: {ReturnOnInvestment:P2}\n" +
               $"  Play Period: {FirstPlayed:yyyy-MM-dd} to {LastPlayed:yyyy-MM-dd}\n" +
               $"  Total Play Time: {TotalPlayTime.TotalHours:F1} hours";
    }

    /// <summary>
    /// Determines equality based on player name and all statistical values.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not PlayerStatistics other)
            return false;

        return PlayerName.Equals(other.PlayerName, StringComparison.OrdinalIgnoreCase) &&
               GamesPlayed == other.GamesPlayed &&
               GamesWon == other.GamesWon &&
               GamesLost == other.GamesLost &&
               GamesPushed == other.GamesPushed &&
               BlackjacksAchieved == other.BlackjacksAchieved &&
               TotalWagered.Equals(other.TotalWagered) &&
               NetWinnings.Equals(other.NetWinnings) &&
               FirstPlayed.Equals(other.FirstPlayed) &&
               LastPlayed.Equals(other.LastPlayed);
    }

    /// <summary>
    /// Gets the hash code based on player name and key statistics.
    /// </summary>
    /// <returns>The hash code for this player statistics.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            PlayerName.ToLowerInvariant(),
            GamesPlayed,
            GamesWon,
            GamesLost,
            GamesPushed,
            BlackjacksAchieved,
            TotalWagered,
            NetWinnings
        );
    }
}