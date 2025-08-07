using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;
using GroupProject.Infrastructure.Formatting;

namespace GroupProject.Infrastructure.Providers
{
    /// <summary>
    /// Console implementation of IInputProvider for reading user input from the console.
    /// Provides input validation and retry logic for robust user interaction.
    /// </summary>
    public class ConsoleInputProvider : IInputProvider
    {
        private readonly IOutputProvider _outputProvider;

        /// <summary>
        /// Initializes a new instance of ConsoleInputProvider.
        /// </summary>
        /// <param name="outputProvider">The output provider for displaying prompts and error messages.</param>
        public ConsoleInputProvider(IOutputProvider outputProvider)
        {
            _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
        }

        /// <summary>
        /// Prompts the user for input and returns the entered string.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's input.</returns>
        public async Task<string> GetInputAsync(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            await _outputProvider.WriteAsync($"{prompt}: ");
            var input = await Task.Run(() => Console.ReadLine());
            
            // Basic input sanitization - remove control characters but preserve spaces
            if (input != null)
            {
                input = new string(input.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
            }
            
            return input ?? string.Empty;
        }

        /// <summary>
        /// Prompts the user for an integer input within the specified range.
        /// Continues prompting until a valid integer within the range is entered.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <param name="min">The minimum allowed value (inclusive).</param>
        /// <param name="max">The maximum allowed value (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validated integer input.</returns>
        public async Task<int> GetIntegerInputAsync(string prompt, int min, int max)
        {
            if (min > max)
            {
                throw new ArgumentException("Minimum value cannot be greater than maximum value.");
            }

            var attempts = 0;
            const int maxAttempts = 5;

            while (true)
            {
                attempts++;
                var input = await GetInputAsync($"{prompt} ({min}-{max})");
                
                // Handle empty input
                if (string.IsNullOrWhiteSpace(input))
                {
                    await _outputProvider.WriteLineAsync("❌ Input cannot be empty. Please enter a number.");
                    continue;
                }

                // Sanitize input - remove extra whitespace and common non-numeric characters
                var sanitizedInput = input.Trim().Replace(" ", "");
                
                if (int.TryParse(sanitizedInput, out var value))
                {
                    if (value >= min && value <= max)
                    {
                        return value;
                    }
                    
                    await _outputProvider.WriteLineAsync($"❌ {value} is out of range. Please enter a number between {min} and {max}.");
                }
                else
                {
                    await _outputProvider.WriteLineAsync($"❌ '{input}' is not a valid number. Please enter a whole number between {min} and {max}.");
                }

                // Provide additional help after multiple failed attempts
                if (attempts >= 3 && attempts < maxAttempts)
                {
                    await _outputProvider.WriteLineAsync($"💡 Hint: Enter only digits (e.g., {min}, {(min + max) / 2}, {max})");
                }
                else if (attempts >= maxAttempts)
                {
                    await _outputProvider.WriteLineAsync("⚠️  Too many invalid attempts. Please be careful with your input.");
                    attempts = 0; // Reset counter but continue trying
                }
            }
        }

        /// <summary>
        /// Prompts the user for a player action from the available valid actions.
        /// Accepts multiple input formats (h/hit, s/stand) and continues prompting until valid input is received.
        /// </summary>
        /// <param name="playerName">The name of the player being prompted.</param>
        /// <param name="validActions">The collection of valid actions the player can choose from.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the selected player action.</returns>
        public async Task<PlayerAction> GetPlayerActionAsync(string playerName, IEnumerable<PlayerAction> validActions)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));
            }

            var validActionsList = validActions?.ToList() ?? throw new ArgumentNullException(nameof(validActions));
            
            if (!validActionsList.Any())
            {
                throw new ArgumentException("Valid actions cannot be empty.", nameof(validActions));
            }

            var actionPrompts = GetActionPrompts(validActionsList);
            var prompt = $"{playerName}, choose your action ({string.Join(", ", actionPrompts)})";

            var attempts = 0;
            const int maxAttempts = 3;

            while (true)
            {
                attempts++;
                var input = await GetInputAsync(prompt);
                
                // Handle empty input
                if (string.IsNullOrWhiteSpace(input))
                {
                    await _outputProvider.WriteLineAsync("❌ Please enter an action. You cannot skip your turn.");
                    continue;
                }

                // Sanitize and normalize input
                var normalizedInput = input.Trim().ToLowerInvariant().Replace(" ", "");

                var selectedAction = ParsePlayerAction(normalizedInput, validActionsList);
                if (selectedAction.HasValue)
                {
                    return selectedAction.Value;
                }

                // Provide detailed error message
                await _outputProvider.WriteLineAsync($"❌ '{input}' is not a valid action.");
                
                // Show available options with more detail
                if (attempts == 1)
                {
                    await _outputProvider.WriteLineAsync($"   Available actions: {string.Join(", ", actionPrompts)}");
                }
                else if (attempts >= maxAttempts)
                {
                    await _outputProvider.WriteLineAsync("💡 Detailed help:");
                    foreach (var action in validActionsList)
                    {
                        var helpText = action switch
                        {
                            PlayerAction.Hit => "   • Hit (h/hit) - Take another card",
                            PlayerAction.Stand => "   • Stand (s/stand) - Keep your current hand and end turn",
                            PlayerAction.DoubleDown => "   • Double Down (d/double) - Double bet and take one card",
                            PlayerAction.Split => "   • Split (p/split) - Split pair into two hands",
                            _ => $"   • {action}"
                        };
                        await _outputProvider.WriteLineAsync(helpText);
                    }
                    attempts = 0; // Reset counter but continue trying
                }
            }
        }

        /// <summary>
        /// Prompts the user for a yes/no confirmation.
        /// Accepts various formats (y/yes/n/no) and continues prompting until valid input is received.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true for yes, false for no.</returns>
        public async Task<bool> GetConfirmationAsync(string prompt)
        {
            var attempts = 0;
            const int maxAttempts = 3;

            while (true)
            {
                attempts++;
                var input = await GetInputAsync($"{prompt} (y/n)");
                
                // Handle empty input
                if (string.IsNullOrWhiteSpace(input))
                {
                    await _outputProvider.WriteLineAsync("❌ Please enter 'y' for yes or 'n' for no.");
                    continue;
                }

                // Sanitize and normalize input
                var normalizedInput = input.Trim().ToLowerInvariant().Replace(" ", "");

                switch (normalizedInput)
                {
                    case "y":
                    case "yes":
                    case "1":
                    case "true":
                        return true;
                    case "n":
                    case "no":
                    case "0":
                    case "false":
                        return false;
                    default:
                        await _outputProvider.WriteLineAsync($"❌ '{input}' is not a valid response.");
                        
                        if (attempts >= maxAttempts)
                        {
                            await _outputProvider.WriteLineAsync("💡 Valid responses: y, yes, n, no (case insensitive)");
                            attempts = 0; // Reset counter but continue trying
                        }
                        else
                        {
                            await _outputProvider.WriteLineAsync("   Please enter 'y' for yes or 'n' for no.");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Prompts the user for a monetary bet amount within the specified range.
        /// Continues prompting until a valid bet amount within the range and available funds is entered.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <param name="minBet">The minimum allowed bet amount.</param>
        /// <param name="maxBet">The maximum allowed bet amount.</param>
        /// <param name="availableFunds">The player's available funds.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validated bet amount.</returns>
        public async Task<Money> GetBetAmountAsync(string prompt, Money minBet, Money maxBet, Money availableFunds)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));

            if (minBet > maxBet)
                throw new ArgumentException("Minimum bet cannot be greater than maximum bet.");

            if (availableFunds < minBet)
                throw new ArgumentException("Available funds are insufficient for minimum bet.");

            // Determine the effective maximum bet (limited by available funds)
            var effectiveMaxBet = availableFunds < maxBet ? availableFunds : maxBet;

            var attempts = 0;

            while (true)
            {
                attempts++;
                
                // Display betting information
                await _outputProvider.WriteLineAsync($"Available funds: {MoneyFormatter.FormatWithSymbol(availableFunds)}");
                await _outputProvider.WriteLineAsync($"Betting range: {MoneyFormatter.FormatWithSymbol(minBet)} - {MoneyFormatter.FormatWithSymbol(effectiveMaxBet)}");
                
                var input = await GetInputAsync($"{prompt}");
                
                // Handle empty input
                if (string.IsNullOrWhiteSpace(input))
                {
                    await _outputProvider.WriteLineAsync("❌ Bet amount cannot be empty. Please enter an amount.");
                    continue;
                }

                // Parse the monetary input
                var betAmount = ParseMoneyInput(input, minBet.Currency);
                
                if (betAmount == null)
                {
                    await _outputProvider.WriteLineAsync($"❌ '{input}' is not a valid monetary amount.");
                    if (attempts == 1)
                    {
                        await _outputProvider.WriteLineAsync($"💡 Examples: {MoneyFormatter.FormatWithSymbol(minBet)}, {MoneyFormatter.FormatWithSymbol(new Money(25m))}, {MoneyFormatter.FormatWithSymbol(effectiveMaxBet)}");
                    }
                    continue;
                }

                // Validate bet amount is within range
                if (betAmount < minBet)
                {
                    await _outputProvider.WriteLineAsync($"❌ Bet amount {MoneyFormatter.FormatWithSymbol(betAmount.Value)} is below minimum bet of {MoneyFormatter.FormatWithSymbol(minBet)}.");
                    continue;
                }

                if (betAmount > effectiveMaxBet)
                {
                    if (effectiveMaxBet < maxBet)
                    {
                        await _outputProvider.WriteLineAsync($"❌ Bet amount {MoneyFormatter.FormatWithSymbol(betAmount.Value)} exceeds available funds of {MoneyFormatter.FormatWithSymbol(availableFunds)}.");
                    }
                    else
                    {
                        await _outputProvider.WriteLineAsync($"❌ Bet amount {MoneyFormatter.FormatWithSymbol(betAmount.Value)} exceeds maximum bet of {MoneyFormatter.FormatWithSymbol(maxBet)}.");
                    }
                    continue;
                }

                return betAmount.Value;
            }
        }

        /// <summary>
        /// Prompts the user for their initial bankroll amount.
        /// Continues prompting until a valid bankroll amount within the range is entered.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="defaultAmount">The default bankroll amount to suggest.</param>
        /// <param name="minAmount">The minimum allowed bankroll amount.</param>
        /// <param name="maxAmount">The maximum allowed bankroll amount.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the initial bankroll amount.</returns>
        public async Task<Money> GetInitialBankrollAsync(string playerName, Money defaultAmount, Money minAmount, Money maxAmount)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                throw new ArgumentException("Player name cannot be null or empty.", nameof(playerName));

            if (minAmount > maxAmount)
                throw new ArgumentException("Minimum amount cannot be greater than maximum amount.");

            if (defaultAmount < minAmount || defaultAmount > maxAmount)
                throw new ArgumentException("Default amount must be within the specified range.");

            var attempts = 0;

            await _outputProvider.WriteLineAsync($"Setting up bankroll for {playerName}:");
            await _outputProvider.WriteLineAsync($"Range: {MoneyFormatter.FormatWithSymbol(minAmount)} - {MoneyFormatter.FormatWithSymbol(maxAmount)}");
            await _outputProvider.WriteLineAsync($"Default: {MoneyFormatter.FormatWithSymbol(defaultAmount)}");

            while (true)
            {
                attempts++;
                
                var input = await GetInputAsync($"Enter initial bankroll for {playerName} (or press Enter for default)");
                
                // Handle empty input - use default
                if (string.IsNullOrWhiteSpace(input))
                {
                    await _outputProvider.WriteLineAsync($"✓ Using default bankroll: {MoneyFormatter.FormatWithSymbol(defaultAmount)}");
                    return defaultAmount;
                }

                // Parse the monetary input
                var bankrollAmount = ParseMoneyInput(input, defaultAmount.Currency);
                
                if (bankrollAmount == null)
                {
                    await _outputProvider.WriteLineAsync($"❌ '{input}' is not a valid monetary amount.");
                    if (attempts == 1)
                    {
                        await _outputProvider.WriteLineAsync($"💡 Examples: {MoneyFormatter.FormatWithSymbol(minAmount)}, {MoneyFormatter.FormatWithSymbol(defaultAmount)}, {MoneyFormatter.FormatWithSymbol(maxAmount)}");
                    }
                    continue;
                }

                // Validate bankroll amount is within range
                if (bankrollAmount < minAmount)
                {
                    await _outputProvider.WriteLineAsync($"❌ Bankroll amount {MoneyFormatter.FormatWithSymbol(bankrollAmount.Value)} is below minimum of {MoneyFormatter.FormatWithSymbol(minAmount)}.");
                    continue;
                }

                if (bankrollAmount > maxAmount)
                {
                    await _outputProvider.WriteLineAsync($"❌ Bankroll amount {MoneyFormatter.FormatWithSymbol(bankrollAmount.Value)} exceeds maximum of {MoneyFormatter.FormatWithSymbol(maxAmount)}.");
                    continue;
                }

                await _outputProvider.WriteLineAsync($"✓ Bankroll set to: {MoneyFormatter.FormatWithSymbol(bankrollAmount.Value)}");
                return bankrollAmount.Value;
            }
        }

        /// <summary>
        /// Parses a string input into a Money value object.
        /// Supports various input formats including currency symbols and decimal amounts.
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <param name="currency">The currency to use for the parsed amount.</param>
        /// <returns>A Money object if parsing succeeds, null otherwise.</returns>
        private static Money? ParseMoneyInput(string input, string currency)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Clean the input - remove common currency symbols and extra whitespace
            var cleanInput = input.Trim()
                .Replace("$", "")
                .Replace("€", "")
                .Replace("£", "")
                .Replace("¥", "")
                .Replace(",", "")
                .Replace(" ", "");

            // Try to parse as decimal
            if (decimal.TryParse(cleanInput, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, 
                CultureInfo.InvariantCulture, out var amount))
            {
                // Ensure the amount is positive and has at most 2 decimal places
                if (amount > 0)
                {
                    try
                    {
                        return new Money(amount, currency);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Amount has more than 2 decimal places
                        return null;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> GetActionPrompts(IEnumerable<PlayerAction> validActions)
        {
            return validActions.Select(action => action switch
            {
                PlayerAction.Hit => "h/hit",
                PlayerAction.Stand => "s/stand",
                PlayerAction.DoubleDown => "d/double",
                PlayerAction.Split => "p/split",
                _ => action.ToString().ToLowerInvariant()
            });
        }

        private static PlayerAction? ParsePlayerAction(string input, IEnumerable<PlayerAction> validActions)
        {
            var validActionsList = validActions.ToList();

            return input switch
            {
                // Hit variations
                "h" or "hit" or "1" when validActionsList.Contains(PlayerAction.Hit) => PlayerAction.Hit,
                
                // Stand variations  
                "s" or "stand" or "stay" or "2" when validActionsList.Contains(PlayerAction.Stand) => PlayerAction.Stand,
                
                // Double down variations
                "d" or "double" or "doubledown" or "dd" or "3" when validActionsList.Contains(PlayerAction.DoubleDown) => PlayerAction.DoubleDown,
                
                // Split variations
                "p" or "split" or "sp" or "4" when validActionsList.Contains(PlayerAction.Split) => PlayerAction.Split,
                
                _ => null
            };
        }
    }
}