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
        private readonly IGameStatePreserver? _statePreserver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandler"/> class.
        /// </summary>
        /// <param name="outputProvider">The output provider for logging errors.</param>
        /// <param name="statePreserver">Optional state preserver for error recovery.</param>
        public ErrorHandler(IOutputProvider outputProvider, IGameStatePreserver? statePreserver = null)
        {
            _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
            _statePreserver = statePreserver;
        }

        /// <inheritdoc />
        public async Task<string> HandleExceptionAsync(Exception exception, string context = "")
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            // Preserve state before handling critical errors (if state preserver is available)
            if (_statePreserver != null && IsCriticalError(exception))
            {
                try
                {
                    var stateId = $"error_recovery_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
                    await _statePreserver.PreserveStateAsync(stateId, $"Error recovery: {exception.GetType().Name} in {context}");
                }
                catch (Exception stateEx)
                {
                    // Don't let state preservation errors interfere with error handling
                    await LogErrorAsync(stateEx, "State preservation during error handling");
                }
            }

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
                
                // Betting-related exceptions are recoverable
                InvalidBetException => true,
                InsufficientFundsException => true,
                BettingPhaseException => true,
                
                // Split-related exceptions are recoverable
                InvalidSplitException => true,
                SplitHandLimitException => true,
                
                // Double down exceptions are recoverable
                DoubleDownException => true,
                
                // Session exceptions may or may not be recoverable
                SessionException sessionEx => sessionEx.IsRecoverable,
                
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
                // Game-specific exceptions
                InvalidPlayerActionException playerEx => 
                    $"Invalid action: {playerEx.Message}",
                
                InvalidGameStateException gameStateEx => 
                    $"Game state error: {gameStateEx.Message}",
                
                // Betting-related exceptions with recovery guidance
                InvalidBetException betEx => 
                    $"{betEx.Message} {betEx.RecoveryGuidance}",
                
                InsufficientFundsException fundsEx => 
                    $"{fundsEx.Message} {fundsEx.RecoveryGuidance}",
                
                BettingPhaseException phaseEx => 
                    $"Betting error: {phaseEx.Message} {phaseEx.RecoveryGuidance}",
                
                // Split-related exceptions with recovery guidance
                InvalidSplitException splitEx => 
                    $"{splitEx.Message} {splitEx.RecoveryGuidance}",
                
                SplitHandLimitException limitEx => 
                    $"{limitEx.Message} {limitEx.RecoveryGuidance}",
                
                // Double down exceptions with recovery guidance
                DoubleDownException doubleEx => 
                    $"{doubleEx.Message} {doubleEx.RecoveryGuidance}",
                
                // Session exceptions with recovery guidance
                SessionException sessionEx => 
                    $"Session error: {sessionEx.Message} {sessionEx.RecoveryGuidance}",
                
                // Standard argument exceptions
                ArgumentException argEx when argEx.Message.Contains("Player name") => 
                    "Please enter a valid player name.",
                
                ArgumentException argEx when argEx.Message.Contains("Count") => 
                    "Please enter a valid number of players.",
                
                ArgumentOutOfRangeException => 
                    "The value you entered is not within the valid range.",
                
                // Operation exceptions
                InvalidOperationException opEx when opEx.Message.Contains("empty") => 
                    "Cannot perform this action - no cards available.",
                
                InvalidOperationException opEx when opEx.Message.Contains("configuration") => 
                    "Game configuration error. Please check your settings.",
                
                // IO and system exceptions
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
        /// Determines if an error is critical and requires state preservation.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the error is critical, false otherwise.</returns>
        private static bool IsCriticalError(Exception exception)
        {
            return exception switch
            {
                // System-level errors that could corrupt game state
                OutOfMemoryException => true,
                StackOverflowException => true,
                AccessViolationException => true,
                
                // Session errors that could lose game progress
                SessionException sessionEx when sessionEx.Reason == SessionErrorReason.SessionCorrupted => true,
                SessionException sessionEx when sessionEx.Reason == SessionErrorReason.PersistenceFailure => true,
                
                // IO errors that could indicate data corruption
                System.IO.IOException => true,
                
                // Invalid game state that could indicate corruption
                InvalidGameStateException => true,
                
                // Other errors are not considered critical
                _ => false
            };
        }

        /// <summary>
        /// Attempts to recover from an error using preserved state.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="context">Additional context about the error.</param>
        /// <returns>A task that returns true if recovery was successful, false otherwise.</returns>
        public async Task<bool> TryRecoverFromErrorAsync(Exception exception, string context = "")
        {
            if (_statePreserver == null || !IsRecoverableError(exception))
                return false;

            try
            {
                // Get the most recent state that might be suitable for recovery
                var stateIds = await _statePreserver.GetPreservedStateIdsAsync();
                var recentStateId = stateIds
                    .Where(id => id.StartsWith("error_recovery_") || id.StartsWith("checkpoint_"))
                    .OrderByDescending(id => id)
                    .FirstOrDefault();

                if (recentStateId != null)
                {
                    var recovered = await _statePreserver.RestoreStateAsync(recentStateId);
                    if (recovered)
                    {
                        await LogErrorAsync(new InvalidOperationException($"Successfully recovered from {exception.GetType().Name} using state {recentStateId}"), context);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception recoveryEx)
            {
                await LogErrorAsync(recoveryEx, $"Error recovery attempt for {exception.GetType().Name}");
                return false;
            }
        }

        /// <summary>
        /// Creates a checkpoint of the current game state for potential recovery.
        /// </summary>
        /// <param name="checkpointName">A descriptive name for the checkpoint.</param>
        /// <param name="context">Additional context about when the checkpoint was created.</param>
        /// <returns>A task that returns the checkpoint ID if successful, null otherwise.</returns>
        public async Task<string?> CreateCheckpointAsync(string checkpointName, string context = "")
        {
            if (_statePreserver == null || string.IsNullOrWhiteSpace(checkpointName))
                return null;

            try
            {
                var checkpointId = $"checkpoint_{checkpointName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                await _statePreserver.PreserveStateAsync(checkpointId, context);
                return checkpointId;
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, $"Failed to create checkpoint: {checkpointName}");
                return null;
            }
        }

        /// <summary>
        /// Cleans up old error recovery states to prevent storage bloat.
        /// </summary>
        /// <param name="maxAge">The maximum age of states to keep.</param>
        /// <returns>A task that returns the number of states cleaned up.</returns>
        public async Task<int> CleanupOldRecoveryStatesAsync(TimeSpan? maxAge = null)
        {
            if (_statePreserver == null)
                return 0;

            try
            {
                var cleanupAge = maxAge ?? TimeSpan.FromHours(24); // Default to 24 hours
                return await _statePreserver.ClearOldStatesAsync(cleanupAge);
            }
            catch (Exception ex)
            {
                await LogErrorAsync(ex, "Failed to cleanup old recovery states");
                return 0;
            }
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