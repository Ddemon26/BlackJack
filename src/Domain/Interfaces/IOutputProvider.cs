using System.Threading.Tasks;

namespace GroupProject.Domain.Interfaces
{
    /// <summary>
    /// Provides abstraction for output operations.
    /// This interface enables testable output handling by allowing mock implementations.
    /// </summary>
    public interface IOutputProvider
    {
        /// <summary>
        /// Writes a line of text to the output followed by a line terminator.
        /// </summary>
        /// <param name="message">The text to write. If null, only the line terminator is written.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteLineAsync(string? message = null);

        /// <summary>
        /// Writes text to the output without a line terminator.
        /// </summary>
        /// <param name="message">The text to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteAsync(string message);

        /// <summary>
        /// Clears the output display if supported by the implementation.
        /// </summary>
        /// <returns>A task that represents the asynchronous clear operation.</returns>
        Task ClearAsync();

        /// <summary>
        /// Writes a formatted line of text to the output.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteLineAsync(string format, params object[] args);

        /// <summary>
        /// Writes formatted text to the output without a line terminator.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteAsync(string format, params object[] args);
    }
}