using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Domain.Interfaces;
using GroupProject.Domain.ValueObjects;

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