using System;
using System.Threading.Tasks;
using GroupProject.Application.Interfaces;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.Interfaces;

namespace GroupProject.Application.Services
{
    /// <summary>
    /// Handles application errors by providing user-friendly messages and logging technical details.
    /// </summary>
    public class ErrorHandler : IErrorHandler
    {
        private readonly IOutputProvider _outputProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandler"/> class.
        /// </summary>
        /// <param name="outputProvider">The output provider for logging errors.</param>
        public ErrorHandler(IOutputProvider outputProvider)
        {
            _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
        }

        /// <inheritdoc />
        public async Task<string> HandleExceptionAsync(Exception exception, string context = "")
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            // Log the technical details
            await LogErrorAsync(exception, context);

            // Return user-friendly message
            return GetUserFriendlyMessage(exception);
        }

        /// <inheritdoc />
        public async Task LogErrorAsync(Exception exception, string context = "")
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var logMessage = FormatErrorLogMessage(exception, context);
            
            // In a real application, this would go to a proper logging framework
            // For now, we'll use the output provider to write to console/debug output
            try
            {
                await _outputProvider.WriteLineAsync($"[ERROR] {logMessage}");
            }
            catch
            {
                // Fallback to console if output provider fails
                Console.WriteLine($"[ERROR] {logMessage}");
            }
        }

        /// <inheritdoc />
        public bool IsRecoverableError(Exception exception)
        {
            if (exception == null)
                return false;

            return exception switch
            {
                // Game-specific exceptions are generally recoverable
                InvalidPlayerActionException => true,
                InvalidGameStateException => true,
                
                // Specific argument exceptions (must come before base ArgumentException)
                ArgumentNullException => false, // Usually indicates a programming error
                ArgumentOutOfRangeException => true,
                ArgumentException => true, // Other argument exceptions might be recoverable
                
                // System exceptions are generally not recoverable
                OutOfMemoryException => false,
                StackOverflowException => false,
                AccessViolationException => false,
                
                // IO exceptions might be recoverable
                System.IO.IOException => true,
                
                // Invalid operation might be recoverable depending on context
                InvalidOperationException => true,
                
                // Default to recoverable for unknown exceptions
                _ => true
            };
        }

        /// <inheritdoc />
        public string GetUserFriendlyMessage(Exception exception)
        {
            if (exception == null)
                return "An unknown error occurred.";

            return exception switch
            {
                InvalidPlayerActionException playerEx => 
                    $"Invalid action: {playerEx.Message}",
                
                InvalidGameStateException gameStateEx => 
                    $"Game state error: {gameStateEx.Message}",
                
                ArgumentException argEx when argEx.Message.Contains("Player name") => 
                    "Please enter a valid player name.",
                
                ArgumentException argEx when argEx.Message.Contains("Count") => 
                    "Please enter a valid number of players.",
                
                ArgumentOutOfRangeException => 
                    "The value you entered is not within the valid range.",
                
                InvalidOperationException opEx when opEx.Message.Contains("empty") => 
                    "Cannot perform this action - no cards available.",
                
                InvalidOperationException opEx when opEx.Message.Contains("configuration") => 
                    "Game configuration error. Please check your settings.",
                
                System.IO.IOException => 
                    "There was a problem reading or writing data. Please try again.",
                
                TimeoutException => 
                    "The operation took too long to complete. Please try again.",
                
                NotSupportedException => 
                    "This operation is not supported in the current context.",
                
                // For any other exception, provide a generic but helpful message
                _ => "An unexpected error occurred. Please try again or restart the game."
            };
        }

        /// <summary>
        /// Formats an error message for logging with technical details.
        /// </summary>
        /// <param name="exception">The exception to format.</param>
        /// <param name="context">Additional context information.</param>
        /// <returns>A formatted error message for logging.</returns>
        private static string FormatErrorLogMessage(Exception exception, string context)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            var contextInfo = string.IsNullOrWhiteSpace(context) ? "" : $" | Context: {context}";
            
            var message = $"{timestamp} | {exception.GetType().Name}: {exception.Message}{contextInfo}";
            
            if (exception.InnerException != null)
            {
                message += $" | Inner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
            }
            
            // Include stack trace for debugging (in production, you might want to limit this)
            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
            {
                message += $" | Stack Trace: {exception.StackTrace}";
            }
            
            return message;
        }
    }
}