using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Services;

/// <summary>
/// Implements core game logic and state management for blackjack.
/// </summary>
public class GameService : IGameService
{
    private readonly IShoe _shoe;
    private readonly IGameRules _gameRules;
    private readonly List<Player> _players = new();
    private Player? _dealer;
    private GamePhase _currentPhase = GamePhase.Setup;
    private int _currentPlayerIndex = 0;
    private DateTime _gameStartTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameService"/> class.
    /// </summary>
    /// <param name="shoe">The shoe containing cards for the game.</param>
    /// <param name="gameRules">The game rules implementation.</param>
    public GameService(IShoe shoe, IGameRules gameRules)
    {
        _shoe = shoe ?? throw new ArgumentNullException(nameof(shoe));
        _gameRules = gameRules ?? throw new ArgumentNullException(nameof(gameRules));
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

        var names = playerNames?.ToList() ?? throw new ArgumentNullException(nameof(playerNames));
        
        if (!names.Any())
        {
            throw new ArgumentException("At least one player name must be provided.", nameof(playerNames));
        }

        if (names.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Player names cannot be null or whitespace.", nameof(playerNames));
        }

        // Check for duplicate names (case-insensitive)
        var duplicates = names.GroupBy(n => n.Trim().ToLowerInvariant())
                              .Where(g => g.Count() > 1)
                              .Select(g => g.Key);
        
        if (duplicates.Any())
        {
            throw new ArgumentException($"Duplicate player names are not allowed: {string.Join(", ", duplicates)}", nameof(playerNames));
        }

        // Clear previous game state
        _players.Clear();
        _dealer = null;
        _currentPlayerIndex = 0;
        _gameStartTime = DateTime.UtcNow;

        // Create players
        foreach (var name in names)
        {
            _players.Add(new Player(name.Trim(), PlayerType.Human));
        }

        // Create dealer
        _dealer = new Player("Dealer", PlayerType.Dealer);

        // Shuffle the shoe
        _shoe.Shuffle();

        _currentPhase = GamePhase.InitialDeal;
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
                return PlayerActionResult.Failure("Double down is not yet implemented.");
            
            case PlayerAction.Split:
                return PlayerActionResult.Failure("Split is not yet implemented.");
            
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

    private void AdvanceToNextActivePlayer()
    {
        do
        {
            _currentPlayerIndex++;
        } while (_currentPlayerIndex < _players.Count && 
                 (_players[_currentPlayerIndex].HasBlackjack() || _players[_currentPlayerIndex].IsBusted()));

        // No more active players, check if we should move to dealer turn
        if (_currentPlayerIndex >= _players.Count)
        {
            // Check if any players are still in the game (not busted)
            var playersStillInGame = _players.Any(p => !p.IsBusted());
            
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
}