using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Models;

/// <summary>
/// Represents a summary of game results for all players.
/// </summary>
public class GameSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameSummary"/> class.
    /// </summary>
    /// <param name="playerResults">The results for each player.</param>
    /// <param name="dealerHand">The dealer's final hand.</param>
    /// <param name="gameEndTime">The time when the game ended.</param>
    public GameSummary(
        IReadOnlyDictionary<string, GameResult> playerResults,
        Hand dealerHand,
        DateTime gameEndTime)
    {
        PlayerResults = playerResults ?? throw new ArgumentNullException(nameof(playerResults));
        DealerHand = dealerHand ?? throw new ArgumentNullException(nameof(dealerHand));
        GameEndTime = gameEndTime;
    }

    /// <summary>
    /// Gets the results for each player by name.
    /// </summary>
    public IReadOnlyDictionary<string, GameResult> PlayerResults { get; }

    /// <summary>
    /// Gets the dealer's final hand.
    /// </summary>
    public Hand DealerHand { get; }

    /// <summary>
    /// Gets the time when the game ended.
    /// </summary>
    public DateTime GameEndTime { get; }

    /// <summary>
    /// Gets the number of players who won.
    /// </summary>
    public int WinnerCount => PlayerResults.Values.Count(r => r == GameResult.Win || r == GameResult.Blackjack);

    /// <summary>
    /// Gets the number of players who lost.
    /// </summary>
    public int LoserCount => PlayerResults.Values.Count(r => r == GameResult.Lose);

    /// <summary>
    /// Gets the number of players who pushed (tied).
    /// </summary>
    public int PushCount => PlayerResults.Values.Count(r => r == GameResult.Push);

    /// <summary>
    /// Gets the number of players who got blackjack.
    /// </summary>
    public int BlackjackCount => PlayerResults.Values.Count(r => r == GameResult.Blackjack);
}