using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a player in a blackjack game, including both human players and the dealer.
/// </summary>
public class Player
{
    private readonly Hand _hand;
    private Money _bankroll;
    private Bet? _currentBet;
    private PlayerStatistics _statistics;

    /// <summary>
    /// Initializes a new instance of the <see cref="Player"/> class.
    /// </summary>
    /// <param name="name">The name of the player.</param>
    /// <param name="playerType">The type of player (Human or Dealer).</param>
    /// <param name="initialBankroll">The initial bankroll for the player (defaults to zero).</param>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when initialBankroll is negative.</exception>
    public Player(string name, PlayerType playerType = PlayerType.Human, Money? initialBankroll = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name cannot be null or whitespace.", nameof(name));
        }

        var bankroll = initialBankroll ?? Money.Zero;
        if (bankroll.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(initialBankroll), "Initial bankroll cannot be negative.");
        }

        Name = name.Trim();
        Type = playerType;
        _hand = new Hand();
        _bankroll = bankroll;
        _currentBet = null;
        _statistics = new PlayerStatistics(Name);
    }

    /// <summary>
    /// Gets the name of the player.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of player (Human or Dealer).
    /// </summary>
    public PlayerType Type { get; }

    /// <summary>
    /// Gets the player's current hand.
    /// </summary>
    public Hand Hand => _hand;

    /// <summary>
    /// Gets a value indicating whether this player is the dealer.
    /// </summary>
    public bool IsDealer => Type == PlayerType.Dealer;

    /// <summary>
    /// Gets a value indicating whether this player is a human player.
    /// </summary>
    public bool IsHuman => Type == PlayerType.Human;

    /// <summary>
    /// Gets the player's current bankroll.
    /// </summary>
    public Money Bankroll => _bankroll;

    /// <summary>
    /// Gets the player's current bet, if any.
    /// </summary>
    public Bet? CurrentBet => _currentBet;

    /// <summary>
    /// Gets a value indicating whether the player has sufficient funds for a bet.
    /// </summary>
    /// <param name="amount">The amount to check.</param>
    /// <returns>True if the player has sufficient funds, false otherwise.</returns>
    public bool HasSufficientFunds(Money amount) => _bankroll >= amount;

    /// <summary>
    /// Gets a value indicating whether the player has placed a bet.
    /// </summary>
    public bool HasActiveBet => _currentBet?.IsActive == true;

    /// <summary>
    /// Gets the player's statistics.
    /// </summary>
    public PlayerStatistics Statistics => _statistics;

    /// <summary>
    /// Places a bet for the player.
    /// </summary>
    /// <param name="amount">The amount to bet.</param>
    /// <param name="betType">The type of bet (defaults to Standard).</param>
    /// <exception cref="InvalidOperationException">Thrown when player already has an active bet or has insufficient funds.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    public void PlaceBet(Money amount, BetType betType = BetType.Standard)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bet amount must be positive.");

        if (HasActiveBet)
            throw new InvalidOperationException("Player already has an active bet.");

        if (!HasSufficientFunds(amount))
            throw new InvalidOperationException($"Insufficient funds. Required: {amount}, Available: {_bankroll}");

        _currentBet = new Bet(amount, Name, betType);
        _bankroll -= amount;
    }

    /// <summary>
    /// Updates the player's bankroll by adding or subtracting an amount.
    /// </summary>
    /// <param name="amount">The amount to add (positive) or subtract (negative).</param>
    /// <exception cref="InvalidOperationException">Thrown when the operation would result in a negative bankroll.</exception>
    public void UpdateBankroll(Money amount)
    {
        var newBankroll = _bankroll + amount;
        if (newBankroll.IsNegative)
            throw new InvalidOperationException($"Operation would result in negative bankroll. Current: {_bankroll}, Change: {amount}");

        _bankroll = newBankroll;
    }

    /// <summary>
    /// Adds funds to the player's bankroll.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    public void AddFunds(Money amount)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount to add must be positive.");

        _bankroll += amount;
    }

    /// <summary>
    /// Deducts funds from the player's bankroll.
    /// </summary>
    /// <param name="amount">The amount to deduct.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when player has insufficient funds.</exception>
    public void DeductFunds(Money amount)
    {
        if (!amount.IsPositive)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount to deduct must be positive.");

        if (!HasSufficientFunds(amount))
            throw new InvalidOperationException($"Insufficient funds. Required: {amount}, Available: {_bankroll}");

        _bankroll -= amount;
    }

    /// <summary>
    /// Settles the current bet and updates the bankroll based on the game result.
    /// </summary>
    /// <param name="result">The game result.</param>
    /// <param name="blackjackMultiplier">The multiplier for blackjack payouts (typically 1.5 for 3:2 odds).</param>
    /// <returns>The total amount returned to the player (original bet plus payout).</returns>
    /// <exception cref="InvalidOperationException">Thrown when player has no active bet.</exception>
    public Money SettleBet(GameResult result, decimal blackjackMultiplier = 1.5m)
    {
        if (!HasActiveBet || _currentBet == null)
            throw new InvalidOperationException("Player has no active bet to settle.");

        var totalReturn = _currentBet.CalculateTotalReturn(result, blackjackMultiplier);
        var payout = _currentBet.CalculatePayout(result, blackjackMultiplier);
        var betAmount = _currentBet.Amount;
        
        _currentBet.Settle();
        
        if (totalReturn.IsPositive)
        {
            _bankroll += totalReturn;
        }

        // Record the game result in statistics
        _statistics.RecordGame(result, betAmount, payout);

        var settledBet = _currentBet;
        _currentBet = null;
        
        return totalReturn;
    }

    /// <summary>
    /// Clears the current bet without settling it (for cancellation scenarios).
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when player has no active bet.</exception>
    public void ClearBet()
    {
        if (!HasActiveBet || _currentBet == null)
            throw new InvalidOperationException("Player has no active bet to clear.");

        // Return the bet amount to bankroll
        _bankroll += _currentBet.Amount;
        _currentBet = null;
    }

    /// <summary>
    /// Sets the player's bankroll to a specific amount.
    /// </summary>
    /// <param name="amount">The new bankroll amount.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is negative.</exception>
    public void SetBankroll(Money amount)
    {
        if (amount.IsNegative)
            throw new ArgumentOutOfRangeException(nameof(amount), "Bankroll cannot be negative.");

        _bankroll = amount;
    }

    /// <summary>
    /// Resets the player's statistics to their initial values.
    /// </summary>
    public void ResetStatistics()
    {
        _statistics.Reset();
    }

    /// <summary>
    /// Gets the player's win percentage.
    /// </summary>
    /// <returns>The win percentage as a value between 0 and 1.</returns>
    public double GetWinPercentage() => _statistics.WinPercentage;

    /// <summary>
    /// Gets the player's net winnings.
    /// </summary>
    /// <returns>The net winnings amount.</returns>
    public Money GetNetWinnings() => _statistics.NetWinnings;

    /// <summary>
    /// Gets the total number of games played by the player.
    /// </summary>
    /// <returns>The total number of games played.</returns>
    public int GetGamesPlayed() => _statistics.GamesPlayed;

    /// <summary>
    /// Gets the number of blackjacks achieved by the player.
    /// </summary>
    /// <returns>The number of blackjacks achieved.</returns>
    public int GetBlackjacksAchieved() => _statistics.BlackjacksAchieved;

    /// <summary>
    /// Gets the player's return on investment percentage.
    /// </summary>
    /// <returns>The ROI as a decimal value.</returns>
    public double GetReturnOnInvestment() => _statistics.ReturnOnInvestment;

    /// <summary>
    /// Gets a value indicating whether the player is profitable overall.
    /// </summary>
    /// <returns>True if the player has positive net winnings, false otherwise.</returns>
    public bool IsProfitable() => _statistics.IsProfitable;

    /// <summary>
    /// Adds a card to the player's hand.
    /// </summary>
    /// <param name="card">The card to add.</param>
    public void AddCard(Card card)
    {
        _hand.AddCard(card);
    }

    /// <summary>
    /// Clears the player's hand for a new game.
    /// </summary>
    public void ClearHand()
    {
        _hand.Clear();
    }

    /// <summary>
    /// Resets the player for a new round, clearing hand and bet.
    /// </summary>
    public void ResetForNewRound()
    {
        _hand.Clear();
        _currentBet = null;
    }

    /// <summary>
    /// Gets the current value of the player's hand.
    /// </summary>
    /// <returns>The total value of the hand.</returns>
    public int GetHandValue()
    {
        return _hand.GetValue();
    }

    /// <summary>
    /// Determines if the player's hand is busted (over 21).
    /// </summary>
    /// <returns>True if the hand is busted, false otherwise.</returns>
    public bool IsBusted()
    {
        return _hand.IsBusted();
    }

    /// <summary>
    /// Determines if the player has blackjack (21 with exactly 2 cards).
    /// </summary>
    /// <returns>True if the player has blackjack, false otherwise.</returns>
    public bool HasBlackjack()
    {
        return _hand.IsBlackjack();
    }

    /// <summary>
    /// Determines if the player's hand is soft (contains an Ace valued at 11).
    /// </summary>
    /// <returns>True if the hand is soft, false otherwise.</returns>
    public bool HasSoftHand()
    {
        return _hand.IsSoft();
    }

    /// <summary>
    /// Gets the number of cards in the player's hand.
    /// </summary>
    /// <returns>The number of cards in the hand.</returns>
    public int GetCardCount()
    {
        return _hand.CardCount;
    }

    /// <summary>
    /// Returns a string representation of the player showing their name, type, and hand.
    /// </summary>
    /// <returns>A formatted string representation of the player.</returns>
    public override string ToString()
    {
        var typeString = IsDealer ? "Dealer" : "Player";
        var bankrollInfo = IsDealer ? "" : $" (Bankroll: {_bankroll})";
        return $"{typeString} {Name}: {_hand} (Value: {GetHandValue()}){bankrollInfo}";
    }

    /// <summary>
    /// Returns a string representation of the player with their hand hidden (for dealer's hole card).
    /// </summary>
    /// <param name="hideFirstCard">If true, hides the first card in the hand.</param>
    /// <returns>A formatted string with the specified card hidden.</returns>
    public string ToStringWithHiddenCard(bool hideFirstCard = true)
    {
        if (!hideFirstCard || _hand.CardCount == 0)
        {
            return ToString();
        }

        var typeString = IsDealer ? "Dealer" : "Player";
        var cards = _hand.Cards.ToList();
        
        if (cards.Count == 0)
        {
            return $"{typeString} {Name}: Empty hand";
        }

        var visibleCards = hideFirstCard ? cards.Skip(1) : cards.Take(cards.Count - 1);
        var hiddenCardText = hideFirstCard ? "[Hidden]" : "[Hidden]";
        
        if (hideFirstCard)
        {
            var visibleCardsText = visibleCards.Any() ? string.Join(", ", visibleCards) : "";
            var displayText = visibleCardsText.Length > 0 ? $"{hiddenCardText}, {visibleCardsText}" : hiddenCardText;
            return $"{typeString} {Name}: {displayText}";
        }
        else
        {
            var visibleCardsText = visibleCards.Any() ? string.Join(", ", visibleCards) : "";
            var displayText = visibleCardsText.Length > 0 ? $"{visibleCardsText}, {hiddenCardText}" : hiddenCardText;
            return $"{typeString} {Name}: {displayText}";
        }
    }

    /// <summary>
    /// Determines equality based on name and type.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Player other)
            return false;

        return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Type == other.Type;
    }

    /// <summary>
    /// Gets the hash code based on name and type.
    /// </summary>
    /// <returns>The hash code for this player.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name.ToLowerInvariant(), Type);
    }
}