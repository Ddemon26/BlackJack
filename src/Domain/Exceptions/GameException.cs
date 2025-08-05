using System;

namespace GroupProject.Domain.Exceptions
{
    /// <summary>
    /// Base exception class for all game-related exceptions.
    /// Provides a common foundation for handling business rule violations and game state errors.
    /// </summary>
    public abstract class GameException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected GameException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected GameException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}