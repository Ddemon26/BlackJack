using System;
using System.Collections.Generic;
using System.Linq;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a player attempts an invalid action during the game.
    /// This includes actions that are not allowed based on the current hand state,
    /// game rules, or player status.
    /// </summary>
    public class InvalidPlayerActionException : GameException
    {
        /// <summary>
        /// Gets the action that was attempted.
        /// </summary>
        public PlayerAction? AttemptedAction { get; }

        /// <summary>
        /// Gets the name of the player who attempted the action.
        /// </summary>
        public string? PlayerName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPlayerActionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the invalid player action error.</param>
        public InvalidPlayerActionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPlayerActionException"/> class with a specified error message,
        /// player name, and attempted action.
        /// </summary>
        /// <param name="message">The message that describes the invalid player action error.</param>
        /// <param name="playerName">The name of the player who attempted the action.</param>
        /// <param name="attemptedAction">The action that was attempted.</param>
        public InvalidPlayerActionException(string message, string playerName, PlayerAction attemptedAction) : base(message)
        {
            PlayerName = playerName;
            AttemptedAction = attemptedAction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPlayerActionException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidPlayerActionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an exception for when a player attempts an action that is not valid for their current hand state.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="action">The attempted action.</param>
        /// <param name="reason">The reason why the action is invalid.</param>
        /// <returns>A new <see cref="InvalidPlayerActionException"/> instance.</returns>
        public static InvalidPlayerActionException ActionNotAllowed(string playerName, PlayerAction action, string reason)
        {
            return new InvalidPlayerActionException(
                $"Player {playerName} cannot perform action {action}: {reason}",
                playerName,
                action);
        }

        /// <summary>
        /// Creates an exception for when a player attempts an action that is not in the list of valid actions.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="action">The attempted action.</param>
        /// <param name="validActions">The list of valid actions.</param>
        /// <returns>A new <see cref="InvalidPlayerActionException"/> instance.</returns>
        public static InvalidPlayerActionException ActionNotValid(string playerName, PlayerAction action, IEnumerable<PlayerAction> validActions)
        {
            var validActionsString = string.Join(", ", validActions.Select(a => a.ToString()));
            return new InvalidPlayerActionException(
                $"Player {playerName} attempted invalid action {action}. Valid actions are: {validActionsString}",
                playerName,
                action);
        }

        /// <summary>
        /// Creates an exception for when a player attempts to act when their hand is already busted.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="action">The attempted action.</param>
        /// <returns>A new <see cref="InvalidPlayerActionException"/> instance.</returns>
        public static InvalidPlayerActionException HandAlreadyBusted(string playerName, PlayerAction action)
        {
            return new InvalidPlayerActionException(
                $"Player {playerName} cannot perform action {action}: Hand is already busted",
                playerName,
                action);
        }

        /// <summary>
        /// Creates an exception for when a player attempts to act when their hand is already complete (stood).
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="action">The attempted action.</param>
        /// <returns>A new <see cref="InvalidPlayerActionException"/> instance.</returns>
        public static InvalidPlayerActionException HandAlreadyComplete(string playerName, PlayerAction action)
        {
            return new InvalidPlayerActionException(
                $"Player {playerName} cannot perform action {action}: Hand is already complete",
                playerName,
                action);
        }
    }
}