using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Application.Interfaces;
using GroupProject.Application.Models;
using GroupProject.Domain.Entities;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Domain.Events;
using GroupProject.Infrastructure.Validation;
using GroupProject.Infrastructure.Formatting;
using GroupProject.Infrastructure.ObjectPooling;

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
        
        // Show bankroll and bet information for non-dealer players
        if (!player.IsDealer)
        {
            await _outputProvider.WriteLineAsync($"  Bankroll: {MoneyFormatter.FormatWithSymbol(player.Bankroll)}");
            
            if (player.HasActiveBet)
            {
                var bet = player.CurrentBet;
                var betTypeText = bet.Type == BetType.Standard ? "" : $" ({bet.Type})";
                await _outputProvider.WriteLineAsync($"  Current Bet: {MoneyFormatter.FormatWithSymbol(bet.Amount)}{betTypeText}");
            }
        }
        
        var cards = player.Hand.Cards.ToList();
        if (!cards.Any())
        {
            await _outputProvider.WriteLineAsync("  No cards");
            return;
        }

        // Display cards using optimized formatting
        await _outputProvider.WriteAsync("  Cards: ");
        
        if (hideFirstCard && cards.Count > 0)
        {
            await _outputProvider.WriteAsync("[Hidden Card]");
            if (cards.Count > 1)
            {
                await _outputProvider.WriteAsync(", ");
                await _outputProvider.WriteAsync(CardFormatter.FormatCardsSymbols(cards.Skip(1)));
            }
        }
        else
        {
            await _outputProvider.WriteAsync(CardFormatter.FormatCardsSymbols(cards));
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
            await _outputProvider.WriteLineAsync("  Value: ? + " + (cards.Count > 1 ? CardFormatter.FormatCardSymbol(cards[1]) : "?"));
        }
    }

    /// <summary>
    /// Displays a multi-hand player with all their hands (for split scenarios).
    /// </summary>
    /// <param name="multiHandPlayer">The multi-hand player to display.</param>
    /// <param name="hideFirstCard">Whether to hide the first card (typically for dealer).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when multiHandPlayer is null.</exception>
    public async Task ShowMultiHandPlayerAsync(MultiHandPlayer multiHandPlayer, bool hideFirstCard = false)
    {
        if (multiHandPlayer == null)
            throw new ArgumentNullException(nameof(multiHandPlayer));

        var player = multiHandPlayer.Player;
        var playerType = player.IsDealer ? "DEALER" : "PLAYER";
        var nameDisplay = player.IsDealer ? player.Name.ToUpper() : player.Name;
        
        await _outputProvider.WriteLineAsync($"{playerType}: {nameDisplay}");
        
        // Show bankroll information for non-dealer players
        if (!player.IsDealer)
        {
            await _outputProvider.WriteLineAsync($"  Bankroll: {MoneyFormatter.FormatWithSymbol(player.Bankroll)}");
            
            if (multiHandPlayer.HandCount > 1)
            {
                var totalBet = multiHandPlayer.GetTotalBetAmount();
                await _outputProvider.WriteLineAsync($"  Total Bet: {MoneyFormatter.FormatWithSymbol(totalBet)} ({multiHandPlayer.HandCount} hands)");
            }
        }

        // Display each hand
        if (multiHandPlayer.HandCount == 0)
        {
            await _outputProvider.WriteLineAsync("  No hands");
            return;
        }

        for (int i = 0; i < multiHandPlayer.Hands.Count; i++)
        {
            var playerHand = multiHandPlayer.Hands[i];
            var handNumber = multiHandPlayer.HandCount > 1 ? $" #{i + 1}" : "";
            var currentIndicator = playerHand == multiHandPlayer.CurrentHand ? " ← Current" : "";
            var splitIndicator = playerHand.IsSplitHand ? " (Split)" : "";
            var statusIndicator = !playerHand.IsActive && !playerHand.IsComplete ? " (Inactive)" : 
                                 playerHand.IsComplete ? " (Complete)" : "";

            await _outputProvider.WriteLineAsync($"  Hand{handNumber}{splitIndicator}{statusIndicator}{currentIndicator}:");

            // Show bet for this hand
            var betTypeText = playerHand.Bet.Type == BetType.Standard ? "" : $" ({playerHand.Bet.Type})";
            await _outputProvider.WriteLineAsync($"    Bet: {MoneyFormatter.FormatWithSymbol(playerHand.Bet.Amount)}{betTypeText}");

            var cards = playerHand.Hand.Cards.ToList();
            if (!cards.Any())
            {
                await _outputProvider.WriteLineAsync("    No cards");
                continue;
            }

            // Display cards using optimized formatting
            await _outputProvider.WriteAsync("    Cards: ");
            
            if (hideFirstCard && cards.Count > 0 && i == 0) // Only hide first card of first hand for dealer
            {
                await _outputProvider.WriteAsync("[Hidden Card]");
                if (cards.Count > 1)
                {
                    await _outputProvider.WriteAsync(", ");
                    await _outputProvider.WriteAsync(CardFormatter.FormatCardsSymbols(cards.Skip(1)));
                }
            }
            else
            {
                await _outputProvider.WriteAsync(CardFormatter.FormatCardsSymbols(cards));
            }
            
            await _outputProvider.WriteLineAsync();

            // Display hand value (don't show if dealer's first card is hidden)
            if (!hideFirstCard || !player.IsDealer || i > 0)
            {
                var handValue = playerHand.HandValue;
                var softIndicator = playerHand.Hand.IsSoft() ? " (Soft)" : "";
                await _outputProvider.WriteLineAsync($"    Value: {handValue}{softIndicator}");
                
                // Show special conditions
                if (playerHand.IsBlackjack)
                {
                    await _outputProvider.WriteLineAsync("    ★ BLACKJACK! ★");
                }
                else if (playerHand.IsBusted)
                {
                    await _outputProvider.WriteLineAsync("    ✗ BUSTED!");
                }
            }
            else
            {
                await _outputProvider.WriteLineAsync("    Value: ? + " + (cards.Count > 1 ? CardFormatter.FormatCardSymbol(cards[1]) : "?"));
            }
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

        // Show dealer's final hand using optimized formatting
        await _outputProvider.WriteLineAsync("DEALER FINAL HAND:");
        await _outputProvider.WriteLineAsync($"  Cards: {CardFormatter.FormatCardsSymbols(results.DealerHand.Cards)}");
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
    /// Shows a notification when the shoe is reshuffled.
    /// </summary>
    /// <param name="reshuffleEventArgs">The reshuffle event arguments containing details about the reshuffle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when reshuffleEventArgs is null.</exception>
    public async Task ShowShoeReshuffleNotificationAsync(ShoeReshuffleEventArgs reshuffleEventArgs)
    {
        if (reshuffleEventArgs == null)
            throw new ArgumentNullException(nameof(reshuffleEventArgs));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🔄 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("🔄                    SHOE RESHUFFLED                           ");
        await _outputProvider.WriteLineAsync("🔄 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync($"🔄 Reason: {reshuffleEventArgs.Reason}");
        await _outputProvider.WriteLineAsync($"🔄 Cards remaining when triggered: {reshuffleEventArgs.RemainingPercentage:P1}");
        await _outputProvider.WriteLineAsync($"🔄 Penetration threshold: {reshuffleEventArgs.PenetrationThreshold:P1}");
        await _outputProvider.WriteLineAsync($"🔄 Time: {reshuffleEventArgs.Timestamp:HH:mm:ss}");
        await _outputProvider.WriteLineAsync("🔄 The shoe has been shuffled and is ready for continued play.");
        await _outputProvider.WriteLineAsync("🔄 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Shows the current shoe status when it's relevant to display.
    /// </summary>
    /// <param name="shoeStatus">The shoe status to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when shoeStatus is null.</exception>
    public async Task ShowShoeStatusAsync(ShoeStatus shoeStatus)
    {
        if (shoeStatus == null)
            throw new ArgumentNullException(nameof(shoeStatus));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("📊                     SHOE STATUS                              ");
        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync($"📊 Decks: {shoeStatus.DeckCount}");
        await _outputProvider.WriteLineAsync($"📊 Cards remaining: {shoeStatus.RemainingCards}/{shoeStatus.TotalCards} ({shoeStatus.RemainingPercentage:P1})");
        await _outputProvider.WriteLineAsync($"📊 Cards dealt: {shoeStatus.CardsDealt}");
        await _outputProvider.WriteLineAsync($"📊 Penetration threshold: {shoeStatus.PenetrationThreshold:P1}");
        
        if (shoeStatus.NeedsReshuffle)
        {
            await _outputProvider.WriteLineAsync("📊 ⚠️  RESHUFFLE NEEDED - Shoe has reached penetration threshold");
        }
        else if (shoeStatus.IsNearlyEmpty)
        {
            await _outputProvider.WriteLineAsync("📊 ⚠️  SHOE NEARLY EMPTY - Reshuffle will occur soon");
        }
        
        if (shoeStatus.AutoReshuffleEnabled)
        {
            await _outputProvider.WriteLineAsync("📊 🔄 Automatic reshuffling: ENABLED");
        }
        else
        {
            await _outputProvider.WriteLineAsync("📊 🔄 Automatic reshuffling: DISABLED");
        }
        
        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Prompts the user to place a bet for the specified player.
    /// </summary>
    /// <param name="playerName">The name of the player placing the bet.</param>
    /// <param name="minBet">The minimum allowed bet amount.</param>
    /// <param name="maxBet">The maximum allowed bet amount.</param>
    /// <param name="availableFunds">The player's available funds.</param>
    /// <returns>A task that returns the bet amount.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when any Money parameter is null.</exception>
    public async Task<Money> GetBetAmountAsync(string playerName, Money minBet, Money maxBet, Money availableFunds)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync($"💰                    BETTING TIME - {playerName.ToUpper()}");
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");

        return await _inputProvider.GetBetAmountAsync($"Enter your bet amount", minBet, maxBet, availableFunds);
    }

    /// <summary>
    /// Displays the current bankroll and betting information for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="bankroll">The player's current bankroll.</param>
    /// <param name="currentBet">The player's current bet, if any.</param>
    /// <param name="minBet">The minimum bet amount.</param>
    /// <param name="maxBet">The maximum bet amount.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    public async Task ShowBankrollInfoAsync(string playerName, Money bankroll, Bet? currentBet = null, Money? minBet = null, Money? maxBet = null)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        await _outputProvider.WriteLineAsync($"💳 {playerName} - Bankroll: {MoneyFormatter.FormatWithSymbol(bankroll)}");
        
        if (currentBet != null)
        {
            var betStatus = currentBet.IsActive ? "Active" : "Settled";
            await _outputProvider.WriteLineAsync($"   Current Bet: {MoneyFormatter.FormatWithSymbol(currentBet.Amount)} ({betStatus})");
        }

        if (minBet.HasValue && maxBet.HasValue)
        {
            await _outputProvider.WriteLineAsync($"   Betting Limits: {MoneyFormatter.FormatWithSymbol(minBet.Value)} - {MoneyFormatter.FormatWithSymbol(maxBet.Value)}");
        }
    }

    /// <summary>
    /// Displays a bet confirmation message.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="bet">The bet that was placed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when bet is null.</exception>
    public async Task ShowBetConfirmationAsync(string playerName, Bet bet)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (bet == null)
            throw new ArgumentNullException(nameof(bet));

        await _outputProvider.WriteLineAsync($"✅ {playerName} placed bet: {MoneyFormatter.FormatWithSymbol(bet.Amount)}");
        
        if (bet.Type != BetType.Standard)
        {
            await _outputProvider.WriteLineAsync($"   Bet Type: {bet.Type}");
        }
    }

    /// <summary>
    /// Displays betting validation feedback when a bet is invalid.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="attemptedBet">The bet amount that was attempted.</param>
    /// <param name="reason">The reason the bet was invalid.</param>
    /// <param name="availableFunds">The player's available funds.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName or reason is null or empty.</exception>
    public async Task ShowBetValidationErrorAsync(string playerName, Money attemptedBet, string reason, Money availableFunds)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

        await _outputProvider.WriteLineAsync($"❌ {playerName}: Cannot place bet of {MoneyFormatter.FormatWithSymbol(attemptedBet)}");
        await _outputProvider.WriteLineAsync($"   Reason: {reason}");
        await _outputProvider.WriteLineAsync($"   Available funds: {MoneyFormatter.FormatWithSymbol(availableFunds)}");
    }

    /// <summary>
    /// Prompts the user to set up their initial bankroll.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="defaultAmount">The default bankroll amount.</param>
    /// <param name="minAmount">The minimum allowed bankroll.</param>
    /// <param name="maxAmount">The maximum allowed bankroll.</param>
    /// <returns>A task that returns the initial bankroll amount.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    public async Task<Money> GetInitialBankrollAsync(string playerName, Money defaultAmount, Money minAmount, Money maxAmount)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("💰                   BANKROLL SETUP");
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");

        return await _inputProvider.GetInitialBankrollAsync(playerName, defaultAmount, minAmount, maxAmount);
    }

    /// <summary>
    /// Displays the betting round summary showing all player bets.
    /// </summary>
    /// <param name="playerBets">Dictionary of player names and their bets.</param>
    /// <param name="totalPot">The total pot amount.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when playerBets is null.</exception>
    public async Task ShowBettingRoundSummaryAsync(IReadOnlyDictionary<string, Bet> playerBets, Money totalPot)
    {
        if (playerBets == null)
            throw new ArgumentNullException(nameof(playerBets));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("💰                  BETTING ROUND COMPLETE");
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");

        if (!playerBets.Any())
        {
            await _outputProvider.WriteLineAsync("💰 No bets placed this round.");
            return;
        }

        await _outputProvider.WriteLineAsync("💰 PLAYER BETS:");
        foreach (var (playerName, bet) in playerBets.OrderBy(kvp => kvp.Key))
        {
            var betTypeText = bet.Type == BetType.Standard ? "" : $" ({bet.Type})";
            await _outputProvider.WriteLineAsync($"💰   {playerName}: {MoneyFormatter.FormatWithSymbol(bet.Amount)}{betTypeText}");
        }

        await _outputProvider.WriteLineAsync($"💰 TOTAL POT: {MoneyFormatter.FormatWithSymbol(totalPot)}");
        await _outputProvider.WriteLineAsync("💰 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays payout information for a player.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="originalBet">The original bet amount.</param>
    /// <param name="payout">The payout amount.</param>
    /// <param name="totalReturn">The total return (bet + payout).</param>
    /// <param name="gameResult">The game result that determined the payout.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    public async Task ShowPayoutInfoAsync(string playerName, Money originalBet, Money payout, Money totalReturn, GameResult gameResult)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        var resultSymbol = gameResult switch
        {
            GameResult.Win => "✅",
            GameResult.Blackjack => "🎉",
            GameResult.Push => "🤝",
            GameResult.Lose => "❌",
            _ => "❓"
        };

        var resultText = gameResult switch
        {
            GameResult.Win => "WIN",
            GameResult.Blackjack => "BLACKJACK!",
            GameResult.Push => "PUSH",
            GameResult.Lose => "LOSE",
            _ => "UNKNOWN"
        };

        await _outputProvider.WriteLineAsync($"{resultSymbol} {playerName} - {resultText}");
        await _outputProvider.WriteLineAsync($"   Original Bet: {MoneyFormatter.FormatWithSymbol(originalBet)}");
        
        if (gameResult == GameResult.Push)
        {
            await _outputProvider.WriteLineAsync($"   Returned: {MoneyFormatter.FormatWithSymbol(totalReturn)} (bet returned)");
        }
        else if (gameResult == GameResult.Lose)
        {
            await _outputProvider.WriteLineAsync($"   Lost: {MoneyFormatter.FormatWithSymbol(originalBet)}");
        }
        else
        {
            await _outputProvider.WriteLineAsync($"   Payout: {MoneyFormatter.FormatWithSymbol(payout)}");
            await _outputProvider.WriteLineAsync($"   Total Return: {MoneyFormatter.FormatWithSymbol(totalReturn)}");
            
            if (gameResult == GameResult.Blackjack)
            {
                await _outputProvider.WriteLineAsync($"   🎰 Blackjack pays 3:2!");
            }
        }
    }

    /// <summary>
    /// Displays enhanced player statistics including session and lifetime stats.
    /// </summary>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="statistics">The player's statistics.</param>
    /// <param name="sessionStats">Optional session-specific statistics.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when playerName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when statistics is null.</exception>
    public async Task ShowPlayerStatisticsAsync(string playerName, PlayerStatistics statistics, PlayerStatistics? sessionStats = null)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

        if (statistics == null)
            throw new ArgumentNullException(nameof(statistics));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync($"📊                 STATISTICS - {playerName.ToUpper()}");
        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");

        // Lifetime Statistics
        await _outputProvider.WriteLineAsync("📊 LIFETIME STATISTICS:");
        await _outputProvider.WriteLineAsync($"📊   Games Played: {statistics.GamesPlayed}");
        await _outputProvider.WriteLineAsync($"📊   Games Won: {statistics.GamesWon}");
        await _outputProvider.WriteLineAsync($"📊   Games Lost: {statistics.GamesLost}");
        await _outputProvider.WriteLineAsync($"📊   Games Pushed: {statistics.GamesPushed}");
        await _outputProvider.WriteLineAsync($"📊   Blackjacks: {statistics.BlackjacksAchieved}");
        await _outputProvider.WriteLineAsync($"📊   Win Rate: {statistics.WinPercentage:P1}");
        await _outputProvider.WriteLineAsync($"📊   Total Wagered: {MoneyFormatter.FormatWithSymbol(statistics.TotalWagered)}");
        await _outputProvider.WriteLineAsync($"📊   Net Winnings: {MoneyFormatter.FormatWithSymbol(statistics.NetWinnings)}");
        await _outputProvider.WriteLineAsync($"📊   Average Bet: {MoneyFormatter.FormatWithSymbol(statistics.AverageBet)}");
        await _outputProvider.WriteLineAsync($"📊   First Played: {statistics.FirstPlayed:yyyy-MM-dd HH:mm}");
        await _outputProvider.WriteLineAsync($"📊   Last Played: {statistics.LastPlayed:yyyy-MM-dd HH:mm}");

        // Session Statistics (if provided)
        if (sessionStats != null && sessionStats.GamesPlayed > 0)
        {
            await _outputProvider.WriteLineAsync();
            await _outputProvider.WriteLineAsync("📊 SESSION STATISTICS:");
            await _outputProvider.WriteLineAsync($"📊   Session Games: {sessionStats.GamesPlayed}");
            await _outputProvider.WriteLineAsync($"📊   Session Wins: {sessionStats.GamesWon}");
            await _outputProvider.WriteLineAsync($"📊   Session Losses: {sessionStats.GamesLost}");
            await _outputProvider.WriteLineAsync($"📊   Session Pushes: {sessionStats.GamesPushed}");
            await _outputProvider.WriteLineAsync($"📊   Session Blackjacks: {sessionStats.BlackjacksAchieved}");
            await _outputProvider.WriteLineAsync($"📊   Session Win Rate: {sessionStats.WinPercentage:P1}");
            await _outputProvider.WriteLineAsync($"📊   Session Wagered: {MoneyFormatter.FormatWithSymbol(sessionStats.TotalWagered)}");
            await _outputProvider.WriteLineAsync($"📊   Session Net: {MoneyFormatter.FormatWithSymbol(sessionStats.NetWinnings)}");
            await _outputProvider.WriteLineAsync($"📊   Session Avg Bet: {MoneyFormatter.FormatWithSymbol(sessionStats.AverageBet)}");
        }

        await _outputProvider.WriteLineAsync("📊 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays a comprehensive session summary with all player results.
    /// </summary>
    /// <param name="sessionSummary">The session summary to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sessionSummary is null.</exception>
    public async Task ShowSessionSummaryAsync(SessionSummary sessionSummary)
    {
        if (sessionSummary == null)
            throw new ArgumentNullException(nameof(sessionSummary));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎯 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("🎯                    SESSION SUMMARY");
        await _outputProvider.WriteLineAsync("🎯 ═══════════════════════════════════════════════════════════════");

        // Session Overview
        await _outputProvider.WriteLineAsync("🎯 SESSION OVERVIEW:");
        await _outputProvider.WriteLineAsync($"🎯   Session ID: {sessionSummary.SessionId}");
        await _outputProvider.WriteLineAsync($"🎯   Start Time: {sessionSummary.StartTime:yyyy-MM-dd HH:mm:ss}");
        await _outputProvider.WriteLineAsync($"🎯   End Time: {sessionSummary.EndTime:yyyy-MM-dd HH:mm:ss}");
        await _outputProvider.WriteLineAsync($"🎯   Duration: {sessionSummary.Duration:hh\\:mm\\:ss}");
        await _outputProvider.WriteLineAsync($"🎯   Rounds Played: {sessionSummary.RoundsPlayed}");
        await _outputProvider.WriteLineAsync($"🎯   Total Players: {sessionSummary.PlayerStatistics.Count}");

        // Financial Summary
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎯 FINANCIAL SUMMARY:");
        await _outputProvider.WriteLineAsync($"🎯   Total Wagered: {MoneyFormatter.FormatWithSymbol(sessionSummary.TotalWagered)}");
        await _outputProvider.WriteLineAsync($"🎯   Total Net Winnings: {MoneyFormatter.FormatWithSymbol(sessionSummary.TotalNetWinnings)}");
        await _outputProvider.WriteLineAsync($"🎯   Average Bet: {MoneyFormatter.FormatWithSymbol(sessionSummary.AverageBetAmount)}");
        await _outputProvider.WriteLineAsync($"🎯   Largest Bankroll: {MoneyFormatter.FormatWithSymbol(sessionSummary.LargestBankroll)}");

        // Player Results
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎯 PLAYER RESULTS:");
        await _outputProvider.WriteLineAsync("🎯 ┌──────────────┬──────────┬──────────┬──────────┬──────────┬────────────┬────────────┐");
        await _outputProvider.WriteLineAsync("🎯 │ Player       │ Games    │ Wins     │ Losses   │ Pushes   │ Net Win    │ Final Bank │");
        await _outputProvider.WriteLineAsync("🎯 ├──────────────┼──────────┼──────────┼──────────┼──────────┼────────────┼────────────┤");

        foreach (var (playerName, stats) in sessionSummary.PlayerStatistics.OrderByDescending(kvp => kvp.Value.NetWinnings.Amount))
        {
            var finalBankroll = sessionSummary.FinalBankrolls.TryGetValue(playerName, out var bankroll) ? bankroll : Money.Zero;
            var winRate = stats.WinPercentage;
            
            await _outputProvider.WriteLineAsync($"🎯 │ {playerName,-12} │ {stats.GamesPlayed,8} │ {stats.GamesWon,8} │ {stats.GamesLost,8} │ {stats.GamesPushed,8} │ {MoneyFormatter.FormatWithSymbol(stats.NetWinnings),10} │ {MoneyFormatter.FormatWithSymbol(finalBankroll),10} │");
        }

        await _outputProvider.WriteLineAsync("🎯 └──────────────┴──────────┴──────────┴──────────┴──────────┴────────────┴────────────┘");

        // Session Highlights
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎯 SESSION HIGHLIGHTS:");
        
        if (!string.IsNullOrEmpty(sessionSummary.BiggestWinner))
        {
            var winnerStats = sessionSummary.PlayerStatistics[sessionSummary.BiggestWinner];
            await _outputProvider.WriteLineAsync($"🎯   🏆 Biggest Winner: {sessionSummary.BiggestWinner} ({MoneyFormatter.FormatWithSymbol(winnerStats.NetWinnings)})");
        }

        var totalBlackjacks = sessionSummary.PlayerStatistics.Values.Sum(s => s.BlackjacksAchieved);
        if (totalBlackjacks > 0)
        {
            await _outputProvider.WriteLineAsync($"🎯   ⭐ Total Blackjacks: {totalBlackjacks}");
        }

        var (avgGames, avgNet, avgWinRate) = sessionSummary.CalculateAverageMetrics();
        await _outputProvider.WriteLineAsync($"🎯   📈 Average Games per Player: {avgGames:F1}");
        await _outputProvider.WriteLineAsync($"🎯   💰 Average Net per Player: {MoneyFormatter.FormatWithSymbol(avgNet)}");
        await _outputProvider.WriteLineAsync($"🎯   🎲 Average Win Rate: {avgWinRate:P1}");

        await _outputProvider.WriteLineAsync("🎯 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays a compact session summary for quick reference.
    /// </summary>
    /// <param name="sessionSummary">The session summary to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sessionSummary is null.</exception>
    public async Task ShowCompactSessionSummaryAsync(SessionSummary sessionSummary)
    {
        if (sessionSummary == null)
            throw new ArgumentNullException(nameof(sessionSummary));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("📋 SESSION SUMMARY:");
        await _outputProvider.WriteLineAsync($"📋   Duration: {sessionSummary.Duration:hh\\:mm\\:ss} | Rounds: {sessionSummary.RoundsPlayed} | Players: {sessionSummary.PlayerStatistics.Count}");
        await _outputProvider.WriteLineAsync($"📋   Total Wagered: {MoneyFormatter.FormatWithSymbol(sessionSummary.TotalWagered)} | Net: {MoneyFormatter.FormatWithSymbol(sessionSummary.TotalNetWinnings)}");

        if (!string.IsNullOrEmpty(sessionSummary.BiggestWinner))
        {
            var winnerStats = sessionSummary.PlayerStatistics[sessionSummary.BiggestWinner];
            await _outputProvider.WriteLineAsync($"📋   🏆 Top Player: {sessionSummary.BiggestWinner} ({MoneyFormatter.FormatWithSymbol(winnerStats.NetWinnings)})");
        }

        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays final game results with enhanced formatting and statistics.
    /// </summary>
    /// <param name="results">The game results to display.</param>
    /// <param name="payoutSummary">Optional payout summary for detailed financial information.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
    public async Task ShowEnhancedGameResultsAsync(GameSummary results, PayoutSummary? payoutSummary = null)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🏁 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("🏁                     ROUND RESULTS");
        await _outputProvider.WriteLineAsync("🏁 ═══════════════════════════════════════════════════════════════");

        // Show dealer's final hand using optimized formatting
        await _outputProvider.WriteLineAsync("🏁 DEALER FINAL HAND:");
        await _outputProvider.WriteLineAsync($"🏁   Cards: {CardFormatter.FormatCardsSymbols(results.DealerHand.Cards)}");
        await _outputProvider.WriteLineAsync($"🏁   Value: {results.DealerHand.GetValue()}");
        
        if (results.DealerHand.IsBusted())
        {
            await _outputProvider.WriteLineAsync("🏁   ✗ DEALER BUSTED!");
        }
        else if (results.DealerHand.IsBlackjack())
        {
            await _outputProvider.WriteLineAsync("🏁   ★ DEALER BLACKJACK!");
        }
        
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🏁 PLAYER RESULTS:");
        await _outputProvider.WriteLineAsync("🏁 ───────────────────────────────────────────────────────────────");

        // Show each player's result with enhanced formatting
        foreach (var playerResult in results.PlayerResults)
        {
            var playerName = playerResult.Key;
            var result = playerResult.Value;
            
            var resultSymbol = result switch
            {
                GameResult.Win => "🟢",
                GameResult.Blackjack => "⭐",
                GameResult.Push => "🟡",
                GameResult.Lose => "🔴",
                _ => "❓"
            };
            
            var resultText = result switch
            {
                GameResult.Win => "WIN",
                GameResult.Blackjack => "BLACKJACK WIN",
                GameResult.Push => "PUSH (TIE)",
                GameResult.Lose => "LOSE",
                _ => "UNKNOWN"
            };
            
            await _outputProvider.WriteLineAsync($"🏁   {resultSymbol} {playerName}: {resultText}");
        }

        // Show payout information if available
        if (payoutSummary != null && payoutSummary.TotalPayouts > 0)
        {
            await _outputProvider.WriteLineAsync();
            await _outputProvider.WriteLineAsync("🏁 PAYOUT SUMMARY:");
            await _outputProvider.WriteLineAsync($"🏁   Total Payouts: {MoneyFormatter.FormatWithSymbol(payoutSummary.TotalPayoutAmount)}");
            await _outputProvider.WriteLineAsync($"🏁   Total Returns: {MoneyFormatter.FormatWithSymbol(payoutSummary.TotalReturnAmount)}");
        }

        await _outputProvider.WriteLineAsync();
        
        // Show summary statistics with enhanced formatting
        await _outputProvider.WriteLineAsync("🏁 ROUND SUMMARY:");
        await _outputProvider.WriteLineAsync($"🏁   🟢 Winners: {results.WinnerCount}");
        await _outputProvider.WriteLineAsync($"🏁   🔴 Losers: {results.LoserCount}");
        await _outputProvider.WriteLineAsync($"🏁   🟡 Pushes: {results.PushCount}");
        if (results.BlackjackCount > 0)
        {
            await _outputProvider.WriteLineAsync($"🏁   ⭐ Blackjacks: {results.BlackjackCount}");
        }
        
        await _outputProvider.WriteLineAsync($"🏁   ⏰ Completed: {results.GameEndTime:HH:mm:ss}");
        await _outputProvider.WriteLineAsync("🏁 ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync();
    }

    /// <summary>
    /// Displays the configuration menu and handles user selections.
    /// </summary>
    /// <param name="currentConfig">The current game configuration.</param>
    /// <returns>A task that returns the updated configuration or null if cancelled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentConfig is null.</exception>
    public async Task<GameConfiguration?> ShowConfigurationMenuAsync(GameConfiguration currentConfig)
    {
        if (currentConfig == null)
            throw new ArgumentNullException(nameof(currentConfig));

        var config = currentConfig.Clone();
        bool exitMenu = false;

        while (!exitMenu)
        {
            await ShowCurrentConfigurationAsync(config);
            await _outputProvider.WriteLineAsync();
            await _outputProvider.WriteLineAsync("⚙️ CONFIGURATION MENU:");
            await _outputProvider.WriteLineAsync("⚙️   1. Game Rules");
            await _outputProvider.WriteLineAsync("⚙️   2. Betting Settings");
            await _outputProvider.WriteLineAsync("⚙️   3. Display Settings");
            await _outputProvider.WriteLineAsync("⚙️   4. Advanced Settings");
            await _outputProvider.WriteLineAsync("⚙️   5. Reset to Defaults");
            await _outputProvider.WriteLineAsync("⚙️   6. Save and Exit");
            await _outputProvider.WriteLineAsync("⚙️   7. Cancel (Discard Changes)");
            await _outputProvider.WriteLineAsync();

            var choice = await _inputProvider.GetIntegerInputAsync("Select an option", 1, 7);

            switch (choice)
            {
                case 1:
                    await ConfigureGameRulesAsync(config);
                    break;
                case 2:
                    await ConfigureBettingSettingsAsync(config);
                    break;
                case 3:
                    await ConfigureDisplaySettingsAsync(config);
                    break;
                case 4:
                    await ConfigureAdvancedSettingsAsync(config);
                    break;
                case 5:
                    config = new GameConfiguration();
                    await _outputProvider.WriteLineAsync("✅ Configuration reset to defaults.");
                    break;
                case 6:
                    if (config.IsValid)
                    {
                        await _outputProvider.WriteLineAsync("✅ Configuration saved successfully.");
                        return config;
                    }
                    else
                    {
                        await ShowConfigurationErrorsAsync(config);
                    }
                    break;
                case 7:
                    var confirmCancel = await _inputProvider.GetConfirmationAsync("Are you sure you want to discard all changes?");
                    if (confirmCancel)
                    {
                        await _outputProvider.WriteLineAsync("❌ Configuration changes discarded.");
                        return null;
                    }
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// Displays the current configuration settings.
    /// </summary>
    /// <param name="config">The configuration to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public async Task ShowCurrentConfigurationAsync(GameConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("⚙️ ═══════════════════════════════════════════════════════════════");
        await _outputProvider.WriteLineAsync("⚙️                  CURRENT CONFIGURATION");
        await _outputProvider.WriteLineAsync("⚙️ ═══════════════════════════════════════════════════════════════");

        // Game Rules
        await _outputProvider.WriteLineAsync("⚙️ GAME RULES:");
        await _outputProvider.WriteLineAsync($"⚙️   Decks in Shoe: {config.NumberOfDecks}");
        await _outputProvider.WriteLineAsync($"⚙️   Players: {config.MinPlayers}-{config.MaxPlayers}");
        await _outputProvider.WriteLineAsync($"⚙️   Double Down: {(config.AllowDoubleDown ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Split Pairs: {(config.AllowSplit ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Surrender: {(config.AllowSurrender ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Insurance: {(config.AllowInsurance ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Dealer Hits Soft 17: {(config.DealerHitsOnSoft17 ? "Yes" : "No")}");
        await _outputProvider.WriteLineAsync($"⚙️   Blackjack Payout: {config.BlackjackPayout:F1}:1");

        // Betting Settings
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("⚙️ BETTING SETTINGS:");
        await _outputProvider.WriteLineAsync($"⚙️   Minimum Bet: {MoneyFormatter.FormatWithSymbol(config.MinimumBet)}");
        await _outputProvider.WriteLineAsync($"⚙️   Maximum Bet: {MoneyFormatter.FormatWithSymbol(config.MaximumBet)}");
        await _outputProvider.WriteLineAsync($"⚙️   Default Bankroll: {MoneyFormatter.FormatWithSymbol(config.DefaultBankroll)}");
        await _outputProvider.WriteLineAsync($"⚙️   Bankroll Range: {MoneyFormatter.FormatWithSymbol(config.MinimumBankroll)} - {MoneyFormatter.FormatWithSymbol(config.MaximumBankroll)}");

        // Display Settings
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("⚙️ DISPLAY SETTINGS:");
        await _outputProvider.WriteLineAsync($"⚙️   Card Format: {config.CardDisplayFormat}");
        await _outputProvider.WriteLineAsync($"⚙️   Detailed Statistics: {(config.ShowDetailedStatistics ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Save Statistics: {(config.SaveStatistics ? "Enabled" : "Disabled")}");

        // Advanced Settings
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("⚙️ ADVANCED SETTINGS:");
        await _outputProvider.WriteLineAsync($"⚙️   Auto Reshuffle: {(config.AutoReshuffleEnabled ? "Enabled" : "Disabled")}");
        await _outputProvider.WriteLineAsync($"⚙️   Penetration Threshold: {config.PenetrationThreshold:P1}");
        await _outputProvider.WriteLineAsync($"⚙️   Max Player Name Length: {config.PlayerNameMaxLength}");

        await _outputProvider.WriteLineAsync("⚙️ ═══════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Configures game rules settings.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfigureGameRulesAsync(GameConfiguration config)
    {
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎮 GAME RULES CONFIGURATION:");
        await _outputProvider.WriteLineAsync("🎮   1. Number of Decks");
        await _outputProvider.WriteLineAsync("🎮   2. Player Limits");
        await _outputProvider.WriteLineAsync("🎮   3. Double Down");
        await _outputProvider.WriteLineAsync("🎮   4. Split Pairs");
        await _outputProvider.WriteLineAsync("🎮   5. Surrender");
        await _outputProvider.WriteLineAsync("🎮   6. Insurance");
        await _outputProvider.WriteLineAsync("🎮   7. Dealer Hits Soft 17");
        await _outputProvider.WriteLineAsync("🎮   8. Blackjack Payout");
        await _outputProvider.WriteLineAsync("🎮   9. Back to Main Menu");

        var choice = await _inputProvider.GetIntegerInputAsync("Select setting to modify", 1, 9);

        switch (choice)
        {
            case 1:
                config.NumberOfDecks = await _inputProvider.GetIntegerInputAsync("Number of decks (1-8)", 1, 8);
                break;
            case 2:
                config.MinPlayers = await _inputProvider.GetIntegerInputAsync("Minimum players (1-7)", 1, 7);
                config.MaxPlayers = await _inputProvider.GetIntegerInputAsync("Maximum players (1-7)", config.MinPlayers, 7);
                break;
            case 3:
                config.AllowDoubleDown = await _inputProvider.GetConfirmationAsync("Allow double down?");
                break;
            case 4:
                config.AllowSplit = await _inputProvider.GetConfirmationAsync("Allow split pairs?");
                break;
            case 5:
                config.AllowSurrender = await _inputProvider.GetConfirmationAsync("Allow surrender?");
                break;
            case 6:
                config.AllowInsurance = await _inputProvider.GetConfirmationAsync("Allow insurance?");
                break;
            case 7:
                config.DealerHitsOnSoft17 = await _inputProvider.GetConfirmationAsync("Dealer hits on soft 17?");
                break;
            case 8:
                var payoutInput = await _inputProvider.GetInputAsync("Blackjack payout ratio (e.g., 1.5 for 3:2)");
                if (double.TryParse(payoutInput, out var payout) && payout >= 1.0 && payout <= 2.0)
                {
                    config.BlackjackPayout = payout;
                }
                else
                {
                    await _outputProvider.WriteLineAsync("❌ Invalid payout ratio. Must be between 1.0 and 2.0.");
                }
                break;
            case 9:
                return;
        }

        await _outputProvider.WriteLineAsync("✅ Setting updated.");
    }

    /// <summary>
    /// Configures betting settings.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfigureBettingSettingsAsync(GameConfiguration config)
    {
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("💰 BETTING SETTINGS CONFIGURATION:");
        await _outputProvider.WriteLineAsync("💰   1. Minimum Bet");
        await _outputProvider.WriteLineAsync("💰   2. Maximum Bet");
        await _outputProvider.WriteLineAsync("💰   3. Default Bankroll");
        await _outputProvider.WriteLineAsync("💰   4. Minimum Bankroll");
        await _outputProvider.WriteLineAsync("💰   5. Maximum Bankroll");
        await _outputProvider.WriteLineAsync("💰   6. Back to Main Menu");

        var choice = await _inputProvider.GetIntegerInputAsync("Select setting to modify", 1, 6);

        switch (choice)
        {
            case 1:
                config.MinimumBet = await _inputProvider.GetBetAmountAsync("Enter minimum bet amount", new Money(1m), config.MaximumBet, new Money(10000m));
                break;
            case 2:
                config.MaximumBet = await _inputProvider.GetBetAmountAsync("Enter maximum bet amount", config.MinimumBet, new Money(10000m), new Money(10000m));
                break;
            case 3:
                config.DefaultBankroll = await _inputProvider.GetInitialBankrollAsync("Default", config.DefaultBankroll, config.MinimumBankroll, config.MaximumBankroll);
                break;
            case 4:
                config.MinimumBankroll = await _inputProvider.GetInitialBankrollAsync("Minimum", config.MinimumBankroll, new Money(10m), config.MaximumBankroll);
                break;
            case 5:
                config.MaximumBankroll = await _inputProvider.GetInitialBankrollAsync("Maximum", config.MaximumBankroll, config.MinimumBankroll, new Money(100000m));
                break;
            case 6:
                return;
        }

        await _outputProvider.WriteLineAsync("✅ Setting updated.");
    }

    /// <summary>
    /// Configures display settings.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfigureDisplaySettingsAsync(GameConfiguration config)
    {
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🎨 DISPLAY SETTINGS CONFIGURATION:");
        await _outputProvider.WriteLineAsync("🎨   1. Card Display Format");
        await _outputProvider.WriteLineAsync("🎨   2. Show Detailed Statistics");
        await _outputProvider.WriteLineAsync("🎨   3. Save Statistics");
        await _outputProvider.WriteLineAsync("🎨   4. Back to Main Menu");

        var choice = await _inputProvider.GetIntegerInputAsync("Select setting to modify", 1, 4);

        switch (choice)
        {
            case 1:
                await _outputProvider.WriteLineAsync("Card Display Formats:");
                await _outputProvider.WriteLineAsync("  1. Symbols (A♠, K♥)");
                await _outputProvider.WriteLineAsync("  2. Text (Ace of Spades, King of Hearts)");
                await _outputProvider.WriteLineAsync("  3. Abbreviated (AS, KH)");
                var formatChoice = await _inputProvider.GetIntegerInputAsync("Select format", 1, 3);
                config.CardDisplayFormat = formatChoice switch
                {
                    1 => CardDisplayFormat.Symbols,
                    2 => CardDisplayFormat.Text,
                    3 => CardDisplayFormat.Abbreviated,
                    _ => CardDisplayFormat.Symbols
                };
                break;
            case 2:
                config.ShowDetailedStatistics = await _inputProvider.GetConfirmationAsync("Show detailed statistics?");
                break;
            case 3:
                config.SaveStatistics = await _inputProvider.GetConfirmationAsync("Save game statistics?");
                break;
            case 4:
                return;
        }

        await _outputProvider.WriteLineAsync("✅ Setting updated.");
    }

    /// <summary>
    /// Configures advanced settings.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfigureAdvancedSettingsAsync(GameConfiguration config)
    {
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("🔧 ADVANCED SETTINGS CONFIGURATION:");
        await _outputProvider.WriteLineAsync("🔧   1. Auto Reshuffle");
        await _outputProvider.WriteLineAsync("🔧   2. Penetration Threshold");
        await _outputProvider.WriteLineAsync("🔧   3. Max Player Name Length");
        await _outputProvider.WriteLineAsync("🔧   4. Back to Main Menu");

        var choice = await _inputProvider.GetIntegerInputAsync("Select setting to modify", 1, 4);

        switch (choice)
        {
            case 1:
                config.AutoReshuffleEnabled = await _inputProvider.GetConfirmationAsync("Enable automatic reshuffling?");
                break;
            case 2:
                var thresholdInput = await _inputProvider.GetInputAsync("Penetration threshold (0.1-0.9, e.g., 0.25 for 25%)");
                if (double.TryParse(thresholdInput, out var threshold) && threshold >= 0.1 && threshold <= 0.9)
                {
                    config.PenetrationThreshold = threshold;
                }
                else
                {
                    await _outputProvider.WriteLineAsync("❌ Invalid threshold. Must be between 0.1 and 0.9.");
                }
                break;
            case 3:
                config.PlayerNameMaxLength = await _inputProvider.GetIntegerInputAsync("Maximum player name length (3-50)", 3, 50);
                break;
            case 4:
                return;
        }

        await _outputProvider.WriteLineAsync("✅ Setting updated.");
    }

    /// <summary>
    /// Shows configuration validation errors.
    /// </summary>
    /// <param name="config">The configuration with errors.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ShowConfigurationErrorsAsync(GameConfiguration config)
    {
        var errors = config.Validate();
        
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("❌ CONFIGURATION ERRORS:");
        foreach (var error in errors)
        {
            await _outputProvider.WriteLineAsync($"❌   {error.ErrorMessage}");
        }
        await _outputProvider.WriteLineAsync();
        await _outputProvider.WriteLineAsync("Please fix these errors before saving the configuration.");
    }
}