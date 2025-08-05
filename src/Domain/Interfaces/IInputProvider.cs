using System.Collections.Generic;
using System.Threading.Tasks;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Interfaces
{
    /// <summary>
    /// Provides abstraction for user input operations.
    /// This interface enables testable input handling by allowing mock implementations.
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>
        /// Prompts the user for input and returns the entered string.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the user's input.</returns>
        Task<string> GetInputAsync(string prompt);

        /// <summary>
        /// Prompts the user for an integer input within the specified range.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <param name="min">The minimum allowed value (inclusive).</param>
        /// <param name="max">The maximum allowed value (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the validated integer input.</returns>
        Task<int> GetIntegerInputAsync(string prompt, int min, int max);

        /// <summary>
        /// Prompts the user for a player action from the available valid actions.
        /// </summary>
        /// <param name="playerName">The name of the player being prompted.</param>
        /// <param name="validActions">The collection of valid actions the player can choose from.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the selected player action.</returns>
        Task<PlayerAction> GetPlayerActionAsync(string playerName, IEnumerable<PlayerAction> validActions);

        /// <summary>
        /// Prompts the user for a yes/no confirmation.
        /// </summary>
        /// <param name="prompt">The prompt message to display to the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true for yes, false for no.</returns>
        Task<bool> GetConfirmationAsync(string prompt);
    }
}