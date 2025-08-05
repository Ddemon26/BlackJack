using System;
using System.Collections.Generic;
using Xunit;
using GroupProject.Domain.Exceptions;
using GroupProject.Domain.ValueObjects;

namespace GroupProject.Tests.Domain.Exceptions
{
    /// <summary>
    /// Tests for the InvalidPlayerActionException class.
    /// </summary>
    public class InvalidPlayerActionExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessageProperty()
        {
            // Arrange
            const string expectedMessage = "Invalid player action";

            // Act
            var exception = new InvalidPlayerActionException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Null(exception.PlayerName);
            Assert.Null(exception.AttemptedAction);
        }

        [Fact]
        public void Constructor_WithMessagePlayerNameAndAction_SetsAllProperties()
        {
            // Arrange
            const string expectedMessage = "Invalid player action";
            const string playerName = "Alice";
            const PlayerAction action = PlayerAction.Hit;

            // Act
            var exception = new InvalidPlayerActionException(expectedMessage, playerName, action);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            const string expectedMessage = "Invalid player action";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new InvalidPlayerActionException(expectedMessage, innerException);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
            Assert.Null(exception.PlayerName);
            Assert.Null(exception.AttemptedAction);
        }

        [Fact]
        public void InvalidPlayerActionException_InheritsFromGameException()
        {
            // Arrange & Act
            var exception = new InvalidPlayerActionException("Test message");

            // Assert
            Assert.IsAssignableFrom<GameException>(exception);
        }

        [Fact]
        public void ActionNotAllowed_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string playerName = "Alice";
            const PlayerAction action = PlayerAction.DoubleDown;
            const string reason = "Cannot double down after hitting";

            // Act
            var exception = InvalidPlayerActionException.ActionNotAllowed(playerName, action, reason);

            // Assert
            Assert.Contains("Player Alice cannot perform action DoubleDown", exception.Message);
            Assert.Contains("Cannot double down after hitting", exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }

        [Fact]
        public void ActionNotValid_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string playerName = "Bob";
            const PlayerAction action = PlayerAction.Split;
            var validActions = new List<PlayerAction> { PlayerAction.Hit, PlayerAction.Stand };

            // Act
            var exception = InvalidPlayerActionException.ActionNotValid(playerName, action, validActions);

            // Assert
            Assert.Contains("Player Bob attempted invalid action Split", exception.Message);
            Assert.Contains("Valid actions are: Hit, Stand", exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }

        [Fact]
        public void HandAlreadyBusted_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string playerName = "Charlie";
            const PlayerAction action = PlayerAction.Hit;

            // Act
            var exception = InvalidPlayerActionException.HandAlreadyBusted(playerName, action);

            // Assert
            Assert.Contains("Player Charlie cannot perform action Hit", exception.Message);
            Assert.Contains("Hand is already busted", exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }

        [Fact]
        public void HandAlreadyComplete_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string playerName = "Diana";
            const PlayerAction action = PlayerAction.Hit;

            // Act
            var exception = InvalidPlayerActionException.HandAlreadyComplete(playerName, action);

            // Assert
            Assert.Contains("Player Diana cannot perform action Hit", exception.Message);
            Assert.Contains("Hand is already complete", exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }

        [Theory]
        [InlineData(PlayerAction.Hit)]
        [InlineData(PlayerAction.Stand)]
        [InlineData(PlayerAction.DoubleDown)]
        [InlineData(PlayerAction.Split)]
        public void ActionNotAllowed_WithDifferentActions_SetsCorrectAction(PlayerAction action)
        {
            // Arrange
            const string playerName = "TestPlayer";
            const string reason = "Test reason";

            // Act
            var exception = InvalidPlayerActionException.ActionNotAllowed(playerName, action, reason);

            // Assert
            Assert.Equal(action, exception.AttemptedAction);
            Assert.Contains(action.ToString(), exception.Message);
        }

        [Fact]
        public void ActionNotValid_WithEmptyValidActions_CreatesCorrectMessage()
        {
            // Arrange
            const string playerName = "TestPlayer";
            const PlayerAction action = PlayerAction.Hit;
            var validActions = new List<PlayerAction>();

            // Act
            var exception = InvalidPlayerActionException.ActionNotValid(playerName, action, validActions);

            // Assert
            Assert.Contains("Valid actions are:", exception.Message);
            Assert.Equal(playerName, exception.PlayerName);
            Assert.Equal(action, exception.AttemptedAction);
        }
    }
}