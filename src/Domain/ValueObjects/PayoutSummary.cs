using System.Collections.Generic;
using System.Linq;

namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents a summary of payouts for multiple players.
/// Contains aggregated information about all payouts processed in a round.
/// </summary>
public class PayoutSummary
{
    private readonly List<PayoutResult> _payouts;

    /// <summary>
    /// Initializes a new instance of the PayoutSummary class.
    /// </summary>
    /// <param name="payouts">The collection of payout results.</param>
    public PayoutSummary(IEnumerable<PayoutResult> payouts)
    {
        _payouts = payouts?.ToList() ?? new List<PayoutResult>();
    }

    /// <summary>
    /// Gets the collection of payout results.
    /// </summary>
    public IReadOnlyList<PayoutResult> Payouts => _payouts.AsReadOnly();

    /// <summary>
    /// Gets the total number of payouts processed.
    /// </summary>
    public int TotalPayouts => _payouts.Count;

    /// <summary>
    /// Gets the total amount paid out to all players.
    /// </summary>
    public Money TotalPayoutAmount
    {
        get
        {
            if (_payouts.Count == 0) return Money.Zero;
            
            var currency = _payouts.First().PayoutAmount.Currency;
            var totalAmount = _payouts.Sum(p => p.PayoutAmount.Amount);
            return new Money(totalAmount, currency);
        }
    }

    /// <summary>
    /// Gets the total amount returned to all players (including original bets).
    /// </summary>
    public Money TotalReturnAmount
    {
        get
        {
            if (_payouts.Count == 0) return Money.Zero;
            
            var currency = _payouts.First().TotalReturn.Currency;
            var totalAmount = _payouts.Sum(p => p.TotalReturn.Amount);
            return new Money(totalAmount, currency);
        }
    }

    /// <summary>
    /// Gets the number of winning payouts.
    /// </summary>
    public int WinCount => _payouts.Count(p => p.IsWin);

    /// <summary>
    /// Gets the number of losing payouts.
    /// </summary>
    public int LossCount => _payouts.Count(p => p.IsLoss);

    /// <summary>
    /// Gets the number of push payouts.
    /// </summary>
    public int PushCount => _payouts.Count(p => p.IsPush);

    /// <summary>
    /// Gets the number of blackjack payouts.
    /// </summary>
    public int BlackjackCount => _payouts.Count(p => p.IsBlackjack);

    /// <summary>
    /// Gets the payout result for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The payout result for the player, or null if not found.</returns>
    public PayoutResult? GetPayoutForPlayer(string playerName)
    {
        return _payouts.FirstOrDefault(p => 
            string.Equals(p.PlayerName, playerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all winning payouts.
    /// </summary>
    /// <returns>A collection of winning payout results.</returns>
    public IEnumerable<PayoutResult> GetWinningPayouts()
    {
        return _payouts.Where(p => p.IsWin);
    }

    /// <summary>
    /// Gets all losing payouts.
    /// </summary>
    /// <returns>A collection of losing payout results.</returns>
    public IEnumerable<PayoutResult> GetLosingPayouts()
    {
        return _payouts.Where(p => p.IsLoss);
    }

    /// <summary>
    /// Gets all push payouts.
    /// </summary>
    /// <returns>A collection of push payout results.</returns>
    public IEnumerable<PayoutResult> GetPushPayouts()
    {
        return _payouts.Where(p => p.IsPush);
    }

    /// <summary>
    /// Gets all blackjack payouts.
    /// </summary>
    /// <returns>A collection of blackjack payout results.</returns>
    public IEnumerable<PayoutResult> GetBlackjackPayouts()
    {
        return _payouts.Where(p => p.IsBlackjack);
    }

    /// <summary>
    /// Returns a string representation of the payout summary.
    /// </summary>
    /// <returns>A formatted string showing the summary statistics.</returns>
    public override string ToString()
    {
        return $"Payouts: {TotalPayouts}, Wins: {WinCount}, Losses: {LossCount}, Pushes: {PushCount}, Blackjacks: {BlackjackCount}, Total Paid: {TotalPayoutAmount}";
    }
}