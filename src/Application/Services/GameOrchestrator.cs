using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.ValueObjects;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.Events;

namespace GroupProject.Application.Services;

/// <summary>
/// Orchestrates complete game sessions and manages game flow control.
/// </summary>
public class GameOrchestrator : IGameOrchestrator
{
    private readonly IGameService _gameService;
    private readonly IUserInterface _userInterface;
    private readonly IErrorHandler _errorHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameOrchestrator"/> class.
    /// </summary>
    /// <param name="gameService">The game service for core game logic.</param>
    /// <param name="userInterface">The user interface for player interaction.</param>
    /// <param name="errorHandler">The error handler for managing exceptions.</param>
    public GameOrchestrator(IGameService gameService, IUserInterface userInterface, IErrorHandler errorHandler)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService));
        _userInterface = userInterface ?? throw new ArgumentNullException(nameof(userInterface));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        
        // Subscribe to shoe reshuffle events
        _gameService.ShoeReshuffled += OnShoeReshuffled;
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
            await HandleGameExceptionAsync(ex, "RunGameAsync");
            
            // Only re-throw if it's not a recoverable error
            if (!_errorHandler.IsRecoverableError(ex))
            {
                throw;
            }
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
        try
        {
            await _userInterface.ShowWelcomeMessageAsync();

            do
            {
                try
                {
                    // Check if shoe needs reshuffling before starting a new round
                    await CheckAndHandleShoeReshuffleAsync();
                    
                    await RunGameAsync();
                }
                catch (Exception ex) when (_errorHandler.IsRecoverableError(ex))
                {
                    // For recoverable errors, show message and continue to next round prompt
                    var userMessage = await _errorHandler.HandleExceptionAsync(ex, "RunMultipleRoundsAsync - Game Round");
                    await _userInterface.ShowErrorMessageAsync(userMessage);
                    await _userInterface.ShowMessageAsync("Let's try starting a new game...");
                }
                
                if (!await ShouldPlayAnotherRoundAsync())
                {
                    break;
                }

                await _userInterface.ShowMessageAsync("\n" + new string('=', 50) + "\n");
            } while (true);

            await _userInterface.ShowMessageAsync("Thanks for playing!");
        }
        catch (Exception ex)
        {
            await HandleGameExceptionAsync(ex, "RunMultipleRoundsAsync");
            throw; // Re-throw for application-level handling
        }
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
        try
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

                try
                {
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
                catch (InvalidPlayerActionException ex)
                {
                    // Handle invalid player actions gracefully
                    var userMessage = await _errorHandler.HandleExceptionAsync(ex, $"HandleSinglePlayerTurnAsync - {player.Name}");
                    await _userInterface.ShowErrorMessageAsync(userMessage);
                    // Continue the loop to let the player try again
                }
                catch (InvalidGameStateException ex)
                {
                    // Handle invalid game state - this might end the player's turn
                    var userMessage = await _errorHandler.HandleExceptionAsync(ex, $"HandleSinglePlayerTurnAsync - {player.Name}");
                    await _userInterface.ShowErrorMessageAsync(userMessage);
                    break; // End this player's turn
                }
            }
        }
        catch (Exception ex)
        {
            await HandleGameExceptionAsync(ex, $"HandleSinglePlayerTurnAsync - {player.Name}");
            // Don't re-throw here as we want to continue with other players
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
        catch (Exception ex)
        {
            var userMessage = await _errorHandler.HandleExceptionAsync(ex, "ShowGameResultsAsync");
            await _userInterface.ShowErrorMessageAsync(userMessage);
        }
    }

    private async Task ShowCurrentGameState()
    {
        var gameState = _gameService.GetCurrentGameState();
        if (gameState != null)
        {
            await _userInterface.ShowGameStateAsync(gameState);
            
            // Show shoe status if it's getting low
            var shoeStatus = _gameService.GetShoeStatus();
            if (shoeStatus.NeedsReshuffle || shoeStatus.IsNearlyEmpty)
            {
                await _userInterface.ShowShoeStatusAsync(shoeStatus);
            }
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

    /// <summary>
    /// Handles exceptions that occur during game operations by logging and showing user-friendly messages.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="context">The context where the exception occurred.</param>
    private async Task HandleGameExceptionAsync(Exception exception, string context)
    {
        var userMessage = await _errorHandler.HandleExceptionAsync(exception, context);
        await _userInterface.ShowErrorMessageAsync(userMessage);
    }

    /// <summary>
    /// Handles shoe reshuffle events by notifying the user interface.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The shoe reshuffle event arguments.</param>
    private async void OnShoeReshuffled(object? sender, ShoeReshuffleEventArgs e)
    {
        try
        {
            await _userInterface.ShowShoeReshuffleNotificationAsync(e);
        }
        catch (Exception ex)
        {
            // Log the error but don't let it crash the game
            await HandleGameExceptionAsync(ex, "OnShoeReshuffled");
        }
    }

    /// <summary>
    /// Checks if the shoe needs reshuffling and handles it before starting a new round.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CheckAndHandleShoeReshuffleAsync()
    {
        try
        {
            var shoeStatus = _gameService.GetShoeStatus();
            
            // If shoe needs reshuffling or is nearly empty, trigger a manual reshuffle
            if (shoeStatus.NeedsReshuffle || shoeStatus.IsNearlyEmpty)
            {
                await _userInterface.ShowMessageAsync("Preparing shoe for next round...");
                _gameService.TriggerShoeReshuffle("Pre-round reshuffle - ensuring adequate cards for gameplay");
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't let it prevent the game from starting
            await HandleGameExceptionAsync(ex, "CheckAndHandleShoeReshuffleAsync");
        }
    }
}