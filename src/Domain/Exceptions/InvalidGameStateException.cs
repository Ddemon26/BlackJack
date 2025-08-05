using System;

namespace GroupProject.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an operation is attempted on a game in an invalid state.
    /// This includes scenarios like trying to deal cards when the game hasn't started,
    /// or attempting player actions when it's not their turn.
    /// </summary>
    public class InvalidGameStateException : GameException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidGameStateException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the invalid game state error.</param>
        public InvalidGameStateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidGameStateException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidGameStateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an exception for when a game operation is attempted before the game has been properly initialized.
        /// </summary>
        /// <param name="operation">The operation that was attempted.</param>
        /// <returns>A new <see cref="InvalidGameStateException"/> instance.</returns>
        public static InvalidGameStateException GameNotInitialized(string operation)
        {
            return new InvalidGameStateException($"Cannot {operation}: Game has not been initialized.");
        }

        /// <summary>
        /// Creates an exception for when a player action is attempted during the wrong game phase.
        /// </summary>
        /// <param name="currentPhase">The current game phase.</param>
        /// <param name="expectedPhase">The expected game phase for the operation.</param>
        /// <returns>A new <see cref="InvalidGameStateException"/> instance.</returns>
        public static InvalidGameStateException WrongGamePhase(string currentPhase, string expectedPhase)
        {
            return new InvalidGameStateException($"Cannot perform operation: Game is in {currentPhase} phase, but {expectedPhase} phase is required.");
        }

        /// <summary>
        /// Creates an exception for when it's not the specified player's turn.
        /// </summary>
        /// <param name="playerName">The name of the player attempting the action.</param>
        /// <param name="currentPlayerName">The name of the player whose turn it is.</param>
        /// <returns>A new <see cref="InvalidGameStateException"/> instance.</returns>
        public static InvalidGameStateException NotPlayerTurn(string playerName, string currentPlayerName)
        {
            return new InvalidGameStateException($"It is not {playerName}'s turn. Current turn: {currentPlayerName}");
        }
    }
}