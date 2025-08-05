using System;
using Xunit;
using GroupProject.Domain.Exceptions;

namespace GroupProject.Tests.Domain.Exceptions
{
    /// <summary>
    /// Tests for the InvalidGameStateException class.
    /// </summary>
    public class InvalidGameStateExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_SetsMessageProperty()
        {
            // Arrange
            const string expectedMessage = "Invalid game state";

            // Act
            var exception = new InvalidGameStateException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_SetsProperties()
        {
            // Arrange
            const string expectedMessage = "Invalid game state";
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new InvalidGameStateException(expectedMessage, innerException);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void InvalidGameStateException_InheritsFromGameException()
        {
            // Arrange & Act
            var exception = new InvalidGameStateException("Test message");

            // Assert
            Assert.IsAssignableFrom<GameException>(exception);
        }

        [Fact]
        public void GameNotInitialized_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string operation = "deal cards";

            // Act
            var exception = InvalidGameStateException.GameNotInitialized(operation);

            // Assert
            Assert.Contains("Cannot deal cards", exception.Message);
            Assert.Contains("Game has not been initialized", exception.Message);
        }

        [Fact]
        public void WrongGamePhase_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string currentPhase = "Betting";
            const string expectedPhase = "Playing";

            // Act
            var exception = InvalidGameStateException.WrongGamePhase(currentPhase, expectedPhase);

            // Assert
            Assert.Contains("Game is in Betting phase", exception.Message);
            Assert.Contains("Playing phase is required", exception.Message);
        }

        [Fact]
        public void NotPlayerTurn_CreatesExceptionWithCorrectMessage()
        {
            // Arrange
            const string playerName = "Alice";
            const string currentPlayerName = "Bob";

            // Act
            var exception = InvalidGameStateException.NotPlayerTurn(playerName, currentPlayerName);

            // Assert
            Assert.Contains("It is not Alice's turn", exception.Message);
            Assert.Contains("Current turn: Bob", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("start game")]
        [InlineData("deal initial cards")]
        public void GameNotInitialized_WithDifferentOperations_CreatesCorrectMessage(string operation)
        {
            // Act
            var exception = InvalidGameStateException.GameNotInitialized(operation);

            // Assert
            Assert.Contains($"Cannot {operation}", exception.Message);
            Assert.Contains("Game has not been initialized", exception.Message);
        }

        [Theory]
        [InlineData("Setup", "Dealing")]
        [InlineData("Dealing", "Playing")]
        [InlineData("Playing", "Finished")]
        public void WrongGamePhase_WithDifferentPhases_CreatesCorrectMessage(string current, string expected)
        {
            // Act
            var exception = InvalidGameStateException.WrongGamePhase(current, expected);

            // Assert
            Assert.Contains($"Game is in {current} phase", exception.Message);
            Assert.Contains($"{expected} phase is required", exception.Message);
        }
    }
}