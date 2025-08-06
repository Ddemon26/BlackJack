using System;
using System.Threading.Tasks;

namespace GroupProject.Application.Interfaces
{
    /// <summary>
    /// Interface for handling and logging application errors.
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handles an exception by logging technical details and returning a user-friendly message.
        /// </summary>
        /// <param name="exception">The exception to handle.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        /// <returns>A user-friendly error message.</returns>
        Task<string> HandleExceptionAsync(Exception exception, string context = "");

        /// <summary>
        /// Logs an error with technical details for debugging purposes.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LogErrorAsync(Exception exception, string context = "");

        /// <summary>
        /// Determines if an error is recoverable and the application can continue.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the error is recoverable, false otherwise.</returns>
        bool IsRecoverableError(Exception exception);

        /// <summary>
        /// Gets a user-friendly error message for the given exception.
        /// </summary>
        /// <param name="exception">The exception to translate.</param>
        /// <returns>A user-friendly error message.</returns>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// Attempts to recover from an error using preserved state.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="context">Additional context about the error.</param>
        /// <returns>A task that returns true if recovery was successful, false otherwise.</returns>
        Task<bool> TryRecoverFromErrorAsync(Exception exception, string context = "");

        /// <summary>
        /// Creates a checkpoint of the current game state for potential recovery.
        /// </summary>
        /// <param name="checkpointName">A descriptive name for the checkpoint.</param>
        /// <param name="context">Additional context about when the checkpoint was created.</param>
        /// <returns>A task that returns the checkpoint ID if successful, null otherwise.</returns>
        Task<string?> CreateCheckpointAsync(string checkpointName, string context = "");

        /// <summary>
        /// Cleans up old error recovery states to prevent storage bloat.
        /// </summary>
        /// <param name="maxAge">The maximum age of states to keep.</param>
        /// <returns>A task that returns the number of states cleaned up.</returns>
        Task<int> CleanupOldRecoveryStatesAsync(TimeSpan? maxAge = null);
    }
}