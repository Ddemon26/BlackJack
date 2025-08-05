using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Models;

/// <summary>
/// Represents the current betting state for a blackjack game round, tracking player bets and betting phase.
/// </summary>
/// <remarks>
/// This model maintains the state of betting during a game round, including which players have placed bets,
/// their current bankrolls, and the current phase of the betting process. It provides methods for managing
/// the betting workflow and validating betting operations.
/// </remarks>
public class BettingState
{
    private readonly Dictionary<string, Bet> _playerBets;
    private readonly Dictionary<string, Money> _playerBankrolls;
    private readonly List<string> _playerOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="BettingState"/> class.
    /// </summary>
    /// <param name="playerNames">The names of players participating in the betting round.</param>
    /// <param name="playerBankrolls">The current bankrolls for each player.</param>
    /// <exception cref="ArgumentNullException">Thrown when playerNames or playerBankrolls is null.</exception>
    /// <exception cref="ArgumentException">Thrown when playerNames is empty or contains invalid names.</exception>
    public BettingState(IEnumerable<string> playerNames, IReadOnlyDictionary<string, Money> playerBankrolls)
    {
        if (playerNames == null)
            throw new ArgumentNullException(nameof(playerNames));

        if (playerBankrolls == null)
            throw new ArgumentNullException(nameof(playerBankrolls));

        var playerNamesList = playerNames.ToList();
        if (!playerNamesList.Any())
            throw new ArgumentException("At least one player name must be provided.", nameof(playerNames));

        if (playerNamesList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Player names cannot be null or empty.", nameof(playerNames));

        // Validate that all players have bankroll entries
        foreach (var playerName in playerNamesList)
        {
            if (!playerBankrolls.ContainsKey(playerName))
                throw new ArgumentException($"Bankroll not provided for player '{playerName}'.", nameof(playerBankrolls));
        }

        _playerBets = new Dictionary<string, Bet>(StringComparer.OrdinalIgnoreCase);
        _playerBankrolls = new Dictionary<string, Money>(playerBankrolls, StringComparer.OrdinalIgnoreCase);
        _playerOrder = playerNamesList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        CurrentPhase = BettingPhase.WaitingForBets;
        CurrentBettingPlayerIndex = 0;
        RoundStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current betting phase.
    /// </summary>
    public BettingPhase CurrentPhase { get; private set; }

    /// <summary>
    /// Gets the index of the current player in the betting order.
    /// </summary>
    public int CurrentBettingPlayerIndex { get; private set; }

    /// <summary>
    /// Gets the name of the current player whose turn it is to bet.
    /// </summary>
    public string? CurrentBettingPlayer
    {
        get
        {
            if (CurrentPhase != BettingPhase.WaitingForBets || 
                CurrentBettingPlayerIndex >= _playerOrder.Count)
                return null;

            return _playerOrder[CurrentBettingPlayerIndex];
        }
    }

    /// <summary>
    /// Gets the time when the betting round started.
    /// </summary>
    public DateTime RoundStartTime { get; }

    /// <summary>
    /// Gets the elapsed time since the betting round started.
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - RoundStartTime;

    /// <summary>
    /// Gets a value indicating whether the betting round is complete.
    /// </summary>
    public bool IsComplete => CurrentPhase == BettingPhase.Complete;

    /// <summary>
    /// Gets the player bets placed in this round.
    /// </summary>
    public IReadOnlyDictionary<string, Bet> PlayerBets => _playerBets.AsReadOnly();

    /// <summary>
    /// Gets the current player bankrolls.
    /// </summary>
    public IReadOnlyDictionary<string, Money> PlayerBankrolls => _playerBankrolls.AsReadOnly();

    /// <summary>
    /// Gets the order in which players place their bets.
    /// </summary>
    public IReadOnlyList<string> PlayerOrder => _playerOrder.AsReadOnly();

    /// <summary>
    /// Gets the total amount wagered by all players in this round.
    /// </summary>
    public Money TotalWagered
    {
        get
        {
            if (!_playerBets.Any())
                return Money.Zero;

            return _playerBets.Values
                .Select(bet => bet.Amount)
                .Aggregate(Money.Zero, (sum, amount) => sum + amount);
        }
    }

    /// <summary>
    /// Gets the number of players who have placed bets.
    /// </summary>
    public int PlayersWithBets => _playerBets.Count;

    /// <summary>
    /// Gets the number of players still waiting to place bets.
    /// </summary>
    public int PlayersWaitingToBet => _playerOrder.Count - _playerBets.Count;

    /// <summary>
    /// Places a bet for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="amount">The amount to bet.</param>
    /// <param name="betType">The type of bet (defaults to Standard).</param>
    /// <returns>True if the bet was successfully placed, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when betting is not allowed in the current phase.</exception>
    public bool PlaceBet(string playerName, Money amount, BetType betType = BetType.Standard)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bet amount must be positive.");

        if (CurrentPhase != BettingPhase.WaitingForBets)
            throw new InvalidOperationException($"Cannot place bets during {CurrentPhase} phase.");

        if (!_playerOrder.Contains(playerName, StringComparer.OrdinalIgnoreCase))
            return false;

        if (_playerBets.ContainsKey(playerName))
            return false; // Player has already bet

        if (!_playerBankrolls.TryGetValue(playerName, out var bankroll) || bankroll < amount)
            return false; // Insufficient funds

        // Place the bet
        var bet = new Bet(amount, playerName, betType);
        _playerBets[playerName] = bet;
        _playerBankrolls[playerName] = bankroll - amount;

        // Advance to next player or complete betting
        AdvanceBettingTurn();

        return true;
    }

    /// <summary>
    /// Checks if a player can place a bet of the specified amount.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="amount">The amount to check.</param>
    /// <returns>True if the player can place the bet, false otherwise.</returns>
    public bool CanPlayerBet(string playerName, Money amount)
    {
        if (string.IsNullOrWhiteSpace(playerName) || !amount.IsPositive)
            return false;

        if (CurrentPhase != BettingPhase.WaitingForBets)
            return false;

        if (!_playerOrder.Contains(playerName, StringComparer.OrdinalIgnoreCase))
            return false;

        if (_playerBets.ContainsKey(playerName))
            return false; // Player has already bet

        if (!_playerBankrolls.TryGetValue(playerName, out var bankroll))
            return false;

        return bankroll >= amount;
    }

    /// <summary>
    /// Gets the bet placed by a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The player's bet, or null if no bet was placed.</returns>
    public Bet? GetPlayerBet(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        return _playerBets.TryGetValue(playerName, out var bet) ? bet : null;
    }

    /// <summary>
    /// Gets the current bankroll for a specific player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The player's current bankroll, or null if the player doesn't exist.</returns>
    public Money? GetPlayerBankroll(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return null;

        return _playerBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : null;
    }

    /// <summary>
    /// Checks if a specific player has placed a bet.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>True if the player has placed a bet, false otherwise.</returns>
    public bool HasPlayerBet(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return false;

        return _playerBets.ContainsKey(playerName);
    }

    /// <summary>
    /// Gets the players who have not yet placed bets.
    /// </summary>
    /// <returns>A collection of player names who haven't bet yet.</returns>
    public IEnumerable<string> GetPlayersWaitingToBet()
    {
        return _playerOrder.Where(playerName => !_playerBets.ContainsKey(playerName));
    }

    /// <summary>
    /// Gets the players who have placed bets.
    /// </summary>
    /// <returns>A collection of player names who have bet.</returns>
    public IEnumerable<string> GetPlayersWithBets()
    {
        return _playerBets.Keys;
    }

    /// <summary>
    /// Skips the current player's turn (for timeout or other reasons).
    /// </summary>
    /// <returns>True if a player's turn was skipped, false if no one was waiting to bet.</returns>
    /// <exception cref="InvalidOperationException">Thrown when not in the waiting for bets phase.</exception>
    public bool SkipCurrentPlayer()
    {
        if (CurrentPhase != BettingPhase.WaitingForBets)
            throw new InvalidOperationException($"Cannot skip player during {CurrentPhase} phase.");

        if (CurrentBettingPlayer == null)
            return false;

        AdvanceBettingTurn();
        return true;
    }

    /// <summary>
    /// Forces completion of the betting round, even if not all players have bet.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when betting is already complete.</exception>
    public void ForceComplete()
    {
        if (CurrentPhase == BettingPhase.Complete)
            throw new InvalidOperationException("Betting round is already complete.");

        CurrentPhase = BettingPhase.Complete;
        CurrentBettingPlayerIndex = _playerOrder.Count;
    }

    /// <summary>
    /// Resets the betting state for a new round with the same players.
    /// </summary>
    /// <param name="updatedBankrolls">The updated bankrolls for each player.</param>
    /// <exception cref="ArgumentNullException">Thrown when updatedBankrolls is null.</exception>
    public void Reset(IReadOnlyDictionary<string, Money> updatedBankrolls)
    {
        if (updatedBankrolls == null)
            throw new ArgumentNullException(nameof(updatedBankrolls));

        _playerBets.Clear();
        _playerBankrolls.Clear();

        foreach (var (playerName, bankroll) in updatedBankrolls)
        {
            if (_playerOrder.Contains(playerName, StringComparer.OrdinalIgnoreCase))
            {
                _playerBankrolls[playerName] = bankroll;
            }
        }

        CurrentPhase = BettingPhase.WaitingForBets;
        CurrentBettingPlayerIndex = 0;
    }

    /// <summary>
    /// Advances the betting turn to the next player or completes betting if all players have bet.
    /// </summary>
    private void AdvanceBettingTurn()
    {
        // Find the next player who hasn't bet yet
        do
        {
            CurrentBettingPlayerIndex++;
        }
        while (CurrentBettingPlayerIndex < _playerOrder.Count && 
               _playerBets.ContainsKey(_playerOrder[CurrentBettingPlayerIndex]));

        // If we've gone through all players, betting is complete
        if (CurrentBettingPlayerIndex >= _playerOrder.Count)
        {
            CurrentPhase = BettingPhase.Complete;
        }
    }

    /// <summary>
    /// Returns a string representation of the betting state.
    /// </summary>
    /// <returns>A formatted string showing the current betting status.</returns>
    public override string ToString()
    {
        var totalPlayers = _playerOrder.Count;
        var playersWithBets = _playerBets.Count;
        var currentPlayer = CurrentBettingPlayer ?? "None";
        
        return $"BettingState: {CurrentPhase}, {playersWithBets}/{totalPlayers} players bet, " +
               $"Current: {currentPlayer}, Total: {TotalWagered}";
    }

    /// <summary>
    /// Returns a detailed string representation of the betting state.
    /// </summary>
    /// <returns>A comprehensive formatted string showing all betting details.</returns>
    public string ToDetailedString()
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"Betting State: {CurrentPhase}");
        sb.AppendLine($"Round Started: {RoundStartTime:HH:mm:ss}");
        sb.AppendLine($"Elapsed Time: {ElapsedTime.TotalSeconds:F1}s");
        sb.AppendLine($"Current Player: {CurrentBettingPlayer ?? "None"}");
        sb.AppendLine($"Total Wagered: {TotalWagered}");
        sb.AppendLine();

        sb.AppendLine("Player Status:");
        foreach (var playerName in _playerOrder)
        {
            var bankroll = _playerBankrolls.TryGetValue(playerName, out var b) ? b : Money.Zero;
            var bet = _playerBets.TryGetValue(playerName, out var playerBet) ? playerBet.Amount : Money.Zero;
            var status = _playerBets.ContainsKey(playerName) ? "Bet" : "Waiting";
            
            sb.AppendLine($"  {playerName}: {status}, Bankroll: {bankroll}, Bet: {bet}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines equality based on current state and player composition.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not BettingState other)
            return false;

        return CurrentPhase == other.CurrentPhase &&
               CurrentBettingPlayerIndex == other.CurrentBettingPlayerIndex &&
               _playerOrder.SequenceEqual(other._playerOrder, StringComparer.OrdinalIgnoreCase) &&
               _playerBets.Count == other._playerBets.Count &&
               _playerBankrolls.Count == other._playerBankrolls.Count;
    }

    /// <summary>
    /// Gets the hash code based on current state.
    /// </summary>
    /// <returns>The hash code for this betting state.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            CurrentPhase,
            CurrentBettingPlayerIndex,
            _playerOrder.Count,
            _playerBets.Count,
            _playerBankrolls.Count
        );
    }
}

/// <summary>
/// Represents the different phases of a betting round.
/// </summary>
public enum BettingPhase
{
    /// <summary>
    /// Waiting for players to place their bets.
    /// </summary>
    WaitingForBets,

    /// <summary>
    /// All players have placed their bets and the betting round is complete.
    /// </summary>
    Complete
}