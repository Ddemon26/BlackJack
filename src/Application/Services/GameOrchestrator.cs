using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Application.Services;

/// <summary>
/// Orchestrates complete game sessions and manages game flow control.
/// </summary>
public class GameOrchestrator : IGameOrchestrator
{
    private readonly IGameService _gameService;
    private readonly IUserInterface _userInterface;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameOrchestrator"/> class.
    /// </summary>
    /// <param name="gameService">The game service for core game logic.</param>
    /// <param name="userInterface">The user interface for player interaction.</param>
    public GameOrchestrator(IGameService gameService, IUserInterface userInterface)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _userInterface = userInterface ?? throw new ArgumentNullException(nameof(userInterface));
    }

    /// <inheritdoc />
    public async Task RunGameAsync()
    {
        try
        {
            // Get player information
            var playerCount = await GetPlayerCountAsync();
            var playerNames = await GetPlayerNamesAsync(playerCount);

            // Start the game
            _gameService.StartNewGame(playerNames);
            await _userInterface.ShowMessageAsync("Starting new game...");

            // Deal initial cards
            _gameService.DealInitialCards();
            await ShowCurrentGameState();

            // Handle player turns
            await HandlePlayerTurnsAsync();

            // Handle dealer turn if needed
            await HandleDealerTurnAsync();

            // Show final results
            await ShowGameResultsAsync();
        }
        catch (Exception ex)
        {
            await _userInterface.ShowErrorMessageAsync($"An error occurred during the game: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShouldPlayAnotherRoundAsync()
    {
        return await _userInterface.ShouldPlayAnotherRoundAsync();
    }

    /// <inheritdoc />
    public async Task RunMultipleRoundsAsync()
    {
        await _userInterface.ShowWelcomeMessageAsync();

        do
        {
            await RunGameAsync();
            
            if (!await ShouldPlayAnotherRoundAsync())
            {
                break;
            }

            await _userInterface.ShowMessageAsync("\n" + new string('=', 50) + "\n");
        } while (true);

        await _userInterface.ShowMessageAsync("Thanks for playing!");
    }

    /// <inheritdoc />
    public async Task<int> GetPlayerCountAsync()
    {
        return await _userInterface.GetPlayerCountAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetPlayerNamesAsync(int playerCount)
    {
        return await _userInterface.GetPlayerNamesAsync(playerCount);
    }

    private async Task HandlePlayerTurnsAsync()
    {
        var gameState = _gameService.GetCurrentGameState();
        if (gameState?.CurrentPhase != GamePhase.PlayerTurns)
        {
            return;
        }

        var players = _gameService.GetPlayers();
        
        foreach (var player in players)
        {
            // Skip players who have blackjack or are busted
            if (player.HasBlackjack() || player.IsBusted())
            {
                if (player.HasBlackjack())
                {
                    await _userInterface.ShowMessageAsync($"{player.Name} has blackjack!");
                }
                continue;
            }

            await HandleSinglePlayerTurnAsync(player);
            
            // Check if game state changed (e.g., moved to dealer turn)
            gameState = _gameService.GetCurrentGameState();
            if (gameState?.CurrentPhase != GamePhase.PlayerTurns)
            {
                break;
            }
        }
    }

    private async Task HandleSinglePlayerTurnAsync(Player player)
    {
        while (_gameService.IsPlayerTurn(player.Name))
        {
            await _userInterface.ShowMessageAsync($"\n{player.Name}'s turn:");
            await _userInterface.ShowPlayerHandAsync(player);

            // Check if player is busted or has 21
            if (player.IsBusted())
            {
                await _userInterface.ShowMessageAsync($"{player.Name} is busted!");
                break;
            }

            if (player.GetHandValue() == 21)
            {
                await _userInterface.ShowMessageAsync($"{player.Name} has 21!");
                break;
            }

            // Get valid actions
            var validActions = GetValidActionsForPlayer(player);
            if (!validActions.Any())
            {
                break;
            }

            // Get player action
            var action = await _userInterface.GetPlayerActionAsync(player.Name, validActions);

            // Process the action
            var result = _gameService.ProcessPlayerAction(player.Name, action);

            if (!result.IsSuccess)
            {
                await _userInterface.ShowErrorMessageAsync(result.ErrorMessage!);
                continue;
            }

            // Show result of action
            if (action == PlayerAction.Hit)
            {
                await _userInterface.ShowMessageAsync($"{player.Name} hits.");
                await _userInterface.ShowPlayerHandAsync(player);

                if (result.IsBusted)
                {
                    await _userInterface.ShowMessageAsync($"{player.Name} is busted!");
                }
                else if (result.UpdatedHand?.GetValue() == 21)
                {
                    await _userInterface.ShowMessageAsync($"{player.Name} has 21!");
                }
            }
            else if (action == PlayerAction.Stand)
            {
                await _userInterface.ShowMessageAsync($"{player.Name} stands.");
            }

            // Check if turn should continue
            if (!result.ShouldContinueTurn)
            {
                break;
            }
        }
    }

    private async Task HandleDealerTurnAsync()
    {
        var gameState = _gameService.GetCurrentGameState();
        if (gameState?.CurrentPhase != GamePhase.DealerTurn)
        {
            return;
        }

        await _userInterface.ShowMessageAsync("\nDealer's turn:");
        
        var dealer = _gameService.GetDealer();
        if (dealer != null)
        {
            // Show dealer's full hand
            await _userInterface.ShowPlayerHandAsync(dealer);

            // Play dealer turn
            _gameService.PlayDealerTurn();

            // Show final dealer hand
            await _userInterface.ShowMessageAsync("Dealer's final hand:");
            await _userInterface.ShowPlayerHandAsync(dealer);

            if (dealer.IsBusted())
            {
                await _userInterface.ShowMessageAsync("Dealer is busted!");
            }
        }
    }

    private async Task ShowGameResultsAsync()
    {
        try
        {
            var results = _gameService.GetGameResults();
            await _userInterface.ShowGameResultsAsync(results);
        }
        catch (InvalidOperationException ex)
        {
            await _userInterface.ShowErrorMessageAsync($"Could not get game results: {ex.Message}");
        }
    }

    private async Task ShowCurrentGameState()
    {
        var gameState = _gameService.GetCurrentGameState();
        if (gameState != null)
        {
            await _userInterface.ShowGameStateAsync(gameState);
        }
    }

    private IEnumerable<PlayerAction> GetValidActionsForPlayer(Player player)
    {
        var validActions = new List<PlayerAction>();

        // Always allow hit and stand for basic implementation
        validActions.Add(PlayerAction.Hit);
        validActions.Add(PlayerAction.Stand);

        // Future: Add logic for double down and split based on game rules
        // if (canDoubleDown) validActions.Add(PlayerAction.DoubleDown);
        // if (canSplit) validActions.Add(PlayerAction.Split);

        return validActions;
    }
}