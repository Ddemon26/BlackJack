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
    }
}