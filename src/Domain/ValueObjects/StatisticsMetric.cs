namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the different metrics by which player statistics can be ranked or sorted.
/// </summary>
public enum StatisticsMetric
{
    /// <summary>
    /// Total number of games played
    /// </summary>
    GamesPlayed,
    
    /// <summary>
    /// Total number of games won
    /// </summary>
    GamesWon,
    
    /// <summary>
    /// Win percentage (games won / games played)
    /// </summary>
    WinPercentage,
    
    /// <summary>
    /// Total net winnings (can be negative)
    /// </summary>
    NetWinnings,
    
    /// <summary>
    /// Total amount wagered
    /// </summary>
    TotalWagered,
    
    /// <summary>
    /// Number of blackjacks achieved
    /// </summary>
    BlackjacksAchieved,
    
    /// <summary>
    /// Blackjack percentage (blackjacks / games played)
    /// </summary>
    BlackjackPercentage,
    
    /// <summary>
    /// Return on investment percentage
    /// </summary>
    ReturnOnInvestment,
    
    /// <summary>
    /// Average bet amount
    /// </summary>
    AverageBet,
    
    /// <summary>
    /// Total play time duration
    /// </summary>
    TotalPlayTime
}