using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Validation;

namespace GroupProject.Presentation.Console;

/// <summary>
/// Console implementation of IUserInterface providing formatted display operations for the blackjack game.
/// Handles all user interface operations including game state display, player interactions, and result presentation.
/// </summary>
public class ConsoleUserInterface : IUserInterface
{
    private readonly IInputProvider _inputProvider;
    private readonly IOutputProvider _outputProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleUserInterface"/> class.
    /// </summary>
    /// <param name="inputProvider">The input provider for user interactions.</param>
    /// <param name="outputProvider">The output provider for display operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ConsoleUserInterface(IInputProvider inputProvider, IOutputProvider outputProvider)
    {
        _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
        _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
    }

    /// <summary>
    /// Shows the welcome message and game instructions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowWelcomeMessageAsync()
    {
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("╔══════════════════════════════════════════════════════════════╗");
        await _outputProvider.WriteLineAsync("║                    WELCOME TO BLACKJACK                     ║");
        await _outputProvider.WriteLineAsync("╚══════════════════════════════════════════════════════════════╝");
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("Game Rules:");
        await _outputProvider.WriteLineAsync("• Get as close to 21 as possible without going over");
        await _outputProvider.WriteLineAsync("• Aces count as 1 or 11, face cards count as 10");
        await _outputProvider.WriteLineAsync("• Dealer hits on 16, stands on 17");
        await _outputProvider.WriteLineAsync("• Blackjack is 21 with exactly 2 cards");
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("Available Actions:");
        await _outputProvider.WriteLineAsync("• Hit (h) - Take another card");
        await _outputProvider.WriteLineAsync("• Stand (s) - Keep your current hand");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays the current game state including all player hands.
    /// </summary>
    /// <param name="gameState">The current game state to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when gameState is null.</exception>
    public async Task ShowGameStateAsync(GameState gameState)
    {
        if (gameState == null)
            throw new ArgumentNullException(nameof(gameState));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync($"                    GAME STATE - {gameState.CurrentPhase}");
        await _outputProvider.WriteLineAsync("═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();

        // Show dealer hand (hide first card during player turns)
        bool hideDealerCard = gameState.CurrentPhase == GamePhase.PlayerTurns || 
                             gameState.CurrentPhase == GamePhase.InitialDeal;
        await ShowPlayerHandAsync(gameState.Dealer, hideDealerCard);
        await _outputProvider.WriteLineAsync();

        // Show all player hands
        foreach (var player in gameState.Players)
        {
            await ShowPlayerHandAsync(player);
            
            // Highlight current player
            if (player.Name == gameState.CurrentPlayerName)
            {
                await _outputProvider.WriteLineAsync("    ← Current Player");
            }
            
            await _outputProvider.WriteLineAsync();
        }
    }

    /// <summary>
    /// Displays a specific player's hand with formatted card display.
    /// </summary>
    /// <param name="player">The player whose hand to display.</param>
    /// <param name="hideFirstCard">Whether to hide the first card (typically for dealer).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when player is null.</exception>
    public async Task ShowPlayerHandAsync(Player player, bool hideFirstCard = false)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        var playerType = player.IsDealer ? "DEALER" : "PLAYER";
        var nameDisplay = player.IsDealer ? player.Name.ToUpper() : player.Name;
        
        await _outputProvider.WriteLineAsync($"{playerType}: {nameDisplay}");
        
        var cards = player.Hand.Cards.ToList();
        if (!cards.Any())
        {
            await _outputProvider.WriteLineAsync("  No cards");
            return;
        }

        // Display cards
        await _outputProvider.WriteAsync("  Cards: ");
        
        for (int i = 0; i < cards.Count; i++)
        {
            if (i > 0)
                await _outputProvider.WriteAsync(", ");
                
            if (hideFirstCard && i == 0)
            {
                await _outputProvider.WriteAsync("[Hidden Card]");
            }
            else
            {
                await _outputProvider.WriteAsync(FormatCard(cards[i]));
            }
        }
        
        await _outputProvider.WriteLineAsync();

        // Display hand value (don't show if dealer's first card is hidden)
        if (!hideFirstCard || !player.IsDealer)
        {
            var handValue = player.GetHandValue();
            var softIndicator = player.HasSoftHand() ? " (Soft)" : "";
            await _outputProvider.WriteLineAsync($"  Value: {handValue}{softIndicator}");
            
            // Show special conditions
            if (player.HasBlackjack())
            {
                await _outputProvider.WriteLineAsync("  ★ BLACKJACK! ★");
            }
            else if (player.IsBusted())
            {
                await _outputProvider.WriteLineAsync("  ✗ BUSTED!");
            }
        }
        else
        {
            await _outputProvider.WriteLineAsync("  Value: ? + " + (cards.Count > 1 ? FormatCard(cards[1]) : "?"));
        }
    }

    /// <summary>
    /// Displays the final game results for all players.
    /// </summary>
    /// <param name="results">The game results to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
    public async Task ShowGameResultsAsync(GameSummary results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("╔══════════════════════════════════════════════════════════════╗");
        await _outputProvider.WriteLineAsync("║                        GAME RESULTS                         ║");
        await _outputProvider.WriteLineAsync("╚══════════════════════════════════════════════════════════════╝");
        await _outputProvider.WriteLineAsync();

        // Show dealer's final hand
        await _outputProvider.WriteLineAsync("DEALER FINAL HAND:");
        await _outputProvider.WriteLineAsync($"  Cards: {string.Join(", ", results.DealerHand.Cards.Select(FormatCard))}");
        await _outputProvider.WriteLineAsync($"  Value: {results.DealerHand.GetValue()}");
        
        if (results.DealerHand.IsBusted())
        {
            await _outputProvider.WriteLineAsync("  ✗ DEALER BUSTED!");
        }
        else if (results.DealerHand.IsBlackjack())
        {
            await _outputProvider.WriteLineAsync("  ★ DEALER BLACKJACK!");
        }
        
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("PLAYER RESULTS:");
        await _outputProvider.WriteLineAsync("───────────────────────────────────────────────────────────────");

        // Show each player's result
        foreach (var playerResult in results.PlayerResults)
        {
            var playerName = playerResult.Key;
            var result = playerResult.Value;
            
            var resultSymbol = result switch
            {
                GameResult.Win => "✓",
                GameResult.Blackjack => "★",
                GameResult.Push => "=",
                GameResult.Lose => "✗",
                _ => "?"
            };
            
            var resultText = result switch
            {
                GameResult.Win => "WIN",
                GameResult.Blackjack => "BLACKJACK WIN",
                GameResult.Push => "PUSH (TIE)",
                GameResult.Lose => "LOSE",
                _ => "UNKNOWN"
            };
            
            await _outputProvider.WriteLineAsync($"  {resultSymbol} {playerName}: {resultText}");
        }

        await _outputProvider.WriteLineAsync();
        
        // Show summary statistics
        await _outputProvider.WriteLineAsync("GAME SUMMARY:");
        await _outputProvider.WriteLineAsync($"  Winners: {results.WinnerCount}");
        await _outputProvider.WriteLineAsync($"  Losers: {results.LoserCount}");
        await _outputProvider.WriteLineAsync($"  Pushes: {results.PushCount}");
        if (results.BlackjackCount > 0)
        {
            await _outputProvider.WriteLineAsync($"  Blackjacks: {results.BlackjackCount}");
        }
        
        await _outputProvider.WriteLineAsync($"  Game completed at: {results.GameEndTime:HH:mm:ss}");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Prompts the user for the number of players and returns the result.
    /// </summary>
    /// <returns>A task that returns the number of players.</returns>
    public async Task<int> GetPlayerCountAsync()
    {
        await _outputProvider.WriteLineAsync("Let's set up the game!");
        return await _inputProvider.GetIntegerInputAsync("How many players will be playing?", 1, 4);
    }

    /// <summary>
    /// Prompts the user for player names and returns the collection.
    /// </summary>
    /// <param name="count">The number of player names to collect.</param>
    /// <returns>A task that returns the collection of player names.</returns>
    public async Task<IEnumerable<string>> GetPlayerNamesAsync(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be greater than zero.", nameof(count));

        var playerNames = new List<string>();
        
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("Enter player names:");
        await _outputProvider.WriteLineAsync("(Names can contain letters, numbers, spaces, and basic punctuation. Max 20 characters.)");
        
        for (int i = 1; i <= count; i++)
        {
            string playerName;
            var attempts = 0;
            const int maxAttempts = 3;
            
            do
            {
                attempts++;
                var rawInput = await _inputProvider.GetInputAsync($"Player {i} name");
                playerName = InputValidator.SanitizeString(rawInput);
                
                if (!InputValidator.IsValidPlayerName(playerName))
                {
                    var errorMessage = InputValidator.GetPlayerNameErrorMessage(rawInput);
                    await ShowErrorMessageAsync(errorMessage);
                    
                    if (attempts >= maxAttempts)
                    {
                        await _outputProvider.WriteLineAsync("💡 Example valid names: John, Player1, Mary-Jane, O'Connor");
                        attempts = 0; // Reset counter but continue trying
                    }
                    
                    playerName = string.Empty; // Reset to continue loop
                }
                else if (playerNames.Contains(playerName, StringComparer.OrdinalIgnoreCase))
                {
                    await ShowErrorMessageAsync("That name is already taken. Please choose a different name.");
                    playerName = string.Empty; // Reset to continue loop
                }
            } while (string.IsNullOrWhiteSpace(playerName));
            
            playerNames.Add(playerName);
            await _outputProvider.WriteLineAsync($"✓ Player {i}: {playerName}");
        }
        
        return playerNames;
    }

    /// <summary>
    /// Prompts the user for a player action and returns the result.
    /// </summary>
    /// <param name="playerName">The name of the player taking the action.</param>
    /// <param name="validActions">The valid actions available to the player.</param>
    /// <returns>A task that returns the selected player action.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty, or validActions is null or empty.</exception>
    public async Task<PlayerAction> GetPlayerActionAsync(string playerName, IEnumerable<PlayerAction> validActions)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));
        
        if (validActions == null || !validActions.Any())
            throw new ArgumentException("Valid actions cannot be null or empty.", nameof(validActions));

        await _outputProvider.WriteLineAsync($"It's {playerName}'s turn!");
        return await _inputProvider.GetPlayerActionAsync(playerName, validActions);
    }

    /// <summary>
    /// Prompts the user to determine if they want to play another round.
    /// </summary>
    /// <returns>A task that returns true if the user wants to play another round, false otherwise.</returns>
    public async Task<bool> ShouldPlayAnotherRoundAsync()
    {
        await _outputProvider.WriteLineAsync();
        return await _inputProvider.GetConfirmationAsync("Would you like to play another round?");
    }

    /// <summary>
    /// Shows an error message to the user with distinctive formatting.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowErrorMessageAsync(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        await _outputProvider.WriteLineAsync($"❌ ERROR: {message}");
    }

    /// <summary>
    /// Shows an informational message to the user.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ShowMessageAsync(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        await _outputProvider.WriteLineAsync($"ℹ️  {message}");
    }

    /// <summary>
    /// Clears the display (if supported by the interface).
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ClearDisplayAsync()
    {
        await _outputProvider.ClearAsync();
    }

    /// <summary>
    /// Formats a card for consistent display throughout the interface.
    /// </summary>
    /// <param name="card">The card to format.</param>
    /// <returns>A formatted string representation of the card.</returns>
    private static string FormatCard(Card card)
    {
        var rank = card.Rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q", 
            Rank.King => "K",
            _ => ((int)card.Rank).ToString()
        };

        var suit = card.Suit switch
        {
            Suit.Spades => "♠",
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            _ => card.Suit.ToString()[0].ToString()
        };

        return $"{rank}{suit}";
    }
}