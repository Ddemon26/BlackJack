using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.Services;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.ObjectPooling;

namespace GroupProject.Application.Services;

/// <summary>
/// Implements core game logic and state management for blackjack.
/// </summary>
public class GameService : IGameService
{
    private readonly IShoe _shoe;
    private readonly IGameRules _gameRules;
    private readonly SplitHandManager _splitHandManager;
    private readonly IBettingService _bettingService;
    private readonly List<Player> _players = new();
    private readonly Dictionary<string, List<PlayerHand>> _playerHands = new();
    private readonly Dictionary<string, int> _currentHandIndex = new();
    private Player? _dealer;
    private GamePhase _currentPhase = GamePhase.Setup;
    private int _currentPlayerIndex = 0;
    private DateTime _gameStartTime;
    private BettingState? _currentBettingState;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameService"/> class.
    /// </summary>
    /// <param name="shoe">The shoe containing cards for the game.</param>
    /// <param name="gameRules">The game rules implementation.</param>
    /// <param name="bettingService">The betting service for handling bets and bankrolls.</param>
    /// <param name="splitHandManager">The split hand manager for handling split operations.</param>
    public GameService(IShoe shoe, IGameRules gameRules, IBettingService bettingService, SplitHandManager? splitHandManager = null)
    {
        _shoe = shoe ?? throw new ArgumentNullException(nameof(shoe));
        _gameRules = gameRules ?? throw new ArgumentNullException(nameof(gameRules));
        _bettingService = bettingService ?? throw new ArgumentNullException(nameof(bettingService));
        _splitHandManager = splitHandManager ?? new SplitHandManager();
    }

    /// <inheritdoc />
    public bool IsGameComplete => _currentPhase == GamePhase.GameOver;

    /// <inheritdoc />
    public bool IsGameInProgress => _currentPhase != GamePhase.Setup && _currentPhase != GamePhase.GameOver;

    /// <inheritdoc />
    public void StartNewGame(IEnumerable<string> playerNames)
    {
        if (IsGameInProgress)
        {
            throw new InvalidOperationException("A game is already in progress. Complete the current game before starting a new one.");
        }

        // Use pooled list for better performance
        var names = ListPool<string>.Get();
        try
        {
            names.AddRange(playerNames ?? throw new ArgumentNullException(nameof(playerNames)));
            
            if (!names.Any())
            {
                throw new ArgumentException("At least one player name must be provided.", nameof(playerNames));
            }

            if (names.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Player names cannot be null or whitespace.", nameof(playerNames));
            }

            // Check for duplicate names (case-insensitive) using pooled collections
            var seenNames = ListPool<string>.Get();
            try
            {
                foreach (var name in names)
                {
                    var normalizedName = name.Trim().ToLowerInvariant();
                    if (seenNames.Contains(normalizedName))
                    {
                        throw new ArgumentException($"Duplicate player names are not allowed: {name}", nameof(playerNames));
                    }
                    seenNames.Add(normalizedName);
                }
            }
            finally
            {
                ListPool<string>.Return(seenNames);
            }

            // Clear previous game state
            _players.Clear();
            _dealer = null;
            _currentPlayerIndex = 0;
            _gameStartTime = DateTime.UtcNow;
            ClearAllPlayerHands();

            // Create players
            foreach (var name in names)
            {
                _players.Add(new Player(name.Trim(), PlayerType.Human));
            }
        }
        finally
        {
            ListPool<string>.Return(names);
        }

        // Create dealer
        _dealer = new Player("Dealer", PlayerType.Dealer);

        // Shuffle the shoe
        _shoe.Shuffle();

        _currentPhase = GamePhase.Betting;
        
        // Initialize betting state with zero bankrolls - will be updated with actual bankrolls when needed
        var playerBankrolls = new Dictionary<string, Money>();
        foreach (var player in _players)
        {
            playerBankrolls[player.Name] = Money.Zero;
        }
        _currentBettingState = new BettingState(_players.Select(p => p.Name), playerBankrolls);
    }

    /// <summary>
    /// Processes the betting round by collecting bets from all players.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no game is in progress or betting phase is not active.</exception>
    public async Task<BettingResult> ProcessBettingRoundAsync()
    {
        if (_currentPhase != GamePhase.Betting)
        {
            return BettingResult.Failure("Cannot process betting round. Game must be in Betting phase.");
        }

        if (_players.Count == 0)
        {
            return BettingResult.Failure("No players in the current game.");
        }

        try
        {
            // Initialize betting state with current bankrolls if not already done
            if (_currentBettingState == null)
            {
                var playerBankrolls = new Dictionary<string, Money>();
                foreach (var player in _players)
                {
                    var bankroll = await _bettingService.GetPlayerBankrollAsync(player.Name);
                    playerBankrolls[player.Name] = bankroll;
                }
                _currentBettingState = new BettingState(_players.Select(p => p.Name), playerBankrolls);
            }

            // Validate that all players have sufficient funds for minimum bet
            var minimumBet = _bettingService.MinimumBet;
            var playersWithInsufficientFunds = new List<string>();

            foreach (var player in _players)
            {
                var bankroll = await _bettingService.GetPlayerBankrollAsync(player.Name);
                if (bankroll < minimumBet)
                {
                    playersWithInsufficientFunds.Add(player.Name);
                }
            }

            if (playersWithInsufficientFunds.Any())
            {
                var playerList = string.Join(", ", playersWithInsufficientFunds);
                return BettingResult.Failure($"Players with insufficient funds: {playerList}. Minimum bet: {minimumBet}");
            }

            // All players have sufficient funds, advance to InitialDeal phase
            _currentPhase = GamePhase.InitialDeal;

            return BettingResult.Success("Betting round validation completed successfully. Ready to deal cards.");
        }
        catch (Exception ex)
        {
            return BettingResult.Failure($"Error processing betting round: {ex.Message}");
        }
    }

    /// <summary>
    /// Places a bet for a specific player during the betting round.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="amount">The amount to bet.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the betting result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when betting is not allowed.</exception>
    public async Task<BettingResult> PlacePlayerBetAsync(string playerName, Money amount)
    {
        if (_currentPhase != GamePhase.Betting)
        {
            return BettingResult.Failure("Cannot place bet. Game must be in Betting phase.");
        }

        if (_currentBettingState?.CurrentPhase != BettingPhase.WaitingForBets)
        {
            return BettingResult.Failure("Betting round is not accepting bets.");
        }

        var player = GetPlayer(playerName);
        if (player == null)
        {
            return BettingResult.Failure($"Player '{playerName}' not found in the current game.");
        }

        // Check if player already has a bet
        if (_currentBettingState?.HasPlayerBet(playerName) == true)
        {
            return BettingResult.Failure($"Player {playerName} has already placed a bet this round.");
        }

        try
        {
            // Initialize betting state with actual bankrolls if needed (check if all players have zero bankrolls)
            if (_currentBettingState != null && _players.All(p => _currentBettingState.GetPlayerBankroll(p.Name) == Money.Zero))
            {
                var playerBankrolls = new Dictionary<string, Money>();
                foreach (var p in _players)
                {
                    var bankroll = await _bettingService.GetPlayerBankrollAsync(p.Name);
                    playerBankrolls[p.Name] = bankroll;
                }
                _currentBettingState = new BettingState(_players.Select(p => p.Name), playerBankrolls);
            }

            // Validate bet through betting service first
            var validationResult = await _bettingService.ValidateBetAsync(playerName, amount);
            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            // Update betting state first (before calling betting service)
            bool success;
            try
            {
                success = _currentBettingState?.PlaceBet(playerName, amount, BetType.Standard) ?? false;
            }
            catch (Exception ex)
            {
                return BettingResult.Failure($"Failed to update betting state for {playerName}: {ex.Message}");
            }
            
            if (!success)
            {
                return BettingResult.Failure($"Failed to update betting state for {playerName}.");
            }

            // Place the bet through the betting service
            var bettingResult = await _bettingService.PlaceBetAsync(playerName, amount);
            
            if (bettingResult.IsFailure)
            {
                // If betting service fails, we need to revert the betting state
                // For now, we'll just return the failure - in a real implementation we'd need proper rollback
                return bettingResult;
            }

            // Update the player's bet in the domain entity
            if (bettingResult.Bet != null)
            {
                player.PlaceBet(amount, BetType.Standard);
            }

            return bettingResult;
        }
        catch (Exception ex)
        {
            return BettingResult.Failure($"Error placing bet for {playerName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current betting state.
    /// </summary>
    /// <returns>The current betting state, or null if no betting round is active.</returns>
    public BettingState? GetCurrentBettingState()
    {
        return _currentBettingState;
    }

    /// <summary>
    /// Determines if all players have placed their bets.
    /// </summary>
    /// <returns>True if all players have placed bets, false otherwise.</returns>
    public bool AllPlayersHaveBets()
    {
        if (_currentBettingState == null || _players.Count == 0)
        {
            return false;
        }

        return _players.All(player => _currentBettingState.HasPlayerBet(player.Name));
    }

    /// <inheritdoc />
    public void DealInitialCards()
    {
        if (_currentPhase != GamePhase.InitialDeal)
        {
            throw new InvalidOperationException("Cannot deal initial cards. Game must be in InitialDeal phase.");
        }

        if (_shoe.RemainingCards < (_players.Count + 1) * 2)
        {
            throw new InvalidOperationException("Not enough cards in the shoe to deal initial cards.");
        }

        // Clear all hands
        foreach (var player in _players)
        {
            player.ClearHand();
        }
        _dealer!.ClearHand();

        // Deal first card to each player, then dealer
        foreach (var player in _players)
        {
            player.AddCard(_shoe.Draw());
        }
        _dealer.AddCard(_shoe.Draw());

        // Deal second card to each player, then dealer
        foreach (var player in _players)
        {
            player.AddCard(_shoe.Draw());
        }
        _dealer.AddCard(_shoe.Draw());

        // Initialize player hands tracking
        foreach (var player in _players)
        {
            InitializePlayerHands(player.Name);
        }

        // Move to player turns phase and find first active player
        _currentPlayerIndex = -1; // Start before first player
        _currentPhase = GamePhase.PlayerTurns;
        AdvanceToNextActivePlayer(); // This will set to first active player
    }

    /// <inheritdoc />
    public PlayerActionResult ProcessPlayerAction(string playerName, PlayerAction action)
    {
        if (!IsGameInProgress)
        {
            return PlayerActionResult.Failure("No game is currently in progress.");
        }

        if (_currentPhase != GamePhase.PlayerTurns)
        {
            return PlayerActionResult.Failure("It is not currently a player's turn.");
        }

        var player = GetPlayer(playerName);
        if (player == null)
        {
            return PlayerActionResult.Failure($"Player '{playerName}' not found in the current game.");
        }

        if (!IsPlayerTurn(playerName))
        {
            return PlayerActionResult.Failure($"It is not {playerName}'s turn.");
        }

        // Check if player already has blackjack or is busted
        if (player.HasBlackjack() || player.IsBusted())
        {
            return PlayerActionResult.Failure("Player cannot take actions with blackjack or busted hand.");
        }

        // Validate the action
        if (!_gameRules.IsValidPlayerAction(action, player.Hand))
        {
            return PlayerActionResult.Failure($"Action '{action}' is not valid for the current hand.");
        }

        // Process the action
        switch (action)
        {
            case PlayerAction.Hit:
                return ProcessHitAction(player);
            
            case PlayerAction.Stand:
                return ProcessStandAction(player);
            
            case PlayerAction.DoubleDown:
                return ProcessDoubleDownAction(player);
            
            case PlayerAction.Split:
                return ProcessSplitAction(player);
            
            default:
                return PlayerActionResult.Failure($"Unknown action: {action}");
        }
    }

    /// <inheritdoc />
    public void PlayDealerTurn()
    {
        if (_currentPhase != GamePhase.DealerTurn)
        {
            throw new InvalidOperationException("Cannot play dealer turn. Game must be in DealerTurn phase.");
        }

        if (_dealer == null)
        {
            throw new InvalidOperationException("No dealer found in the current game.");
        }

        // Dealer plays according to standard rules: hit on 16, stand on 17
        while (_gameRules.ShouldDealerHit(_dealer.GetHandValue()) && !_dealer.IsBusted())
        {
            if (_shoe.IsEmpty)
            {
                throw new InvalidOperationException("Shoe is empty, cannot continue dealer turn.");
            }

            _dealer.AddCard(_shoe.Draw());
        }

        _currentPhase = GamePhase.Results;
    }

    /// <inheritdoc />
    public GameSummary GetGameResults()
    {
        if (_currentPhase != GamePhase.Results && _currentPhase != GamePhase.GameOver)
        {
            throw new InvalidOperationException("Game results are not available. Game must be complete.");
        }

        if (_dealer == null)
        {
            throw new InvalidOperationException("No dealer found in the current game.");
        }

        var results = new Dictionary<string, GameResult>();

        foreach (var player in _players)
        {
            var result = _gameRules.DetermineResult(player.Hand, _dealer.Hand);
            results[player.Name] = result;
        }

        _currentPhase = GamePhase.GameOver;

        return new GameSummary(results, _dealer.Hand, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public GameState? GetCurrentGameState()
    {
        if (!IsGameInProgress && _currentPhase != GamePhase.GameOver)
        {
            return null;
        }

        var currentPlayerName = _currentPhase == GamePhase.PlayerTurns && _currentPlayerIndex < _players.Count
            ? _players[_currentPlayerIndex].Name
            : null;

        return new GameState(_players.AsReadOnly(), _dealer!, _currentPhase, currentPlayerName);
    }

    /// <inheritdoc />
    public IReadOnlyList<Player> GetPlayers()
    {
        return _players.AsReadOnly();
    }

    /// <inheritdoc />
    public Player? GetDealer()
    {
        return _dealer;
    }

    /// <inheritdoc />
    public Player? GetPlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return null;
        }

        return _players.FirstOrDefault(p => p.Name.Equals(playerName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public bool IsPlayerTurn(string playerName)
    {
        if (_currentPhase != GamePhase.PlayerTurns || _currentPlayerIndex >= _players.Count || _currentPlayerIndex < 0)
        {
            return false;
        }

        var currentPlayer = _players[_currentPlayerIndex];
        return currentPlayer.Name.Equals(playerName?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private PlayerActionResult ProcessHitAction(Player player)
    {
        if (_shoe.IsEmpty)
        {
            return PlayerActionResult.Failure("No more cards available in the shoe.");
        }

        player.AddCard(_shoe.Draw());

        // Check if player busted or got 21
        if (player.IsBusted())
        {
            AdvanceToNextActivePlayer();
            return PlayerActionResult.SuccessEndTurn(player.Hand);
        }

        if (player.GetHandValue() == 21)
        {
            AdvanceToNextActivePlayer();
            return PlayerActionResult.SuccessEndTurn(player.Hand);
        }

        // Player can continue hitting
        return PlayerActionResult.Success(player.Hand, shouldContinueTurn: true);
    }

    private PlayerActionResult ProcessStandAction(Player player)
    {
        AdvanceToNextActivePlayer();
        return PlayerActionResult.SuccessEndTurn(player.Hand);
    }

    private PlayerActionResult ProcessDoubleDownAction(Player player)
    {
        // Validate that player can double down
        if (!CanPlayerDoubleDown(player))
        {
            return PlayerActionResult.Failure("Cannot double down. Player must have exactly 2 cards and sufficient funds.");
        }

        if (_shoe.IsEmpty)
        {
            return PlayerActionResult.Failure("No more cards available in the shoe.");
        }

        // Double the bet
        if (!player.HasActiveBet || player.CurrentBet == null)
        {
            return PlayerActionResult.Failure("Player must have an active bet to double down.");
        }

        var originalBet = player.CurrentBet;
        try
        {
            // Create double down bet (this will validate sufficient funds)
            var doubleDownBet = originalBet.CreateDoubleDownBet();
            
            // Deduct additional bet amount from bankroll
            player.DeductFunds(originalBet.Amount);
            
            // Clear the original bet and place the double down bet
            player.ClearBet();
            player.PlaceBet(doubleDownBet.Amount, BetType.DoubleDown);
        }
        catch (InvalidOperationException ex)
        {
            return PlayerActionResult.Failure($"Cannot double down: {ex.Message}");
        }

        // Deal exactly one card
        player.AddCard(_shoe.Draw());

        // Player's turn ends after double down regardless of hand value
        AdvanceToNextActivePlayer();
        return PlayerActionResult.SuccessEndTurn(player.Hand, isDoubleDown: true);
    }

    /// <inheritdoc />
    public async Task<PlayerActionResult> ProcessDoubleDownAsync(string playerName)
    {
        return await Task.FromResult(ProcessPlayerAction(playerName, PlayerAction.DoubleDown));
    }

    /// <inheritdoc />
    public async Task<bool> CanPlayerDoubleDownAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return await Task.FromResult(false);
        }

        var player = GetPlayer(playerName);
        if (player == null)
        {
            return await Task.FromResult(false);
        }

        return await Task.FromResult(CanPlayerDoubleDown(player));
    }

    /// <inheritdoc />
    public async Task<PlayerActionResult> ProcessSplitAsync(string playerName)
    {
        return await Task.FromResult(ProcessPlayerAction(playerName, PlayerAction.Split));
    }

    /// <inheritdoc />
    public async Task<bool> CanPlayerSplitAsync(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return await Task.FromResult(false);
        }

        var player = GetPlayer(playerName);
        if (player == null)
        {
            return await Task.FromResult(false);
        }

        return await Task.FromResult(CanPlayerSplit(player));
    }

    /// <inheritdoc />
    public IReadOnlyList<Hand> GetPlayerHands(string playerName)
    {
        var playerHands = GetPlayerHandsInternal(playerName);
        return playerHands.Select(ph => ph.Hand).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets all PlayerHand objects for a specific player (internal use).
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>A read-only list of the player's PlayerHand objects.</returns>
    private IReadOnlyList<PlayerHand> GetPlayerHandsInternal(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return new List<PlayerHand>().AsReadOnly();
        }

        if (_playerHands.TryGetValue(playerName, out var hands))
        {
            return hands.AsReadOnly();
        }

        // If no split hands exist, return the player's main hand
        var player = GetPlayer(playerName);
        if (player?.HasActiveBet == true && player.CurrentBet != null)
        {
            var mainHand = new PlayerHand(player.Hand, player.CurrentBet);
            return new List<PlayerHand> { mainHand }.AsReadOnly();
        }

        return new List<PlayerHand>().AsReadOnly();
    }

    /// <summary>
    /// Gets the current active hand for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>The current active hand, or null if none exists.</returns>
    public PlayerHand? GetCurrentPlayerHand(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return null;
        }

        var hands = GetPlayerHandsInternal(playerName);
        if (hands.Count == 0)
        {
            return null;
        }

        if (!_currentHandIndex.TryGetValue(playerName, out var handIndex))
        {
            handIndex = 0;
        }

        return handIndex < hands.Count ? hands[handIndex] : null;
    }

    /// <summary>
    /// Advances to the next hand for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>True if there is a next hand, false if all hands are complete.</returns>
    public bool AdvanceToNextPlayerHand(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return false;
        }

        var hands = GetPlayerHandsInternal(playerName);
        if (hands.Count <= 1)
        {
            return false; // No multiple hands to advance through
        }

        if (!_currentHandIndex.TryGetValue(playerName, out var currentIndex))
        {
            currentIndex = 0;
        }

        // Mark current hand as inactive
        if (currentIndex < hands.Count)
        {
            hands[currentIndex].MarkAsInactive();
        }

        // Find next active hand
        do
        {
            currentIndex++;
        } while (currentIndex < hands.Count && !hands[currentIndex].IsActive);

        _currentHandIndex[playerName] = currentIndex;
        return currentIndex < hands.Count;
    }

    /// <summary>
    /// Determines if a player has more hands to play.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>True if the player has more active hands, false otherwise.</returns>
    public bool PlayerHasMoreHands(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return false;
        }

        var hands = GetPlayerHandsInternal(playerName);
        if (!_currentHandIndex.TryGetValue(playerName, out var currentIndex))
        {
            currentIndex = 0;
        }

        // Check if there are any active hands after the current one
        for (int i = currentIndex + 1; i < hands.Count; i++)
        {
            if (hands[i].IsActive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Initializes multiple hands tracking for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    private void InitializePlayerHands(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return;
        }

        var player = GetPlayer(playerName);
        if (player?.HasActiveBet == true && player.CurrentBet != null)
        {
            var mainHand = new PlayerHand(player.Hand, player.CurrentBet);
            _playerHands[playerName] = new List<PlayerHand> { mainHand };
            _currentHandIndex[playerName] = 0;
        }
    }

    /// <summary>
    /// Clears multiple hands tracking for all players.
    /// </summary>
    private void ClearAllPlayerHands()
    {
        _playerHands.Clear();
        _currentHandIndex.Clear();
    }

    private PlayerActionResult ProcessSplitAction(Player player)
    {
        // Validate that player can split
        if (!CanPlayerSplit(player))
        {
            return PlayerActionResult.Failure("Cannot split. Player must have exactly 2 cards of the same rank and sufficient funds.");
        }

        if (_shoe.RemainingCards < 2)
        {
            return PlayerActionResult.Failure("Not enough cards available in the shoe to split.");
        }

        // For now, we'll implement a basic split that creates a new hand but doesn't fully support multiple hands
        // This will be enhanced in task 4.3
        try
        {
            var originalBet = player.CurrentBet!;
            
            // Create split bet (this will validate sufficient funds)
            var splitBet = _splitHandManager.CreateSplitBet(originalBet);
            
            // Deduct additional bet amount from bankroll
            player.DeductFunds(originalBet.Amount);
            
            // Split the hand
            var (firstHand, secondHand) = _splitHandManager.SplitHand(player.Hand);
            
            // For now, we'll just replace the player's hand with the first split hand
            // and deal one card to it. Full multiple hand support will be in task 4.3
            player.ClearHand();
            player.AddCard(firstHand.Cards[0]);
            
            // Deal one card to the first hand
            player.AddCard(_shoe.Draw());
            
            // Mark as split hand
            player.Hand.MarkAsSplitHand();
            
            // Update bet to split bet
            player.ClearBet();
            player.PlaceBet(splitBet.Amount, BetType.Split);
            
            // For split Aces, the hand is complete after one card
            if (firstHand.Cards[0].Rank == Rank.Ace)
            {
                player.Hand.MarkAsComplete();
                AdvanceToNextActivePlayer();
                return PlayerActionResult.SuccessEndTurn(player.Hand, isSplit: true);
            }
            
            // For other splits, player can continue playing
            return PlayerActionResult.Success(player.Hand, shouldContinueTurn: true, isSplit: true);
        }
        catch (InvalidOperationException ex)
        {
            return PlayerActionResult.Failure($"Cannot split: {ex.Message}");
        }
    }

    private bool CanPlayerSplit(Player player)
    {
        // Must have exactly 2 cards of the same rank
        if (!_gameRules.CanSplit(player.Hand))
        {
            return false;
        }

        // Must not be busted or have blackjack
        if (player.IsBusted() || player.HasBlackjack())
        {
            return false;
        }

        // Must have an active bet and sufficient funds to match it
        if (!player.HasActiveBet || player.CurrentBet == null)
        {
            return false;
        }

        return player.HasSufficientFunds(player.CurrentBet.Amount);
    }

    private bool CanPlayerDoubleDown(Player player)
    {
        // Must have exactly 2 cards
        if (player.GetCardCount() != 2)
        {
            return false;
        }

        // Must not be busted or have blackjack
        if (player.IsBusted() || player.HasBlackjack())
        {
            return false;
        }

        // Must have an active bet and sufficient funds to double it
        if (!player.HasActiveBet || player.CurrentBet == null)
        {
            return false;
        }

        return player.HasSufficientFunds(player.CurrentBet.Amount);
    }

    private void AdvanceToNextActivePlayer()
    {
        // First, check if current player has more hands to play
        if (_currentPlayerIndex < _players.Count)
        {
            var currentPlayer = _players[_currentPlayerIndex];
            if (PlayerHasMoreHands(currentPlayer.Name))
            {
                AdvanceToNextPlayerHand(currentPlayer.Name);
                return; // Stay with same player, different hand
            }
        }

        // Move to next player
        do
        {
            _currentPlayerIndex++;
        } while (_currentPlayerIndex < _players.Count && 
                 IsPlayerCompletelyDone(_players[_currentPlayerIndex]));

        // If we found a new player, initialize their hands and reset to first hand
        if (_currentPlayerIndex < _players.Count)
        {
            var nextPlayer = _players[_currentPlayerIndex];
            InitializePlayerHands(nextPlayer.Name);
            _currentHandIndex[nextPlayer.Name] = 0;
            return;
        }

        // No more active players, check if we should move to dealer turn
        if (_currentPlayerIndex >= _players.Count)
        {
            // Check if any players are still in the game (not all hands busted)
            var playersStillInGame = _players.Any(p => !AreAllPlayerHandsBusted(p.Name));
            
            if (playersStillInGame)
            {
                _currentPhase = GamePhase.DealerTurn;
            }
            else
            {
                // All players busted, skip dealer turn
                _currentPhase = GamePhase.Results;
            }
        }
    }

    /// <summary>
    /// Determines if a player is completely done (all hands complete or busted).
    /// </summary>
    /// <param name="player">The player to check.</param>
    /// <returns>True if the player is completely done, false otherwise.</returns>
    private bool IsPlayerCompletelyDone(Player player)
    {
        // Check if player has blackjack or is busted on their main hand
        if (player.HasBlackjack() || player.IsBusted())
        {
            return true;
        }

        // Check if all player hands are complete
        var hands = GetPlayerHandsInternal(player.Name);
        return hands.Count > 0 && hands.All(h => h.IsComplete);
    }

    /// <summary>
    /// Determines if all of a player's hands are busted.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <returns>True if all hands are busted, false otherwise.</returns>
    private bool AreAllPlayerHandsBusted(string playerName)
    {
        var hands = GetPlayerHandsInternal(playerName);
        if (hands.Count == 0)
        {
            var player = GetPlayer(playerName);
            return player?.IsBusted() == true;
        }

        return hands.All(h => h.IsBusted);
    }
}