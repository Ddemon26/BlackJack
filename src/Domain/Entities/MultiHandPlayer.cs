using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Entities;

/// <summary>
/// Represents a player that can have multiple hands (for split scenarios).
/// Wraps the basic Player functionality and adds support for multiple hands.
/// </summary>
public class MultiHandPlayer
{
    private readonly List<PlayerHand> _hands = new();
    private int _currentHandIndex = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiHandPlayer"/> class.
    /// </summary>
    /// <param name="player">The underlying player.</param>
    /// <exception cref="ArgumentNullException">Thrown when player is null.</exception>
    public MultiHandPlayer(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        
        // Initialize with the player's current hand if they have one
        if (player.HasActiveBet && player.CurrentBet != null)
        {
            var playerHand = new PlayerHand(player.Hand, player.CurrentBet);
            _hands.Add(playerHand);
        }
    }

    /// <summary>
    /// Gets the underlying player.
    /// </summary>
    public Player Player { get; }

    /// <summary>
    /// Gets the player's name.
    /// </summary>
    public string Name => Player.Name;

    /// <summary>
    /// Gets the player's type.
    /// </summary>
    public PlayerType Type => Player.Type;

    /// <summary>
    /// Gets the player's current bankroll.
    /// </summary>
    public Money Bankroll => Player.Bankroll;

    /// <summary>
    /// Gets all hands belonging to this player.
    /// </summary>
    public IReadOnlyList<PlayerHand> Hands => _hands.AsReadOnly();

    /// <summary>
    /// Gets the currently active hand for play.
    /// </summary>
    public PlayerHand? CurrentHand => _currentHandIndex < _hands.Count ? _hands[_currentHandIndex] : null;

    /// <summary>
    /// Gets the number of hands this player has.
    /// </summary>
    public int HandCount => _hands.Count;

    /// <summary>
    /// Gets a value indicating whether this player has any active hands.
    /// </summary>
    public bool HasActiveHands => _hands.Any(h => h.IsActive);

    /// <summary>
    /// Gets a value indicating whether all hands are complete.
    /// </summary>
    public bool AllHandsComplete => _hands.All(h => h.IsComplete);

    /// <summary>
    /// Gets a value indicating whether any hand is busted.
    /// </summary>
    public bool HasBustedHand => _hands.Any(h => h.IsBusted);

    /// <summary>
    /// Gets a value indicating whether any hand has blackjack.
    /// </summary>
    public bool HasBlackjackHand => _hands.Any(h => h.IsBlackjack);

    /// <summary>
    /// Adds a new hand to this player.
    /// </summary>
    /// <param name="hand">The hand to add.</param>
    /// <param name="bet">The bet associated with the hand.</param>
    /// <exception cref="ArgumentNullException">Thrown when hand or bet is null.</exception>
    public void AddHand(Hand hand, Bet bet)
    {
        if (hand == null) throw new ArgumentNullException(nameof(hand));
        if (bet == null) throw new ArgumentNullException(nameof(bet));

        var playerHand = new PlayerHand(hand, bet);
        _hands.Add(playerHand);
    }

    /// <summary>
    /// Advances to the next active hand.
    /// </summary>
    /// <returns>True if there is a next active hand, false if all hands are complete.</returns>
    public bool AdvanceToNextHand()
    {
        // Mark current hand as inactive if it exists
        if (CurrentHand != null && CurrentHand.IsActive)
        {
            CurrentHand.MarkAsInactive();
        }

        // Find next active hand
        do
        {
            _currentHandIndex++;
        } while (_currentHandIndex < _hands.Count && !_hands[_currentHandIndex].IsActive);

        return _currentHandIndex < _hands.Count;
    }

    /// <summary>
    /// Resets to the first hand for a new turn sequence.
    /// </summary>
    public void ResetToFirstHand()
    {
        _currentHandIndex = 0;
        
        // Reactivate all incomplete hands
        foreach (var hand in _hands.Where(h => !h.IsComplete))
        {
            hand.Reactivate();
        }
    }

    /// <summary>
    /// Splits the current hand into two hands.
    /// </summary>
    /// <param name="splitHandManager">The split hand manager to use.</param>
    /// <returns>The new split hand that was created.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current hand cannot be split.</exception>
    /// <exception cref="ArgumentNullException">Thrown when splitHandManager is null.</exception>
    public PlayerHand SplitCurrentHand(SplitHandManager splitHandManager)
    {
        if (splitHandManager == null) throw new ArgumentNullException(nameof(splitHandManager));
        
        var currentHand = CurrentHand;
        if (currentHand == null)
        {
            throw new InvalidOperationException("No current hand to split.");
        }

        if (!currentHand.CanSplit())
        {
            throw new InvalidOperationException("Current hand cannot be split.");
        }

        if (!splitHandManager.HasSufficientFundsForSplit(Player))
        {
            throw new InvalidOperationException("Insufficient funds to split hand.");
        }

        // Split the hand
        var (firstHand, secondHand) = splitHandManager.SplitHand(currentHand.Hand);
        
        // Create split bet
        var splitBet = splitHandManager.CreateSplitBet(currentHand.Bet);
        
        // Deduct additional funds from player
        Player.DeductFunds(currentHand.Bet.Amount);
        
        // Replace current hand with first split hand
        var currentIndex = _hands.IndexOf(currentHand);
        _hands[currentIndex] = new PlayerHand(firstHand, currentHand.Bet);
        
        // Add second split hand
        var newPlayerHand = new PlayerHand(secondHand, splitBet);
        _hands.Insert(currentIndex + 1, newPlayerHand);
        
        return newPlayerHand;
    }

    /// <summary>
    /// Doubles down on the current hand.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the current hand cannot be doubled down.</exception>
    public void DoubleDownCurrentHand()
    {
        var currentHand = CurrentHand;
        if (currentHand == null)
        {
            throw new InvalidOperationException("No current hand to double down.");
        }

        if (!currentHand.CanDoubleDown())
        {
            throw new InvalidOperationException("Current hand cannot be doubled down.");
        }

        var originalBet = currentHand.Bet;
        if (!Player.HasSufficientFunds(originalBet.Amount))
        {
            throw new InvalidOperationException("Insufficient funds to double down.");
        }

        // Deduct additional funds
        Player.DeductFunds(originalBet.Amount);
        
        // Create double down bet (this would need to be handled differently in a real implementation)
        // For now, we'll just mark the hand as having a double down bet
        // The actual bet doubling would be handled at the Player level
    }

    /// <summary>
    /// Adds a card to the current hand.
    /// </summary>
    /// <param name="card">The card to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no current hand or it cannot receive cards.</exception>
    public void AddCardToCurrentHand(Card card)
    {
        var currentHand = CurrentHand;
        if (currentHand == null)
        {
            throw new InvalidOperationException("No current hand to add card to.");
        }

        currentHand.AddCard(card);
    }

    /// <summary>
    /// Marks the current hand as complete.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when there is no current hand.</exception>
    public void CompleteCurrentHand()
    {
        var currentHand = CurrentHand;
        if (currentHand == null)
        {
            throw new InvalidOperationException("No current hand to complete.");
        }

        currentHand.MarkAsComplete();
    }

    /// <summary>
    /// Gets the total amount bet across all hands.
    /// </summary>
    /// <returns>The total bet amount.</returns>
    public Money GetTotalBetAmount()
    {
        return _hands.Aggregate(Money.Zero, (total, hand) => total + hand.Bet.Amount);
    }

    /// <summary>
    /// Clears all hands and resets the player for a new round.
    /// </summary>
    public void ClearAllHands()
    {
        _hands.Clear();
        _currentHandIndex = 0;
        Player.ResetForNewRound();
    }

    /// <summary>
    /// Returns a string representation of this multi-hand player.
    /// </summary>
    /// <returns>A formatted string showing the player and all their hands.</returns>
    public override string ToString()
    {
        if (_hands.Count == 0)
        {
            return $"{Name}: No hands";
        }

        if (_hands.Count == 1)
        {
            return $"{Name}: {_hands[0]}";
        }

        var handsText = string.Join(", ", _hands.Select((h, i) => $"Hand {i + 1}: {h}"));
        return $"{Name}: {handsText}";
    }
}