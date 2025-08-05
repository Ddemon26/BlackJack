namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the result of a payout calculation.
/// Contains information about the payout amount and total return for a bet.
/// </summary>
public class PayoutResult
{
    /// <summary>
    /// Initializes a new instance of the PayoutResult class.
    /// </summary>
    /// <param name="bet">The original bet.</param>
    /// <param name="gameResult">The game result that determined the payout.</param>
    /// <param name="payoutAmount">The payout amount (winnings only).</param>
    /// <param name="totalReturn">The total return (original bet + payout).</param>
    public PayoutResult(Bet bet, GameResult gameResult, Money payoutAmount, Money totalReturn)
    {
        Bet = bet ?? throw new ArgumentNullException(nameof(bet));
        GameResult = gameResult;
        PayoutAmount = payoutAmount;
        TotalReturn = totalReturn;
    }

    /// <summary>
    /// Gets the original bet.
    /// </summary>
    public Bet Bet { get; }

    /// <summary>
    /// Gets the game result that determined the payout.
    /// </summary>
    public GameResult GameResult { get; }

    /// <summary>
    /// Gets the payout amount (winnings only, not including the original bet).
    /// </summary>
    public Money PayoutAmount { get; }

    /// <summary>
    /// Gets the total return (original bet + payout).
    /// </summary>
    public Money TotalReturn { get; }

    /// <summary>
    /// Gets the player name from the associated bet.
    /// </summary>
    public string PlayerName => Bet.PlayerName;

    /// <summary>
    /// Gets a value indicating whether this payout represents a win.
    /// </summary>
    public bool IsWin => GameResult == GameResult.Win || GameResult == GameResult.Blackjack;

    /// <summary>
    /// Gets a value indicating whether this payout represents a loss.
    /// </summary>
    public bool IsLoss => GameResult == GameResult.Lose;

    /// <summary>
    /// Gets a value indicating whether this payout represents a push (tie).
    /// </summary>
    public bool IsPush => GameResult == GameResult.Push;

    /// <summary>
    /// Gets a value indicating whether this payout represents a blackjack win.
    /// </summary>
    public bool IsBlackjack => GameResult == GameResult.Blackjack;

    /// <summary>
    /// Returns a string representation of the payout result.
    /// </summary>
    /// <returns>A formatted string showing the payout details.</returns>
    public override string ToString()
    {
        return $"{PlayerName}: {GameResult} - Payout: {PayoutAmount}, Total Return: {TotalReturn}";
    }
}